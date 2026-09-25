using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PuntoAba.Data;

namespace PuntoAba.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin,Supervisor")]
    public class DashboardController : ControllerBase
    {
        private readonly PuntoAbaDbContext _context;
        private readonly ILogger<DashboardController> _logger;

        public DashboardController(PuntoAbaDbContext context, ILogger<DashboardController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet("estadisticas")]
        public async Task<ActionResult> ObtenerEstadisticas()
        {
            try
            {
                var hoy = DateTime.Today;
                var inicioMes = new DateTime(hoy.Year, hoy.Month, 1);
                var finMes = inicioMes.AddMonths(1);

                var estadisticas = new
                {
                    ventasHoy = new
                    {
                        total = await _context.Ventas
                            .Where(v => v.FechaVenta >= hoy && v.FechaVenta < hoy.AddDays(1))
                            .Select(v => (decimal?)v.Total)
                            .SumAsync() ?? 0m,
                        cantidad = await _context.Ventas
                            .CountAsync(v => v.FechaVenta >= hoy && v.FechaVenta < hoy.AddDays(1))
                    },
                    ventasMes = new
                    {
                        total = await _context.Ventas
                            .Where(v => v.FechaVenta >= inicioMes && v.FechaVenta < finMes)
                            .Select(v => (decimal?)v.Total)
                            .SumAsync() ?? 0m,
                        cantidad = await _context.Ventas
                            .CountAsync(v => v.FechaVenta >= inicioMes && v.FechaVenta < finMes)
                    },
                    inventario = new
                    {
                        totalProductos = await _context.Productos.CountAsync(p => p.Activo),
                        productosStockBajo = await _context.Productos
                            .CountAsync(p => p.Activo && p.Stock <= p.StockMinimo),
                        valorInventario = await _context.Productos
                            .Where(p => p.Activo)
                            .Select(p => (decimal?)(p.Stock * p.PrecioCompra))
                            .SumAsync() ?? 0m
                    },
                    clientes = new
                    {
                        total = await _context.Clientes.CountAsync(c => c.Activo),
                        nuevosEsteMes = await _context.Clientes
                            .CountAsync(c => c.FechaCreacion >= inicioMes && c.FechaCreacion < finMes)
                    },
                    productosTop = await _context.DetallesVenta
                        .Include(d => d.Producto)
                        .Where(d => d.Venta!.FechaVenta >= inicioMes && d.Venta.FechaVenta < finMes)
                        .GroupBy(d => new { d.ProductoId, d.Producto!.Nombre })
                        .Select(g => new
                        {
                            productoId = g.Key.ProductoId,
                            nombre = g.Key.Nombre,
                            cantidadVendida = g.Sum(d => d.Cantidad),
                            totalVentas = g.Sum(d => d.Subtotal)
                        })
                        .OrderByDescending(p => (double)p.cantidadVendida)
                        .Take(5)
                        .ToListAsync()
                };

                return Ok(estadisticas);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener estadísticas del dashboard");
                return StatusCode(500, "Error interno del servidor");
            }
        }

        [HttpGet("ventas-por-dia")]
        public async Task<ActionResult> ObtenerVentasPorDia([FromQuery] int dias = 7)
        {
            try
            {
                var fechaInicio = DateTime.Today.AddDays(-dias);
                
                var ventasPorDia = await _context.Ventas
                    .Where(v => v.FechaVenta >= fechaInicio)
                    .GroupBy(v => v.FechaVenta.Date)
                    .Select(g => new
                    {
                        fecha = g.Key,
                        total = g.Sum(v => v.Total),
                        cantidad = g.Count()
                    })
                    .OrderBy(v => v.fecha)
                    .ToListAsync();

                return Ok(ventasPorDia);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener ventas por día");
                return StatusCode(500, "Error interno del servidor");
            }
        }

        [HttpGet("ventas-por-metodo-pago")]
        public async Task<ActionResult> ObtenerVentasPorMetodoPago()
        {
            try
            {
                var inicioMes = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
                var finMes = inicioMes.AddMonths(1);

                var ventasPorMetodo = await _context.Ventas
                    .Where(v => v.FechaVenta >= inicioMes && v.FechaVenta < finMes)
                    .GroupBy(v => v.MetodoPago)
                    .Select(g => new
                    {
                        metodoPago = g.Key,
                        total = g.Sum(v => v.Total),
                        cantidad = g.Count()
                    })
                    .ToListAsync();

                return Ok(ventasPorMetodo);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener ventas por método de pago");
                return StatusCode(500, "Error interno del servidor");
            }
        }
    }
}
