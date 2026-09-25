using Microsoft.EntityFrameworkCore;
using PuntoAba.Data;
using PuntoAba.DTOs;
using PuntoAba.Models;

namespace PuntoAba.Services
{
    public class ProductoService : IProductoService
    {
        private readonly PuntoAbaDbContext _context;

        public ProductoService(PuntoAbaDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<ProductoDto>> ObtenerTodosAsync()
        {
            var productos = await _context.Productos
                .Include(p => p.Categoria)
                .Where(p => p.Activo)
                .ToListAsync();

            return productos.Select(MapToDto);
        }

        public async Task<ProductoDto?> ObtenerPorIdAsync(int id)
        {
            var producto = await _context.Productos
                .Include(p => p.Categoria)
                .FirstOrDefaultAsync(p => p.Id == id);

            return producto != null ? MapToDto(producto) : null;
        }

        public async Task<ProductoDto?> ObtenerPorCodigoAsync(string codigo)
        {
            var producto = await _context.Productos
                .Include(p => p.Categoria)
                .FirstOrDefaultAsync(p => p.Codigo == codigo);

            return producto != null ? MapToDto(producto) : null;
        }

        public async Task<ProductoDto> CrearAsync(CrearProductoDto dto)
        {
            var producto = new Producto
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
                FechaCreacion = DateTime.UtcNow,
                UltimaSincronizacion = DateTime.UtcNow
            };

            _context.Productos.Add(producto);
            await _context.SaveChangesAsync();

            return (await ObtenerPorIdAsync(producto.Id))!;
        }

        public async Task<ProductoDto?> ActualizarAsync(int id, ActualizarProductoDto dto)
        {
            var producto = await _context.Productos.FindAsync(id);
            if (producto == null) return null;

            if (dto.Nombre != null) producto.Nombre = dto.Nombre;
            if (dto.Descripcion != null) producto.Descripcion = dto.Descripcion;
            if (dto.PrecioCompra.HasValue) producto.PrecioCompra = dto.PrecioCompra.Value;
            if (dto.PrecioVenta.HasValue) producto.PrecioVenta = dto.PrecioVenta.Value;
            if (dto.Stock.HasValue) producto.Stock = dto.Stock.Value;
            if (dto.StockMinimo.HasValue) producto.StockMinimo = dto.StockMinimo.Value;
            if (dto.CategoriaId.HasValue) producto.CategoriaId = dto.CategoriaId.Value;
            if (dto.UnidadMedida != null) producto.UnidadMedida = dto.UnidadMedida;
            if (dto.ImagenUrl != null) producto.ImagenUrl = dto.ImagenUrl;
            if (dto.Activo.HasValue) producto.Activo = dto.Activo.Value;
            if (dto.EsPorPeso.HasValue) producto.EsPorPeso = dto.EsPorPeso.Value;

            producto.FechaActualizacion = DateTime.UtcNow;
            producto.UltimaSincronizacion = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return await ObtenerPorIdAsync(id);
        }

        public async Task<bool> EliminarAsync(int id)
        {
            var producto = await _context.Productos.FindAsync(id);
            if (producto == null) return false;

            producto.Activo = false;
            producto.FechaActualizacion = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> ActualizarStockAsync(int id, decimal cantidad)
        {
            var producto = await _context.Productos.FindAsync(id);
            if (producto == null) return false;

            var stockAnterior = producto.Stock;
            producto.Stock += cantidad;
            producto.FechaActualizacion = DateTime.UtcNow;

            var movimiento = new MovimientoInventario
            {
                ProductoId = id,
                TipoMovimiento = cantidad > 0 ? "Entrada" : "Salida",
                Cantidad = Math.Abs(cantidad),
                StockAnterior = stockAnterior,
                StockNuevo = producto.Stock,
                FechaMovimiento = DateTime.UtcNow,
                Motivo = "Ajuste manual"
            };

            _context.MovimientosInventario.Add(movimiento);
            await _context.SaveChangesAsync();

            return true;
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
