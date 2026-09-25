using Microsoft.EntityFrameworkCore;
using PuntoAba.Data;
using PuntoAba.DTOs;
using PuntoAba.Models;

namespace PuntoAba.Services
{
    public class SincronizacionService : ISincronizacionService
    {
        private readonly PuntoAbaDbContext _context;
        private readonly IProductoService _productoService;
        private readonly IVentaService _ventaService;

        public SincronizacionService(
            PuntoAbaDbContext context,
            IProductoService productoService,
            IVentaService ventaService)
        {
            _context = context;
            _productoService = productoService;
            _ventaService = ventaService;
        }

        public async Task<SincronizacionResponseDto> SincronizarDatosAsync(SincronizacionDto datos)
        {
            var response = new SincronizacionResponseDto
            {
                FechaSincronizacion = DateTime.UtcNow
            };

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Sincronizar productos
                if (datos.Productos != null)
                {
                    foreach (var productoDto in datos.Productos)
                    {
                        await SincronizarProductoAsync(productoDto);
                        response.ProductosSincronizados++;
                    }
                }

                // Sincronizar clientes
                if (datos.Clientes != null)
                {
                    foreach (var clienteDto in datos.Clientes)
                    {
                        await SincronizarClienteAsync(clienteDto);
                        response.ClientesSincronizados++;
                    }
                }

                // Sincronizar ventas
                if (datos.Ventas != null)
                {
                    foreach (var ventaDto in datos.Ventas)
                    {
                        await SincronizarVentaAsync(ventaDto);
                        response.VentasSincronizadas++;
                    }
                }

                // Registrar sincronización
                var registro = new RegistroSincronizacion
                {
                    Entidad = "General",
                    EntidadId = 0,
                    TipoOperacion = "SYNC",
                    FechaSincronizacion = DateTime.UtcNow,
                    DispositivoOrigen = datos.DispositivoOrigen,
                    Exitoso = true
                };
                _context.RegistrosSincronizacion.Add(registro);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                response.Exitoso = true;
                response.Mensaje = "Sincronización completada exitosamente";
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                response.Exitoso = false;
                response.Mensaje = $"Error durante la sincronización: {ex.Message}";
            }

            return response;
        }

        public async Task<SincronizacionDto> ObtenerDatosParaSincronizarAsync(DateTime? ultimaSincronizacion)
        {
            var datos = new SincronizacionDto
            {
                FechaSincronizacion = DateTime.UtcNow,
                DispositivoOrigen = "API"
            };

            // Obtener productos actualizados
            var productosQuery = _context.Productos
                .Include(p => p.Categoria)
                .Where(p => p.Activo);

            if (ultimaSincronizacion.HasValue)
                productosQuery = productosQuery.Where(p => p.UltimaSincronizacion > ultimaSincronizacion.Value);

            datos.Productos = await productosQuery
                .Select(p => new ProductoDto
                {
                    Id = p.Id,
                    Codigo = p.Codigo,
                    Nombre = p.Nombre,
                    Descripcion = p.Descripcion,
                    PrecioCompra = p.PrecioCompra,
                    PrecioVenta = p.PrecioVenta,
                    Stock = p.Stock,
                    StockMinimo = p.StockMinimo,
                    CategoriaId = p.CategoriaId,
                    CategoriaNombre = p.Categoria!.Nombre,
                    UnidadMedida = p.UnidadMedida,
                    ImagenUrl = p.ImagenUrl,
                    Activo = p.Activo,
                    EsPorPeso = p.EsPorPeso
                })
                .ToListAsync();

            // Obtener clientes
            var clientesQuery = _context.Clientes.Where(c => c.Activo);

            if (ultimaSincronizacion.HasValue)
                clientesQuery = clientesQuery.Where(c => c.FechaActualizacion > ultimaSincronizacion.Value 
                    || (c.FechaActualizacion == null && c.FechaCreacion > ultimaSincronizacion.Value));

            datos.Clientes = await clientesQuery
                .Select(c => new ClienteDto
                {
                    Id = c.Id,
                    Nombre = c.Nombre,
                    Apellido = c.Apellido,
                    Telefono = c.Telefono,
                    Email = c.Email,
                    Direccion = c.Direccion,
                    DocumentoIdentidad = c.DocumentoIdentidad,
                    Activo = c.Activo
                })
                .ToListAsync();

            return datos;
        }

        public async Task<EstadoSincronizacionDto> ObtenerEstadoSincronizacionAsync()
        {
            var ultimaSincronizacion = await _context.RegistrosSincronizacion
                .OrderByDescending(r => r.FechaSincronizacion)
                .FirstOrDefaultAsync();

            var ventasSinSincronizar = await _context.Ventas
                .CountAsync(v => !v.Sincronizado);

            return new EstadoSincronizacionDto
            {
                UltimaSincronizacion = ultimaSincronizacion?.FechaSincronizacion,
                RegistrosPendientes = ventasSinSincronizar,
                HayCambiosPendientes = ventasSinSincronizar > 0
            };
        }

