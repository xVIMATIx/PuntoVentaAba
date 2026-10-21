using Microsoft.EntityFrameworkCore;
using PuntoAba.Data;
using PuntoAba.DTOs;
using PuntoAba.Models;

namespace PuntoAba.Services;
public class ControlInventarioService(DbContextOptions<PuntoAbaDbContext> opciones, IAccesoOperacion acceso) : IControlInventarioService
{
    public static void CantidadValida(Producto producto, decimal cantidad, bool permiteCero = false)
    {
        if (cantidad < 0 || (!permiteCero && cantidad == 0) || cantidad > 999999999999999.999m || decimal.Round(cantidad, 3) != cantidad || (!producto.EsPorPeso && decimal.Truncate(cantidad) != cantidad))
            throw new ArgumentException($"Revisa la cantidad de {producto.Nombre}: usa valores {(permiteCero ? "no negativos" : "positivos")}, enteros para piezas o hasta tres decimales para peso.");
    }
    public static string MotivoValido(string motivo)
    {
        motivo = motivo?.Trim() ?? "";
        if (motivo.Length < 3 || motivo.Length > 400) throw new ArgumentException("Escribe un motivo de entre 3 y 400 caracteres.");
        return motivo;
    }
    public static MovimientoInventarioDto Mapear(MovimientoInventario m) => new()
    {
        Id = m.Id, Fecha = m.FechaMovimiento, Codigo = m.Producto?.Codigo ?? "", Producto = m.Producto?.Nombre ?? "", Tipo = m.TipoMovimiento,
        Cantidad = m.Cantidad, StockAnterior = m.StockAnterior, StockNuevo = m.StockNuevo, Motivo = m.Motivo, Referencia = m.Referencia,
        Usuario = m.Usuario?.NombreCompleto, MovimientoOriginalId = m.MovimientoOriginalId
    };
    public async Task<MovimientoInventarioDto> RegistrarAsync(OperacionExistenciasDto dto)
    {
        var actor = await acceso.ExigirAsync("Admin", "Supervisor");
        var motivo = MotivoValido(dto.Motivo);
        if (dto.OperacionId == Guid.Empty || dto.Referencia.Length > 100) throw new ArgumentException("Revisa el identificador y la referencia de la operación.");
        await using var db = new PuntoAbaDbContext(opciones);
        await using var tx = await db.Database.BeginTransactionAsync();
        var existente = await db.MovimientosInventario.Include(m => m.Producto).Include(m => m.Usuario).FirstOrDefaultAsync(m => m.OperacionId == dto.OperacionId);
        if (existente != null) { if (existente.UsuarioId != actor.Id) throw new UnauthorizedAccessException(); return Mapear(existente); }
        var p = await db.Productos.FirstOrDefaultAsync(p => p.Id == dto.ProductoId && p.Activo) ?? throw new ArgumentException("El producto no está disponible.");
        decimal delta;
        if (dto.Tipo == "Ajuste")
        {
            CantidadValida(p, Math.Abs(dto.Cantidad)); delta = dto.Cantidad;
        }
        else
        {
            CantidadValida(p, dto.Cantidad);
            delta = dto.Tipo switch { "Merma" or "DevolucionProveedor" => -dto.Cantidad, "DevolucionCliente" => dto.Cantidad, "DevolucionNoVendible" => 0, _ => throw new ArgumentException("Selecciona una operación válida.") };
            if (dto.Tipo.StartsWith("Devolucion") && string.IsNullOrWhiteSpace(dto.Referencia)) throw new ArgumentException("Indica la referencia de la devolución.");
        }
        if (dto.StockEsperado.HasValue && p.Stock != dto.StockEsperado.Value) throw new ArgumentException("Las existencias cambiaron. Actualiza el producto antes de confirmar.");
        if (p.Stock + delta < 0 || p.Stock + delta > 999999999999999.999m) throw new ArgumentException("La operación dejaría existencias fuera del rango permitido.");
        var m = new MovimientoInventario { Producto = p, TipoMovimiento = dto.Tipo, Cantidad = Math.Abs(dto.Cantidad), StockAnterior = p.Stock, StockNuevo = p.Stock + delta, Motivo = motivo, Referencia = dto.Referencia.Trim(), UsuarioId = actor.Id, OperacionId = dto.OperacionId, CostoUnitario = p.PrecioCompra };
        p.Stock += delta; p.VersionInventario++; p.FechaActualizacion = p.UltimaSincronizacion = DateTime.UtcNow;
        db.MovimientosInventario.Add(m); await db.SaveChangesAsync(); await tx.CommitAsync();
        var r = Mapear(m); r.Usuario = actor.NombreCompleto; return r;
    }
    public async Task<MovimientoInventarioDto> CorregirAsync(int movimientoId, string motivo, Guid operacionId)
    {
        var actor = await acceso.ExigirAsync("Admin", "Supervisor"); motivo = MotivoValido(motivo);
        if (operacionId == Guid.Empty) throw new ArgumentException("La corrección necesita un identificador.");
        await using var db = new PuntoAbaDbContext(opciones); await using var tx = await db.Database.BeginTransactionAsync();
        var repetido = await db.MovimientosInventario.Include(m => m.Producto).FirstOrDefaultAsync(m => m.OperacionId == operacionId);
        if (repetido != null) { if (repetido.UsuarioId != actor.Id) throw new UnauthorizedAccessException(); return Mapear(repetido); }
        var original = await db.MovimientosInventario.Include(m => m.Producto).FirstOrDefaultAsync(m => m.Id == movimientoId) ?? throw new ArgumentException("No se encontró el movimiento.");
        if (await db.MovimientosInventario.AnyAsync(m => m.MovimientoOriginalId == movimientoId)) throw new ArgumentException("Este movimiento ya tiene una corrección vinculada.");
        var p = original.Producto!; var delta = original.StockAnterior - original.StockNuevo;
        if (p.Stock + delta < 0) throw new ArgumentException("La corrección dejaría existencias negativas. Revisa el inventario físico.");
        var m = new MovimientoInventario { Producto = p, TipoMovimiento = "Correccion", Cantidad = original.Cantidad, StockAnterior = p.Stock, StockNuevo = p.Stock + delta, Motivo = motivo, Referencia = $"Corrección de movimiento {movimientoId}", MovimientoOriginalId = movimientoId, UsuarioId = actor.Id, OperacionId = operacionId, CostoUnitario = original.CostoUnitario ?? p.PrecioCompra };
        p.Stock += delta; p.VersionInventario++; p.FechaActualizacion = p.UltimaSincronizacion = DateTime.UtcNow;
        db.MovimientosInventario.Add(m); await db.SaveChangesAsync(); await tx.CommitAsync(); return Mapear(m);
    }
    public async Task<ConteoDto> IniciarConteoAsync(List<int> productos, Guid operacionId)
    {
        var actor = await acceso.ExigirAsync("Admin", "Supervisor", "Cajero");
        if (operacionId == Guid.Empty || productos.Count == 0) throw new ArgumentException("Selecciona al menos un producto.");
        await using var db = new PuntoAbaDbContext(opciones); await using var tx = await db.Database.BeginTransactionAsync();
        var existente = await db.ConteosFisicos.Include(c => c.Detalles).ThenInclude(d => d.Producto).FirstOrDefaultAsync(c => c.OperacionId == operacionId);
        if (existente != null) { if (existente.ResponsableId != actor.Id) throw new UnauthorizedAccessException(); return await MapearConteo(db, existente); }
        var ps = await db.Productos.Where(p => productos.Contains(p.Id) && p.Activo).ToListAsync();
        if (ps.Count != productos.Distinct().Count()) throw new ArgumentException("Hay productos inactivos o inexistentes en la selección.");
        var conteo = new ConteoFisico { OperacionId = operacionId, ResponsableId = actor.Id, Detalles = ps.Select(p => new DetalleConteoFisico { Producto = p, CantidadSistema = p.Stock, VersionProducto = p.VersionInventario }).ToList() };
        db.ConteosFisicos.Add(conteo); await db.SaveChangesAsync(); await tx.CommitAsync(); return await MapearConteo(db, conteo);
    }
    public async Task<List<ConteoDto>> ObtenerConteosAsync()
    {
        var actor = await acceso.ExigirAsync("Admin", "Supervisor", "Cajero"); await using var db = new PuntoAbaDbContext(opciones);
        var consulta = db.ConteosFisicos.AsNoTracking().AsQueryable(); if (actor.Rol == "Cajero") consulta = consulta.Where(c => c.ResponsableId == actor.Id);
        var cs = await consulta.Include(c => c.Detalles).ThenInclude(d => d.Producto).OrderByDescending(c => c.Id).Take(50).ToListAsync();
        var result = new List<ConteoDto>(); foreach (var c in cs) result.Add(await MapearConteo(db, c)); return result;
    }
    public async Task<ConteoDto?> ObtenerConteoAsync(int id)
    {
        var actor = await acceso.ExigirAsync("Admin", "Supervisor", "Cajero");
        await using var db = new PuntoAbaDbContext(opciones);
        var c = await db.ConteosFisicos.AsNoTracking().Include(c => c.Detalles).ThenInclude(d => d.Producto)
            .FirstOrDefaultAsync(c => c.Id == id);
        if (c == null) return null;
        if (actor.Rol == "Cajero" && c.ResponsableId != actor.Id) throw new UnauthorizedAccessException("Solo puedes consultar tus propios conteos.");
        return await MapearConteo(db, c);
    }
    private static async Task<ConteoDto> MapearConteo(PuntoAbaDbContext db, ConteoFisico c) => new()
    {
        Id = c.Id, Fecha = c.FechaInicio, Estado = c.Estado, Version = c.Version,
        Responsable = await db.Usuarios.Where(u => u.Id == c.ResponsableId).Select(u => u.NombreCompleto).FirstAsync(),
        AutorizadoPor = c.AutorizadoPorId.HasValue ? await db.Usuarios.Where(u => u.Id == c.AutorizadoPorId).Select(u => u.NombreCompleto).FirstOrDefaultAsync() : null,
        Detalles = c.Detalles.Select(d => new LineaConteoDto { ProductoId = d.ProductoId, Codigo = d.Producto.Codigo, Nombre = d.Producto.Nombre, EsPorPeso = d.Producto.EsPorPeso, Sistema = d.CantidadSistema, Contada = d.CantidadContada, Motivo = d.Motivo }).ToList()
    };
    public async Task<ConteoDto> GuardarConteoAsync(ConteoDto dto)
    {
        var actor = await acceso.ExigirAsync("Admin", "Supervisor", "Cajero"); await using var db = new PuntoAbaDbContext(opciones);
        var c = await db.ConteosFisicos.Include(c => c.Detalles).ThenInclude(d => d.Producto).FirstOrDefaultAsync(c => c.Id == dto.Id) ?? throw new ArgumentException("No se encontró el conteo.");
        if (actor.Rol == "Cajero" && c.ResponsableId != actor.Id) throw new UnauthorizedAccessException();
        ValidarConteoPendiente(c, dto.Version);
        if (dto.Detalles.Count != c.Detalles.Count || dto.Detalles.Select(d => d.ProductoId).Distinct().Count() != dto.Detalles.Count) throw new ArgumentException("La lista de productos del conteo cambió.");
        foreach (var d in c.Detalles)
        {
            var captura = dto.Detalles.FirstOrDefault(x => x.ProductoId == d.ProductoId) ?? throw new ArgumentException("Falta un producto del conteo.");
            if (captura.Contada.HasValue) CantidadValida(d.Producto, captura.Contada.Value, true);
            if (captura.Motivo.Length > 400) throw new ArgumentException("El motivo admite hasta 400 caracteres.");
            d.CantidadContada = captura.Contada; d.Motivo = captura.Motivo.Trim();
        }
        c.Version++; await db.SaveChangesAsync(); return await MapearConteo(db, c);
    }
    private static void ValidarConteoPendiente(ConteoFisico c, long version)
    {
        if (c.Estado != "Pendiente") throw new ArgumentException("El conteo confirmado no se puede modificar.");
        if (c.Version != version) throw new ArgumentException("Otra persona actualizó este conteo. Vuelve a abrirlo.");
    }
    public async Task<ConteoDto> ActualizarBaseConteoAsync(int id, long version)
    {
        var actor = await acceso.ExigirAsync("Admin", "Supervisor", "Cajero"); await using var db = new PuntoAbaDbContext(opciones);
        var c = await db.ConteosFisicos.Include(c => c.Detalles).ThenInclude(d => d.Producto).FirstOrDefaultAsync(c => c.Id == id) ?? throw new ArgumentException("No se encontró el conteo.");
        if (actor.Rol == "Cajero" && c.ResponsableId != actor.Id) throw new UnauthorizedAccessException();
        ValidarConteoPendiente(c, version);
        foreach (var d in c.Detalles) { d.CantidadSistema = d.Producto.Stock; d.VersionProducto = d.Producto.VersionInventario; d.CantidadContada = null; d.Motivo = ""; }
        c.FechaInicio = DateTime.UtcNow; c.Version++; await db.SaveChangesAsync(); return await MapearConteo(db, c);
    }
    public async Task<ConteoDto> ConfirmarConteoAsync(int id, long version)
    {
        var actor = await acceso.ExigirAsync("Admin", "Supervisor"); await using var db = new PuntoAbaDbContext(opciones); await using var tx = await db.Database.BeginTransactionAsync();
        var c = await db.ConteosFisicos.Include(c => c.Detalles).ThenInclude(d => d.Producto).FirstOrDefaultAsync(c => c.Id == id) ?? throw new ArgumentException("No se encontró el conteo.");
        if (c.Estado == "Confirmado") return await MapearConteo(db, c);
        ValidarConteoPendiente(c, version);
        foreach (var d in c.Detalles)
        {
            if (!d.CantidadContada.HasValue) throw new ArgumentException("Captura la cantidad contada de todos los productos y guarda el borrador.");
            if (!d.Producto.Activo || d.Producto.VersionInventario != d.VersionProducto || d.Producto.Stock != d.CantidadSistema) throw new ArgumentException($"Las existencias de {d.Producto.Nombre} cambiaron durante el conteo. Reinicia la comparación y cuenta de nuevo.");
            CantidadValida(d.Producto, d.CantidadContada.Value, true);
            var delta = d.CantidadContada.Value - d.CantidadSistema;
            if (delta == 0) continue;
            var motivo = MotivoValido(d.Motivo);
            db.MovimientosInventario.Add(new MovimientoInventario { ProductoId = d.ProductoId, TipoMovimiento = "Ajuste", Cantidad = Math.Abs(delta), StockAnterior = d.CantidadSistema, StockNuevo = d.CantidadContada.Value, Motivo = "Conteo físico: " + motivo, Referencia = $"C-{c.Id:D6}", UsuarioId = actor.Id, CostoUnitario = d.Producto.PrecioCompra });
            d.Producto.Stock = d.CantidadContada.Value; d.Producto.VersionInventario++; d.Producto.FechaActualizacion = d.Producto.UltimaSincronizacion = DateTime.UtcNow;
        }
        c.Estado = "Confirmado"; c.AutorizadoPorId = actor.Id; c.FechaConfirmacion = DateTime.UtcNow; c.Version++;
        await db.SaveChangesAsync(); await tx.CommitAsync(); return await MapearConteo(db, c);
    }
    public async Task<ReporteInventarioDto> ReporteAsync(DateTime desde, DateTime hasta)
    {
        await acceso.ExigirAsync("Admin", "Supervisor");
        if (desde.Date > hasta.Date) throw new ArgumentException("Revisa el periodo del reporte.");
        var inicio = desde.Date.ToUniversalTime(); var fin = hasta.Date.AddDays(1).ToUniversalTime();
        await using var db = new PuntoAbaDbContext(opciones);
        var ps = await db.Productos.AsNoTracking().Include(p => p.Categoria).ToListAsync();
        var ms = await db.MovimientosInventario.AsNoTracking().Include(m => m.Producto).Include(m => m.Usuario).Where(m => m.FechaMovimiento < fin).OrderBy(m => m.FechaMovimiento).ThenBy(m => m.Id).ToListAsync();
        var r = new ReporteInventarioDto();
        foreach (var p in ps.Where(p => p.Activo))
        {
            var pm = ms.Where(m => m.ProductoId == p.Id).ToList(); var periodo = pm.Where(m => m.FechaMovimiento >= inicio).ToList();
            var inicial = pm.LastOrDefault(m => m.FechaMovimiento < inicio)?.StockNuevo ?? periodo.FirstOrDefault()?.StockAnterior ?? p.Stock;
            var final = pm.LastOrDefault()?.StockNuevo ?? p.Stock;
            var vendido = periodo.Where(m => m.Motivo == "Venta").Sum(m => m.Cantidad);
            var promedio = (inicial + final) / 2;
            r.Productos.Add(new() { Codigo = p.Codigo, Nombre = p.Nombre, Categoria = p.Categoria?.Nombre ?? "", Proveedor = p.ProveedorHabitual ?? "", Stock = p.Stock, Minimo = p.StockMinimo, Valor = decimal.Round(p.Stock * p.PrecioCompra, 2), Vendido = vendido, Rotacion = promedio > 0 ? vendido / promedio : null });
        }
        var porId = ms.ToDictionary(m => m.Id);
        string TipoOriginal(MovimientoInventario m)
        {
            var vistos = new HashSet<int>();
            while (m.MovimientoOriginalId.HasValue && vistos.Add(m.Id) && porId.TryGetValue(m.MovimientoOriginalId.Value, out var anterior)) m = anterior;
            return m.TipoMovimiento;
        }
        var eventos = ms.Where(m => m.FechaMovimiento >= inicio && (TipoOriginal(m) == "Merma" || TipoOriginal(m).StartsWith("Devolucion"))).ToList();
        r.MermasDevoluciones = eventos.OrderByDescending(m => m.FechaMovimiento).Select(Mapear).ToList();
        r.CostoMermas = eventos.Where(m => TipoOriginal(m) == "Merma").Sum(m => (m.StockAnterior - m.StockNuevo) * (m.CostoUnitario ?? m.Producto!.PrecioCompra));
        r.HayCostosEstimados = eventos.Any(m => m.TipoMovimiento == "Merma" && m.CostoUnitario == null); return r;
    }
}
