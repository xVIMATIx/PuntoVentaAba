using PuntoAba.DTOs;
using PuntoAba.Models;

namespace PuntoAba.Services
{
    public interface IProductoService
    {
        Task<IEnumerable<ProductoDto>> ObtenerTodosAsync();
        Task<ProductoDto?> ObtenerPorIdAsync(int id);
        Task<ProductoDto?> ObtenerPorCodigoAsync(string codigo);
        Task<ProductoDto> CrearAsync(CrearProductoDto dto);
        Task<ProductoDto?> ActualizarAsync(int id, ActualizarProductoDto dto);
        Task<bool> EliminarAsync(int id);
        Task<bool> ActualizarStockAsync(int id, decimal cantidad);
        Task<IEnumerable<ProductoDto>> BuscarAsync(string termino);
        Task<IEnumerable<ProductoDto>> ObtenerPorCategoriaAsync(int categoriaId);
        Task<IEnumerable<ProductoDto>> ObtenerStockBajoAsync();
    }
}
