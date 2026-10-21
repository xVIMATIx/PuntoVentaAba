using PuntoAba.DTOs;
namespace PuntoAba.Services;
public interface IClienteService
{
    Task<List<ClienteDto>> ObtenerTodosAsync(string? termino = null);
    Task<ClienteDto?> ObtenerPorIdAsync(int id);
    Task<ClienteDto> CrearAsync(CrearClienteDto dto);
    Task<ClienteDto?> ActualizarAsync(int id, ActualizarClienteDto dto);
    Task<bool> EliminarAsync(int id);
}
