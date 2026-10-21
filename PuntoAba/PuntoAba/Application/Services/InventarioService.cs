using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using PuntoAba.Data;
using PuntoAba.DTOs;
using PuntoAba.Models;

namespace PuntoAba.Services;

public class InventarioService(DbContextOptions<PuntoAbaDbContext> opciones, IAccesoOperacion acceso) : IInventarioService
{
    public async Task<EntradaMercanciaDto> RegistrarEntradaAsync(CrearEntradaMercanciaDto dto, int? usuarioId = null)
    {
        var actor = await acceso.ExigirAsync("Admin", "Supervisor");
        usuarioId = actor.Id;
        Validator.ValidateObject(dto, new ValidationContext(dto), true);
        if (dto.OperacionId == Guid.Empty) throw new ArgumentException("La entrada necesita un identificador válido.");
        if (dto.Detalles.Count == 0) throw new ArgumentException("Agrega al menos un producto a la entrada.");
        if (dto.Detalles.Select(d => d.ProductoId).Distinct().Count() != dto.Detalles.Count)
            throw new ArgumentException("Hay productos repetidos. Usa una sola línea por producto.");

        await using var db = new PuntoAbaDbContext(opciones);
        await using var transaccion = await db.Database.BeginTransactionAsync();
        // El mismo intento se puede reenviar sin duplicar existencias.
        var existente = await db.EntradasMercancia.Include(e => e.Detalles).ThenInclude(d => d.Producto)
            .AsNoTracking().FirstOrDefaultAsync(e => e.OperacionId == dto.OperacionId);
        if (existente != null) { if (existente.UsuarioId != actor.Id) throw new UnauthorizedAccessException(); return MapearEntrada(existente); }

        if (usuarioId.HasValue && !await db.Usuarios.AnyAsync(u => u.Id == usuarioId.Value && u.Activo))
            throw new ArgumentException("El usuario de la entrada no está disponible.");
        var ids = dto.Detalles.Select(d => d.ProductoId).ToList();
        var productos = await db.Productos.Where(p => ids.Contains(p.Id) && p.Activo).ToDictionaryAsync(p => p.Id);
        var entrada = new EntradaMercancia
        {
            OperacionId = dto.OperacionId, Fecha = DateTime.UtcNow, UsuarioId = usuarioId,
            Proveedor = dto.Proveedor?.Trim(), Documento = dto.Documento?.Trim(), Notas = dto.Notas?.Trim()
        };
        foreach (var linea in dto.Detalles)
        {
            if (!productos.TryGetValue(linea.ProductoId, out var producto))
                throw new ArgumentException("Uno de los productos ya no existe o está inactivo. Actualiza el catálogo.");
            if (linea.StockEsperado.HasValue && linea.StockEsperado != producto.Stock) throw new ArgumentException($"Cambiaron las existencias de {producto.Nombre}. Pulsa Actualizar existencias y revisa el resultado antes de confirmar.");
            if (linea.PorCaja && (producto.EsPorPeso || linea.PiezasPorCaja <= 0 || linea.PiezasPorCaja > 100000 || decimal.Truncate(linea.Cantidad) != linea.Cantidad)) throw new ArgumentException("La recepción por cajas requiere un producto por pieza, cajas enteras y una cantidad válida de piezas por caja.");
            var cantidadBase = linea.CantidadBase;
            ControlInventarioService.CantidadValida(producto, cantidadBase);
            if (linea.Cantidad <= 0 || linea.Cantidad > 99999999.999m || decimal.Round(linea.Cantidad, 3) != linea.Cantidad)
                throw new ArgumentException($"Ingresa una cantidad mayor a cero y de hasta tres decimales para {producto.Nombre}.");
            if (!producto.EsPorPeso && decimal.Truncate(linea.Cantidad) != linea.Cantidad)
                throw new ArgumentException($"{producto.Nombre} se recibe en unidades enteras.");
            if (linea.CostoUnitario < 0 || linea.CostoUnitario > 99999999.99m || decimal.Round(linea.CostoUnitario, 2) != linea.CostoUnitario)
                throw new ArgumentException($"Revisa el costo de {producto.Nombre}; usa hasta dos decimales y un valor no negativo.");
            if (producto.Stock + cantidadBase > 999999999999999.999m)
                throw new ArgumentException($"La cantidad excede el límite de existencias de {producto.Nombre}.");
            entrada.Detalles.Add(new DetalleEntradaMercancia
            {
                ProductoId = producto.Id, Producto = producto, Cantidad = cantidadBase,
                CajasRecibidas = linea.PorCaja ? linea.Cantidad : null, PiezasPorCaja = linea.PorCaja ? linea.PiezasPorCaja : null,
                StockAnterior = producto.Stock, StockNuevo = producto.Stock + cantidadBase,
                CostoUnitario = linea.CostoUnitario, Importe = decimal.Round(cantidadBase * linea.CostoUnitario, 2, MidpointRounding.AwayFromZero)
            });
        }
        entrada.Total = entrada.Detalles.Sum(d => d.Importe);
        if (entrada.Total > 9999999999999999.99m)
            throw new ArgumentException("El importe total de la entrada excede el límite permitido.");
        db.EntradasMercancia.Add(entrada);
        await db.SaveChangesAsync();
        foreach (var linea in entrada.Detalles)
        {
            var producto = linea.Producto;
            var anterior = producto.Stock;
            producto.Stock += linea.Cantidad;
            producto.VersionInventario++;
            producto.FechaActualizacion = producto.UltimaSincronizacion = entrada.Fecha;
            if (dto.ActualizarPrecioCompra) producto.PrecioCompra = linea.CostoUnitario;
            db.MovimientosInventario.Add(new MovimientoInventario
            {
                ProductoId = producto.Id, TipoMovimiento = "Entrada", Cantidad = linea.Cantidad,
                StockAnterior = anterior, StockNuevo = producto.Stock, FechaMovimiento = entrada.Fecha,
                Motivo = "Recepción de mercancía", Referencia = $"E-{entrada.Id:D6}", UsuarioId = usuarioId, CostoUnitario = linea.CostoUnitario
            });
        }
        await db.SaveChangesAsync();
        await transaccion.CommitAsync();
        return MapearEntrada(entrada);
    }

