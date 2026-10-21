using Microsoft.EntityFrameworkCore;
using PuntoAba.Data;
using PuntoAba.DTOs;
namespace PuntoAba.Services;

public class DashboardService(DbContextOptions<PuntoAbaDbContext> opciones, IAccesoOperacion acceso) : IDashboardService
{
    public async Task<EstadisticasDashboard> ObtenerEstadisticasAsync()
    {
        await acceso.ExigirAsync("Admin", "Supervisor");
        await using var db = new PuntoAbaDbContext(opciones);
        var hoy = DateTime.Today; var inicio = new DateTime(hoy.Year, hoy.Month, 1); var fin = inicio.AddMonths(1);
        var ventas = await db.Ventas.AsNoTracking().Where(v => v.FechaVenta >= inicio && v.FechaVenta < fin).Select(v => new { v.Total, v.FechaVenta, v.ClienteId }).ToListAsync();
        var inventario = await db.Productos.AsNoTracking().Where(p => p.Activo).Select(p => new { p.Stock, p.StockMinimo, p.PrecioCompra }).ToListAsync();
        var detalles = await db.DetallesVenta.AsNoTracking().Where(d => d.Venta!.FechaVenta >= inicio && d.Venta.FechaVenta < fin)
            .Select(d => new { d.ProductoId, Nombre = d.Producto!.Nombre, d.Cantidad, d.Subtotal }).ToListAsync();
        var ultimas = await db.Ventas.AsNoTracking().OrderByDescending(v => v.FechaVenta).ThenByDescending(v => v.Id).Take(5)
            .Select(v => new VentaRecienteInicioDto(v.Id, v.FechaVenta, v.NumeroVenta, v.Total)).ToListAsync();
        var clientes = await db.Clientes.AsNoTracking().Where(c => c.Activo && c.Id != 1).Select(c => new { c.Id, c.Nombre, c.Apellido }).ToListAsync();
        var ranking = ventas.Where(v => v.ClienteId.HasValue && v.ClienteId != 1).GroupBy(v => v.ClienteId!.Value)
            .Select(g => new { Id = g.Key, Total = g.Sum(v => v.Total) }).OrderByDescending(x => x.Total).ThenBy(x => x.Id)
            .Join(clientes, x => x.Id, c => c.Id, (x, c) => new ClienteFrecuenteInicioDto(c.Id, (c.Nombre + " " + c.Apellido).Trim(), x.Total)).Take(3).ToList();
        return new()
        {
            VentasRecientes = ultimas, ClientesFrecuentes = ranking,
            VentasHoy = new() { Cantidad = ventas.Count(v => v.FechaVenta >= hoy && v.FechaVenta < hoy.AddDays(1)), Total = ventas.Where(v => v.FechaVenta >= hoy && v.FechaVenta < hoy.AddDays(1)).Sum(v => v.Total) },
            VentasMes = new() { Cantidad = ventas.Count, Total = ventas.Sum(v => v.Total) },
            Inventario = new() { Agotados = inventario.Count(p => p.Stock <= 0), StockBajoSinAgotados = inventario.Count(p => p.Stock > 0 && p.Stock <= p.StockMinimo), TotalProductos = inventario.Count, ProductosStockBajo = inventario.Count(p => p.Stock <= p.StockMinimo), ValorInventario = inventario.Sum(p => p.Stock * p.PrecioCompra) },
            Clientes = new() { Registrados = await db.Clientes.CountAsync(c => c.Activo && c.Id != 1), Total = await db.Clientes.CountAsync(c => c.Activo), NuevosEsteMes = await db.Clientes.CountAsync(c => c.FechaCreacion >= inicio && c.FechaCreacion < fin) },
            ProductosTop = detalles.GroupBy(d => new { d.ProductoId, d.Nombre }).Select(g => new ProductoTop { ProductoId = g.Key.ProductoId, Nombre = g.Key.Nombre,
                CantidadVendida = g.Sum(d => d.Cantidad), TotalVentas = g.Sum(d => d.Subtotal) }).OrderByDescending(p => p.CantidadVendida).Take(5).ToList()
        };
    }
    public async Task<List<VentasDiaDto>> ObtenerVentasPorDiaAsync(int dias = 7)
    {
        await acceso.ExigirAsync("Admin", "Supervisor");
        if (dias < 1 || dias > 366) throw new ArgumentException("El periodo admite entre 1 y 366 días.");
        await using var db = new PuntoAbaDbContext(opciones);
        var desde = DateTime.Today.AddDays(-dias);
        var ventas = await db.Ventas.AsNoTracking().Where(v => v.FechaVenta >= desde).Select(v => new { v.FechaVenta, v.Total }).ToListAsync();
        return ventas.GroupBy(v => v.FechaVenta.Date).OrderBy(g => g.Key).Select(g => new VentasDiaDto(g.Key, g.Sum(v => v.Total), g.Count())).ToList();
    }
    public async Task<List<VentasMetodoDto>> ObtenerVentasPorMetodoAsync()
    {
        await acceso.ExigirAsync("Admin", "Supervisor");
        await using var db = new PuntoAbaDbContext(opciones);
        var inicio = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1); var fin = inicio.AddMonths(1);
        var ventas = await db.Ventas.AsNoTracking().Where(v => v.FechaVenta >= inicio && v.FechaVenta < fin).Select(v => new { v.MetodoPago, v.Total }).ToListAsync();
        return ventas.GroupBy(v => v.MetodoPago).Select(g => new VentasMetodoDto(g.Key, g.Sum(v => v.Total), g.Count())).ToList();
    }
}
