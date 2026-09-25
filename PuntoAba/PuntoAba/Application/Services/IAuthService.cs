using PuntoAba.DTOs;

namespace PuntoAba.Services
{
    public interface IAuthService
    {
        Task<LoginResponseDto?> LoginAsync(LoginDto dto);
        Task<UsuarioDto> CrearUsuarioAsync(CrearUsuarioDto dto);
        Task<bool> CambiarPasswordAsync(int usuarioId, CambiarPasswordDto dto);
        Task<UsuarioDto?> ObtenerUsuarioPorIdAsync(int id);
        Task<IEnumerable<UsuarioDto>> ObtenerTodosUsuariosAsync();
        Task<UsuarioDto?> ActualizarUsuarioAsync(int id, ActualizarUsuarioDto dto);
        Task<bool> DesactivarUsuarioAsync(int id);
    }
}
