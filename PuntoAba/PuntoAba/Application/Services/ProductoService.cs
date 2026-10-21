using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using PuntoAba.Data;
using PuntoAba.DTOs;
using PuntoAba.Models;

namespace PuntoAba.Services
{
    public class ProductoService : IProductoService
    {
        private readonly PuntoAbaDbContext _context;
        private readonly IAccesoOperacion _acceso;

        public ProductoService(PuntoAbaDbContext context, IAccesoOperacion acceso)
        {
            _context = context;
            _acceso = acceso;
        }

        public async Task<IEnumerable<ProductoDto>> ObtenerTodosAsync()
        {
            var productos = await _context.Productos
                .AsNoTracking()
                .Include(p => p.Categoria)
                .Where(p => p.Activo)
                .ToListAsync();

            return productos.Select(MapToDto);
        }

        public async Task<ProductoDto?> ObtenerPorIdAsync(int id)
        {
            var producto = await _context.Productos
                .AsNoTracking()
                .Include(p => p.Categoria)
                .FirstOrDefaultAsync(p => p.Id == id);

            return producto != null ? MapToDto(producto) : null;
        }

        public async Task<ProductoDto?> ObtenerPorCodigoAsync(string codigo)
        {
            var producto = await _context.Productos
                .AsNoTracking()
                .Include(p => p.Categoria)
                .FirstOrDefaultAsync(p => p.Codigo == codigo);

            return producto != null ? MapToDto(producto) : null;
        }

        public async Task<ProductoDto> CrearAsync(CrearProductoDto dto)
        {
            var actor = await _acceso.ExigirAsync("Admin", "Supervisor");
            dto.Codigo = dto.Codigo?.Trim() ?? "";
            dto.Nombre = dto.Nombre?.Trim() ?? "";
            Validator.ValidateObject(dto, new ValidationContext(dto), validateAllProperties: true);
            await ValidarCodigoYCategoriaAsync(dto.Codigo, dto.CategoriaId);
            ValidarObjetivo(dto.StockObjetivo, dto.StockMinimo, dto.EsPorPeso);
            var producto = new Producto
            {
                Codigo = dto.Codigo,
                Nombre = dto.Nombre,
                Descripcion = dto.Descripcion,
                PrecioCompra = dto.PrecioCompra,
                PrecioVenta = dto.PrecioVenta,
                Stock = dto.Stock,
                StockMinimo = dto.StockMinimo,
                StockObjetivo = dto.StockObjetivo, ProveedorHabitual = dto.ProveedorHabitual?.Trim(),
                CategoriaId = dto.CategoriaId,
                UnidadMedida = dto.UnidadMedida,
                EsPorPeso = dto.EsPorPeso,
                ImagenUrl = dto.ImagenUrl,
                FechaCreacion = DateTime.UtcNow,
                UltimaSincronizacion = DateTime.UtcNow
            };

            ControlInventarioService.CantidadValida(producto, producto.Stock, true);
            _context.Productos.Add(producto);
            if (producto.Stock > 0)
            {
                _context.MovimientosInventario.Add(new MovimientoInventario
                {
                    Producto = producto, TipoMovimiento = "Entrada", Cantidad = producto.Stock,
                    StockAnterior = 0, StockNuevo = producto.Stock, Motivo = "Existencias iniciales", UsuarioId = actor.Id, CostoUnitario = producto.PrecioCompra
                });
            }
            await GuardarProductoAsync(producto);

            return (await ObtenerPorIdAsync(producto.Id))!;
        }

        public async Task<ProductoDto?> ActualizarAsync(int id, ActualizarProductoDto dto)
        {
            await _acceso.ExigirAsync("Admin", "Supervisor");
            var producto = await _context.Productos.FindAsync(id);
            if (producto == null) return null;
            await _context.Entry(producto).ReloadAsync();

            var codigo = dto.Codigo?.Trim() ?? producto.Codigo;
            await ValidarCodigoYCategoriaAsync(codigo, dto.CategoriaId ?? producto.CategoriaId, id);
            if (dto.Stock.HasValue && dto.Stock.Value < 0)
                throw new ArgumentException("Las existencias no pueden ser negativas.");
            if (dto.Stock.HasValue && dto.Stock.Value != producto.Stock)
                throw new ArgumentException("Cambia las existencias desde Entradas, Conteos o Ajustes con motivo y responsable.");
            ValidarObjetivo(dto.StockObjetivo ?? producto.StockObjetivo, dto.StockMinimo ?? producto.StockMinimo, dto.EsPorPeso ?? producto.EsPorPeso);
            if (((dto.EsPorPeso.HasValue && dto.EsPorPeso != producto.EsPorPeso) || (dto.UnidadMedida != null && !string.Equals(dto.UnidadMedida.Trim(), producto.UnidadMedida?.Trim(), StringComparison.OrdinalIgnoreCase))) && await _context.MovimientosInventario.AnyAsync(m => m.ProductoId == id))
                throw new ArgumentException("La unidad de un producto con movimientos no se puede cambiar. Conserva la unidad original o crea otro producto.");
            producto.Codigo = codigo;
            if (dto.Nombre != null) producto.Nombre = dto.Nombre;
            if (dto.Descripcion != null) producto.Descripcion = dto.Descripcion;
            if (dto.PrecioCompra.HasValue) producto.PrecioCompra = dto.PrecioCompra.Value;
            if (dto.PrecioVenta.HasValue) producto.PrecioVenta = dto.PrecioVenta.Value;
            if (dto.Stock.HasValue) producto.Stock = dto.Stock.Value;
            if (dto.StockMinimo.HasValue) producto.StockMinimo = dto.StockMinimo.Value;
            if (dto.StockObjetivo.HasValue) producto.StockObjetivo = dto.StockObjetivo.Value;
            if (dto.ProveedorHabitual != null) producto.ProveedorHabitual = dto.ProveedorHabitual.Trim();
            if (dto.CategoriaId.HasValue) producto.CategoriaId = dto.CategoriaId.Value;
            if (dto.UnidadMedida != null) producto.UnidadMedida = dto.UnidadMedida;
            if (dto.ImagenUrl != null) producto.ImagenUrl = dto.ImagenUrl;
            if (dto.Activo.HasValue) producto.Activo = dto.Activo.Value;
            if (dto.EsPorPeso.HasValue) producto.EsPorPeso = dto.EsPorPeso.Value;

            producto.FechaActualizacion = DateTime.UtcNow;
            producto.VersionInventario++;
            producto.UltimaSincronizacion = DateTime.UtcNow;

            await GuardarProductoAsync(producto);
            return await ObtenerPorIdAsync(id);
        }

