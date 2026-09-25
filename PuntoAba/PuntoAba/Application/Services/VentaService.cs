using Microsoft.EntityFrameworkCore;
using PuntoAba.Data;
using PuntoAba.DTOs;
using PuntoAba.Models;

namespace PuntoAba.Services
{
    public class VentaService : IVentaService
    {
        private readonly PuntoAbaDbContext _context;

        public VentaService(PuntoAbaDbContext context)
        {
            _context = context;
        }

        public async Task<VentaDto?> ObtenerPorIdAsync(int id)
        {
            var venta = await _context.Ventas
                .Include(v => v.Cliente)
                .Include(v => v.Usuario)
                .Include(v => v.DetallesVenta)
                    .ThenInclude(d => d.Producto)
                .FirstOrDefaultAsync(v => v.Id == id);

            return venta != null ? MapToDto(venta) : null;
        }

        public async Task<IEnumerable<VentaDto>> ObtenerTodas(DateTime? fechaInicio = null, DateTime? fechaFin = null)
        {
            var query = _context.Ventas
                .Include(v => v.Cliente)
                .Include(v => v.Usuario)
                .Include(v => v.DetallesVenta)
                    .ThenInclude(d => d.Producto)
                .AsQueryable();

            if (fechaInicio.HasValue)
            {
                var inicio = fechaInicio.Value.Date;
                query = query.Where(v => v.FechaVenta >= inicio);
            }

            if (fechaFin.HasValue)
            {
                var finExclusivo = fechaFin.Value.Date.AddDays(1);
                query = query.Where(v => v.FechaVenta < finExclusivo);
            }

            var ventas = await query
                .OrderByDescending(v => v.FechaVenta)
                .ToListAsync();

            return ventas.Select(MapToDto);
        }

        public async Task<VentaDto> CrearAsync(CrearVentaDto dto)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Generar número de venta
                var numeroVenta = await GenerarNumeroVentaAsync();

                // Calcular totales
                decimal subtotal = 0;
                var detalles = new List<DetalleVenta>();

                foreach (var detalleDto in dto.Detalles)
                {
                    var producto = await _context.Productos.FindAsync(detalleDto.ProductoId);
                    if (producto == null)
                        throw new Exception($"Producto con ID {detalleDto.ProductoId} no encontrado");

                    if (detalleDto.Cantidad <= 0)
                        throw new Exception($"La cantidad del producto {producto.Nombre} debe ser mayor a cero");

                    if (!producto.EsPorPeso && detalleDto.Cantidad != Math.Truncate(detalleDto.Cantidad))
                        throw new Exception($"El producto {producto.Nombre} se vende por unidad y requiere cantidades enteras");

                    if (producto.Stock < detalleDto.Cantidad)
                        throw new Exception($"Stock insuficiente para el producto {producto.Nombre}");

                    var subtotalDetalle = (detalleDto.Cantidad * detalleDto.PrecioUnitario) - detalleDto.Descuento;
                    subtotal += subtotalDetalle;

                    var detalle = new DetalleVenta
                    {
                        ProductoId = detalleDto.ProductoId,
                        Cantidad = detalleDto.Cantidad,
                        PrecioUnitario = detalleDto.PrecioUnitario,
                        Descuento = detalleDto.Descuento,
                        Subtotal = subtotalDetalle
                    };

                    detalles.Add(detalle);

                    // Actualizar stock
                    producto.Stock -= detalleDto.Cantidad;
                    producto.FechaActualizacion = DateTime.UtcNow;

                    // Registrar movimiento de inventario
                    var movimiento = new MovimientoInventario
                    {
                        ProductoId = producto.Id,
                        TipoMovimiento = "Salida",
                        Cantidad = detalleDto.Cantidad,
                        StockAnterior = producto.Stock + detalleDto.Cantidad,
                        StockNuevo = producto.Stock,
                        FechaMovimiento = DateTime.UtcNow,
                        Motivo = "Venta",
                        Referencia = numeroVenta
                    };
                    _context.MovimientosInventario.Add(movimiento);
                }

                var total = subtotal - dto.Descuento + dto.Impuesto;

                var venta = new Venta
                {
                    NumeroVenta = numeroVenta,
                    FechaVenta = DateTime.Now,
                    ClienteId = dto.ClienteId,
                    UsuarioId = dto.UsuarioId,
                    Subtotal = subtotal,
                    Descuento = dto.Descuento,
                    Impuesto = dto.Impuesto,
                    Total = total,
                    MetodoPago = dto.MetodoPago,
                    EstadoVenta = "Completada",
                    Notas = dto.Notas,
                    DetallesVenta = detalles,
                    UltimaSincronizacion = DateTime.UtcNow,
                    Sincronizado = false,
                    OrigenVenta = "POS"
                };

