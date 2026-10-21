using PuntoAba.DTOs;
namespace PuntoAba.Services;
public interface ICategoriaService
{
    Task<List<CategoriaDto>> ObtenerTodasAsync();
    Task<CategoriaDto?> ObtenerPorIdAsync(int id);
    Task<CategoriaDto> CrearAsync(CrearCategoriaDto dto);
    Task<CategoriaDto?> ActualizarAsync(int id, ActualizarCategoriaDto dto);
    Task<bool> EliminarAsync(int id);
}