        private async Task ValidarCodigoYCategoriaAsync(string codigo, int categoriaId, int? productoId = null)
        {
            if (string.IsNullOrWhiteSpace(codigo) || codigo.Length > 100)
                throw new ArgumentException("Ingresa un código de producto de hasta 100 caracteres.");

            if (await _context.Productos.AnyAsync(p => p.Codigo == codigo && (!productoId.HasValue || p.Id != productoId.Value)))
                throw new ArgumentException($"El código {codigo} ya está registrado en otro producto, incluso si está inactivo.");

            if (!await _context.Categorias.AnyAsync(c => c.Id == categoriaId && c.Activo))
                throw new ArgumentException("Selecciona una categoría activa que exista en el catálogo de Categorías.");
        }

        private static void ValidarObjetivo(decimal objetivo, decimal minimo, bool peso)
        {
            if (minimo < 0 || objetivo < 0 || (objetivo > 0 && objetivo < minimo) || decimal.Round(objetivo, 3) != objetivo || (!peso && (decimal.Truncate(objetivo) != objetivo || decimal.Truncate(minimo) != minimo)))
                throw new ArgumentException("El objetivo debe ser cero (automático) o al menos igual al mínimo; usa la unidad del producto.");
        }

        private async Task GuardarProductoAsync(Producto producto)
        {
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                // No dejar una operación fallida pendiente para el siguiente intento.
                foreach (var movimiento in _context.ChangeTracker.Entries<MovimientoInventario>()
                    .Where(e => e.State == EntityState.Added && (e.Entity.Producto == producto || e.Entity.ProductoId == producto.Id)).ToList())
                    movimiento.State = EntityState.Detached;
                _context.Entry(producto).State = EntityState.Detached;
                throw new InvalidOperationException("No se pudo guardar el producto. Comprueba que el código no esté repetido y que la categoría siga disponible.", ex);
            }
        }

        public async Task<bool> EliminarAsync(int id)
        {
            await _acceso.ExigirAsync("Admin");
            var producto = await _context.Productos.FindAsync(id);
            if (producto == null) return false;

            producto.Activo = false;
            producto.FechaActualizacion = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> ActualizarStockAsync(int id, decimal cantidad)
        {
            await _acceso.ExigirAsync("Admin", "Supervisor");
            throw new InvalidOperationException("Utiliza la pantalla Ajustes para registrar motivo y responsable; este acceso directo ya no cambia existencias.");
        }
        public async Task<IEnumerable<ProductoDto>> BuscarAsync(string termino)
        {
            return await _context.Productos
                .Include(p => p.Categoria)
                .Where(p => p.Activo && (
                    p.Nombre.Contains(termino) ||
                    p.Codigo.Contains(termino) ||
                    (p.Descripcion != null && p.Descripcion.Contains(termino))
                ))
                .Select(p => MapToDto(p))
                .ToListAsync();
        }

        public async Task<IEnumerable<ProductoDto>> ObtenerPorCategoriaAsync(int categoriaId)
        {
            return await _context.Productos
                .Include(p => p.Categoria)
                .Where(p => p.CategoriaId == categoriaId && p.Activo)
                .Select(p => MapToDto(p))
                .ToListAsync();
        }

        public async Task<IEnumerable<ProductoDto>> ObtenerStockBajoAsync()
        {
            var productos = await _context.Productos
                .Include(p => p.Categoria)
                .Where(p => p.Stock <= p.StockMinimo && p.Activo)
                .ToListAsync();

            return productos.Select(MapToDto);
        }

        private static ProductoDto MapToDto(Producto producto)
        {
            return new ProductoDto
            {
                Id = producto.Id,
                Codigo = producto.Codigo,
                Nombre = producto.Nombre,
                Descripcion = producto.Descripcion,
                PrecioCompra = producto.PrecioCompra,
                PrecioVenta = producto.PrecioVenta,
                Stock = producto.Stock,
                StockMinimo = producto.StockMinimo,
                StockObjetivo = producto.StockObjetivo, ProveedorHabitual = producto.ProveedorHabitual,
                CategoriaId = producto.CategoriaId,
                CategoriaNombre = producto.Categoria?.Nombre,
                UnidadMedida = producto.UnidadMedida,
                ImagenUrl = producto.ImagenUrl,
                Activo = producto.Activo,
                EsPorPeso = producto.EsPorPeso
            };
        }
    }
}