        private async Task SincronizarProductoAsync(ProductoDto dto)
        {
            var producto = await _context.Productos
                .FirstOrDefaultAsync(p => p.Id == dto.Id || p.Codigo == dto.Codigo);

            if (producto == null)
            {
                // Crear nuevo producto
                producto = new Producto
                {
                    Codigo = dto.Codigo,
                    Nombre = dto.Nombre,
                    Descripcion = dto.Descripcion,
                    PrecioCompra = dto.PrecioCompra,
                    PrecioVenta = dto.PrecioVenta,
                    Stock = dto.Stock,
                    StockMinimo = dto.StockMinimo,
                    CategoriaId = dto.CategoriaId,
                    UnidadMedida = dto.UnidadMedida,
                    EsPorPeso = dto.EsPorPeso,
                    ImagenUrl = dto.ImagenUrl,
                    Activo = dto.Activo,
                    FechaCreacion = DateTime.UtcNow,
                    UltimaSincronizacion = DateTime.UtcNow
                };
                _context.Productos.Add(producto);
            }
            else
            {
                // Actualizar producto existente
                producto.Nombre = dto.Nombre;
                producto.Descripcion = dto.Descripcion;
                producto.PrecioCompra = dto.PrecioCompra;
                producto.PrecioVenta = dto.PrecioVenta;
                producto.Stock = dto.Stock;
                producto.StockMinimo = dto.StockMinimo;
                producto.CategoriaId = dto.CategoriaId;
                producto.UnidadMedida = dto.UnidadMedida;
                producto.EsPorPeso = dto.EsPorPeso;
                producto.ImagenUrl = dto.ImagenUrl;
                producto.Activo = dto.Activo;
                producto.FechaActualizacion = DateTime.UtcNow;
                producto.UltimaSincronizacion = DateTime.UtcNow;
            }
        }

        private async Task SincronizarClienteAsync(ClienteDto dto)
        {
            var cliente = await _context.Clientes.FindAsync(dto.Id);

            if (cliente == null)
            {
                cliente = new Cliente
                {
                    Nombre = dto.Nombre,
                    Apellido = dto.Apellido,
                    Telefono = dto.Telefono,
                    Email = dto.Email,
                    Direccion = dto.Direccion,
                    DocumentoIdentidad = dto.DocumentoIdentidad,
                    Activo = dto.Activo,
                    FechaCreacion = DateTime.UtcNow
                };
                _context.Clientes.Add(cliente);
            }
            else
            {
                cliente.Nombre = dto.Nombre;
                cliente.Apellido = dto.Apellido;
                cliente.Telefono = dto.Telefono;
                cliente.Email = dto.Email;
                cliente.Direccion = dto.Direccion;
                cliente.DocumentoIdentidad = dto.DocumentoIdentidad;
                cliente.Activo = dto.Activo;
                cliente.FechaActualizacion = DateTime.UtcNow;
            }
        }

        private async Task SincronizarVentaAsync(VentaDto dto)
        {
            // Verificar si la venta ya existe
            var ventaExistente = await _context.Ventas
                .FirstOrDefaultAsync(v => v.NumeroVenta == dto.NumeroVenta);

            if (ventaExistente != null)
            {
                ventaExistente.Sincronizado = true;
                ventaExistente.UltimaSincronizacion = DateTime.UtcNow;
                return;
            }

            // Crear nueva venta desde datos sincronizados
            var venta = new Venta
            {
                NumeroVenta = dto.NumeroVenta,
                FechaVenta = dto.FechaVenta,
                ClienteId = dto.ClienteId,
                UsuarioId = 1, // Usuario por defecto para ventas sincronizadas
                Subtotal = dto.Subtotal,
                Descuento = dto.Descuento,
                Impuesto = dto.Impuesto,
                Total = dto.Total,
                MetodoPago = dto.MetodoPago,
                EstadoVenta = dto.EstadoVenta,
                Sincronizado = true,
                UltimaSincronizacion = DateTime.UtcNow,
                OrigenVenta = "SYNC"
            };

            _context.Ventas.Add(venta);
            await _context.SaveChangesAsync();

            // Agregar detalles
            foreach (var detalleDto in dto.Detalles)
            {
                var detalle = new DetalleVenta
                {
                    VentaId = venta.Id,
                    ProductoId = detalleDto.ProductoId,
                    Cantidad = detalleDto.Cantidad,
                    PrecioUnitario = detalleDto.PrecioUnitario,
                    Descuento = detalleDto.Descuento,
                    Subtotal = detalleDto.Subtotal
                };
                _context.DetallesVenta.Add(detalle);
            }
        }
    }
}
