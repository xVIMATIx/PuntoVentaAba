using Microsoft.EntityFrameworkCore;
using PuntoAba.Data;
using PuntoAba.DTOs;
using PuntoAba.Models;

namespace PuntoAba.Services
{
    public class VentaService : IVentaService
    {
        private readonly PuntoAbaDbContext _context;
        private readonly IAccesoOperacion _acceso;

        public VentaService(PuntoAbaDbContext context, IAccesoOperacion acceso)
        {
            _context = context;
            _acceso = acceso;
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
            var actor = await _acceso.ExigirAsync("Admin", "Supervisor", "Cajero"); dto.UsuarioId = actor.Id;
            if (dto.OperacionId == Guid.Empty || dto.Detalles.Count == 0) throw new ArgumentException("La venta requiere productos y un identificador válido.");
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var existente = await _context.Ventas.AsNoTracking().FirstOrDefaultAsync(v => v.OperacionId == dto.OperacionId);
                if (existente != null) { if (existente.UsuarioId != actor.Id) throw new UnauthorizedAccessException(); return (await ObtenerPorIdAsync(existente.Id))!; }
                // Generar número de venta
                var numeroVenta = await GenerarNumeroVentaAsync();

                // Calcular totales
                decimal subtotal = 0;
                var detalles = new List<DetalleVenta>();
                var productosActualizados = new HashSet<int>();

                foreach (var detalleDto in dto.Detalles)
                {
                    var producto = await _context.Productos.FindAsync(detalleDto.ProductoId);
                    if (producto == null)
                        throw new ArgumentException($"Producto con ID {detalleDto.ProductoId} no encontrado");
                    if (productosActualizados.Add(producto.Id))
                        await _context.Entry(producto).ReloadAsync();
                    if (!producto.Activo) throw new ArgumentException("El producto está inactivo.");
                    ControlInventarioService.CantidadValida(producto, detalleDto.Cantidad);

                    if (detalleDto.Cantidad <= 0)
                        throw new ArgumentException($"La cantidad del producto {producto.Nombre} debe ser mayor a cero");

                    if (!producto.EsPorPeso && detalleDto.Cantidad != Math.Truncate(detalleDto.Cantidad))
                        throw new ArgumentException($"El producto {producto.Nombre} se vende por unidad y requiere cantidades enteras");

                    // La venta puede dejar stock negativo para reflejar diferencias de captura.
                    // Conservamos el saldo real y su movimiento, sin ajustar existencias a cero.

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
                    producto.VersionInventario++;
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
                        UsuarioId = dto.UsuarioId,
                        CostoUnitario = producto.PrecioCompra,
                        Referencia = numeroVenta
                    };
                    _context.MovimientosInventario.Add(movimiento);
                }

                var total = subtotal - dto.Descuento + dto.Impuesto;
                if (dto.Descuento < 0 || dto.Descuento > subtotal || dto.Impuesto < 0 || total < 0)
                    throw new ArgumentException("Revisa los importes de la venta.");
                decimal? recibido = null, cambio = null;
                if (string.Equals(dto.MetodoPago, "Efectivo", StringComparison.OrdinalIgnoreCase))
                {
                    var totalEfectivo = decimal.Round(total, 2, MidpointRounding.AwayFromZero);
                    if (!dto.Recibido.HasValue || dto.Recibido.Value < 0 || dto.Recibido.Value > 9999999999999999.99m
                        || decimal.Round(dto.Recibido.Value, 2) != dto.Recibido.Value || dto.Recibido.Value < totalEfectivo)
                        throw new ArgumentException("Captura un efectivo recibido válido que cubra el total, con hasta dos decimales.");
                    recibido = dto.Recibido.Value;
                    cambio = recibido.Value - totalEfectivo;
                }
                else if (dto.Recibido.HasValue)
                    throw new ArgumentException("El efectivo recibido solo corresponde al método Efectivo.");

                var venta = new Venta
                {
                    OperacionId = dto.OperacionId,
                    NumeroVenta = numeroVenta,
                    FechaVenta = DateTime.Now,
                    ClienteId = dto.ClienteId,
                    UsuarioId = dto.UsuarioId,
                    Subtotal = subtotal,
                    Descuento = dto.Descuento,
                    Impuesto = dto.Impuesto,
                    Total = total,
                    Recibido = recibido,
                    Cambio = cambio,
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
                _context.ChangeTracker.Clear();
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
                Recibido = venta.Recibido,
                Cambio = venta.Cambio,
                Notas = venta.Notas,
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
                    UnidadMedida = d.Producto?.UnidadMedida,
                    EsPorPeso = d.Producto?.EsPorPeso ?? false
                }).ToList()
            };
        }
    }
}