    public async Task<List<EntradaMercanciaDto>> ObtenerEntradasAsync()
    {
        await acceso.ExigirAsync("Admin", "Supervisor");
        await using var db = new PuntoAbaDbContext(opciones);
        var entradas = await db.EntradasMercancia.AsNoTracking().Include(e => e.Detalles).ThenInclude(d => d.Producto)
            .OrderByDescending(e => e.Fecha).ThenByDescending(e => e.Id).Take(50).ToListAsync();
        return entradas.Select(MapearEntrada).ToList();
    }

    public async Task<PaginaMovimientosDto> ObtenerMovimientosAsync(FiltroMovimientosDto filtro)
    {
        await acceso.ExigirAsync("Admin", "Supervisor");
        if (filtro.Desde.HasValue && filtro.Hasta.HasValue && filtro.Desde.Value.Date > filtro.Hasta.Value.Date)
            throw new ArgumentException("La fecha inicial debe ser anterior o igual a la fecha final.");
        await using var db = new PuntoAbaDbContext(opciones);
        var consulta = db.MovimientosInventario.AsNoTracking().AsQueryable();
        if (filtro.SoloMermasDevoluciones) consulta = consulta.Where(m => m.TipoMovimiento == "Merma" || m.TipoMovimiento == "DevolucionCliente" || m.TipoMovimiento == "DevolucionNoVendible" || m.TipoMovimiento == "DevolucionProveedor");
        if (filtro.Desde.HasValue)
        {
            var desde = filtro.Desde.Value.Date.ToUniversalTime();
            consulta = consulta.Where(m => m.FechaMovimiento >= desde);
        }
        if (filtro.Hasta.HasValue)
        {
            var hasta = filtro.Hasta.Value.Date.AddDays(1).ToUniversalTime();
            consulta = consulta.Where(m => m.FechaMovimiento < hasta);
        }
        if (!string.IsNullOrWhiteSpace(filtro.Tipo)) consulta = consulta.Where(m => m.TipoMovimiento == filtro.Tipo);
        if (!string.IsNullOrWhiteSpace(filtro.Buscar))
        {
            var texto = filtro.Buscar.Trim();
            consulta = consulta.Where(m => m.Producto!.Codigo.Contains(texto) || m.Producto.Nombre.Contains(texto)
                || (m.Referencia != null && m.Referencia.Contains(texto)));
        }
        var total = await consulta.CountAsync();
        var tamano = Math.Clamp(filtro.TamanoPagina, 1, 100);
        var pagina = Math.Max(1, filtro.Pagina);
        var movimientos = await consulta.OrderByDescending(m => m.FechaMovimiento).ThenByDescending(m => m.Id)
            .Skip((pagina - 1) * tamano).Take(tamano)
            .Select(m => new MovimientoInventarioDto
            {
                Id = m.Id, Fecha = m.FechaMovimiento, Codigo = m.Producto!.Codigo, Producto = m.Producto.Nombre,
                Tipo = m.TipoMovimiento, Cantidad = m.Cantidad, StockAnterior = m.StockAnterior, StockNuevo = m.StockNuevo,
                Motivo = m.Motivo, Referencia = m.Referencia, Usuario = m.Usuario != null ? m.Usuario.NombreCompleto : null, MovimientoOriginalId = m.MovimientoOriginalId
            }).ToListAsync();
        return new(movimientos, total);
    }