                _context.Ventas.Add(venta);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return (await ObtenerPorIdAsync(venta.Id))!;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<ReporteVentasDto> ObtenerReporteAsync(DateTime fechaInicio, DateTime fechaFin)
        {
            var ventas = await ObtenerTodas(fechaInicio, fechaFin);
            var ventasList = ventas.ToList();

            return new ReporteVentasDto
            {
                FechaInicio = fechaInicio,
                FechaFin = fechaFin,
                TotalVentas = ventasList.Sum(v => v.Total),
                CantidadVentas = ventasList.Count,
                PromedioVenta = ventasList.Any() ? ventasList.Average(v => v.Total) : 0,
                Ventas = ventasList
            };
        }

        public async Task<IEnumerable<VentaDto>> ObtenerPorClienteAsync(int clienteId)
        {
            var ventas = await _context.Ventas
                .Include(v => v.Cliente)
                .Include(v => v.Usuario)
                .Include(v => v.DetallesVenta)
                    .ThenInclude(d => d.Producto)
                .Where(v => v.ClienteId == clienteId)
                .OrderByDescending(v => v.FechaVenta)
                .ToListAsync();

            return ventas.Select(MapToDto);
        }

        public async Task<IEnumerable<VentaDto>> ObtenerPorUsuarioAsync(int usuarioId)
        {
            var ventas = await _context.Ventas
                .Include(v => v.Cliente)
                .Include(v => v.Usuario)
                .Include(v => v.DetallesVenta)
                    .ThenInclude(d => d.Producto)
                .Where(v => v.UsuarioId == usuarioId)
                .OrderByDescending(v => v.FechaVenta)
                .ToListAsync();

            return ventas.Select(MapToDto);
        }

        public async Task<decimal> ObtenerTotalVentasDelDiaAsync()
        {
            var hoy = DateTime.Today;
            var manana = hoy.AddDays(1);

            return await _context.Ventas
                .Where(v => v.FechaVenta >= hoy && v.FechaVenta < manana)
                .Select(v => (decimal?)v.Total)
                .SumAsync() ?? 0m;
        }

        private async Task<string> GenerarNumeroVentaAsync()
        {
            var hoy = DateTime.Today;
            var prefijo = $"V{hoy:yyyyMMdd}";
            
            var ultimaVenta = await _context.Ventas
                .Where(v => v.NumeroVenta.StartsWith(prefijo))
                .OrderByDescending(v => v.NumeroVenta)
                .FirstOrDefaultAsync();

            int siguiente = 1;
            if (ultimaVenta != null)
            {
                var numero = ultimaVenta.NumeroVenta.Substring(prefijo.Length);
                if (int.TryParse(numero, out int ultimoNumero))
                    siguiente = ultimoNumero + 1;
            }

            return $"{prefijo}{siguiente:D4}";
        }

        private VentaDto MapToDto(Venta venta)
        {
            return new VentaDto
            {
                Id = venta.Id,
                NumeroVenta = venta.NumeroVenta,
                FechaVenta = venta.FechaVenta,
                ClienteId = venta.ClienteId,
                ClienteNombre = venta.Cliente != null ? $"{venta.Cliente.Nombre} {venta.Cliente.Apellido}".Trim() : "Cliente General",
                UsuarioNombre = venta.Usuario?.NombreCompleto,
                Subtotal = venta.Subtotal,
                Descuento = venta.Descuento,
                Impuesto = venta.Impuesto,
                Total = venta.Total,
                MetodoPago = venta.MetodoPago,
                EstadoVenta = venta.EstadoVenta,
                Detalles = venta.DetallesVenta.Select(d => new DetalleVentaDto
                {
                    Id = d.Id,
                    ProductoId = d.ProductoId,
                    ProductoNombre = d.Producto?.Nombre ?? "",
                    ProductoCodigo = d.Producto?.Codigo,
                    Cantidad = d.Cantidad,
                    PrecioUnitario = d.PrecioUnitario,
                    Descuento = d.Descuento,
                    Subtotal = d.Subtotal,
                    EsPorPeso = d.Producto?.EsPorPeso ?? false
                }).ToList()
            };
        }
    }
}