    public async Task<List<ProductoResurtidoDto>> ObtenerResurtidoAsync()
    {
        await acceso.ExigirAsync("Admin", "Supervisor");
        await using var db = new PuntoAbaDbContext(opciones);
        var productos = await db.Productos.AsNoTracking().Include(p => p.Categoria)
            .Where(p => p.Activo && p.Stock <= p.StockMinimo).ToListAsync();
        return productos.OrderBy(p => p.Stock).ThenBy(p => p.Nombre).Select(p =>
        {
            var objetivo = p.StockObjetivo > 0 ? p.StockObjetivo : Math.Max(p.StockMinimo * 2, 1m);
            var cantidad = Math.Max(0, objetivo - p.Stock);
            if (!p.EsPorPeso) cantidad = decimal.Ceiling(cantidad);
            return new ProductoResurtidoDto
            {
                ProductoId = p.Id, Codigo = p.Codigo, Nombre = p.Nombre, Categoria = p.Categoria?.Nombre ?? "", Proveedor = p.ProveedorHabitual ?? "",
                EsPorPeso = p.EsPorPeso, UnidadMedida = p.UnidadMedida ?? "Pieza", Stock = p.Stock,
                Minimo = p.StockMinimo, Objetivo = objetivo, Cantidad = cantidad, PrecioCompra = p.PrecioCompra
            };
        }).ToList();
    }

    private static EntradaMercanciaDto MapearEntrada(EntradaMercancia e) => new()
    {
        Id = e.Id, Fecha = e.Fecha, Proveedor = e.Proveedor, Documento = e.Documento, Notas = e.Notas, Total = e.Total,
        Detalles = e.Detalles.Select(d => new LineaEntradaMercanciaDto
        {
            ProductoId = d.ProductoId, Codigo = d.Producto.Codigo, Nombre = d.Producto.Nombre,
            EsPorPeso = d.Producto.EsPorPeso, UnidadMedida = d.Producto.UnidadMedida ?? "Pieza",
            Cantidad = d.Cantidad, CostoUnitario = d.CostoUnitario, StockEsperado = d.StockAnterior
        }).ToList()
    };
}
