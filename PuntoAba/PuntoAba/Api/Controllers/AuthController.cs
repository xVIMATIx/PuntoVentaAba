using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PuntoAba.DTOs;
using PuntoAba.Services;

namespace PuntoAba.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(IAuthService authService, ILogger<AuthController> logger)
        {
            _authService = authService;
            _logger = logger;
        }

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<ActionResult<LoginResponseDto>> Login([FromBody] LoginDto dto)
        {
            try
            {
                var response = await _authService.LoginAsync(dto);
                if (response == null)
                    return Unauthorized(new { mensaje = "Credenciales inválidas" });

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error durante el login");
                return StatusCode(500, "Error interno del servidor");
            }
        }

        [HttpPost("usuarios")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<UsuarioDto>> CrearUsuario([FromBody] CrearUsuarioDto dto)
        {
            try
            {
                var usuario = await _authService.CrearUsuarioAsync(dto);
                return CreatedAtAction(nameof(ObtenerUsuario), new { id = usuario.Id }, usuario);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear usuario");
                return BadRequest(new { mensaje = ex.Message });
            }
        }

        [HttpGet("usuarios/{id}")]
        [Authorize]
        public async Task<ActionResult<UsuarioDto>> ObtenerUsuario(int id)
        {
            try
            {
                var usuario = await _authService.ObtenerUsuarioPorIdAsync(id);
                if (usuario == null)
                    return NotFound($"Usuario con ID {id} no encontrado");

                return Ok(usuario);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener usuario {Id}", id);
                return StatusCode(500, "Error interno del servidor");
            }
        }

        [HttpGet("usuarios")]
        [Authorize(Roles = "Admin,Supervisor")]
        public async Task<ActionResult<IEnumerable<UsuarioDto>>> ObtenerTodos()
        {
            try
            {
                var usuarios = await _authService.ObtenerTodosUsuariosAsync();
                return Ok(usuarios);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener usuarios");
                return StatusCode(500, "Error interno del servidor");
            }
        }

        [HttpPut("usuarios/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<UsuarioDto>> ActualizarUsuario(int id, [FromBody] ActualizarUsuarioDto dto)
        {
            try
            {
                var usuario = await _authService.ActualizarUsuarioAsync(id, dto);
                if (usuario == null)
                    return NotFound($"Usuario con ID {id} no encontrado");

                return Ok(usuario);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar usuario {Id}", id);
                return StatusCode(500, "Error interno del servidor");
            }
        }

        [HttpPost("cambiar-password")]
        [Authorize]
        public async Task<ActionResult> CambiarPassword([FromBody] CambiarPasswordDto dto)
        {
            try
            {
                var usuarioId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
                var resultado = await _authService.CambiarPasswordAsync(usuarioId, dto);

                if (!resultado)
                    return BadRequest(new { mensaje = "Password actual incorrecta" });

                return Ok(new { mensaje = "Password cambiada exitosamente" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cambiar password");
                return StatusCode(500, "Error interno del servidor");
            }
        }

        [HttpDelete("usuarios/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> DesactivarUsuario(int id)
        {
            try
            {
                var resultado = await _authService.DesactivarUsuarioAsync(id);
                if (!resultado)
                    return NotFound($"Usuario con ID {id} no encontrado");

                return Ok(new { mensaje = "Usuario desactivado correctamente" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al desactivar usuario {Id}", id);
                return StatusCode(500, "Error interno del servidor");
            }
        }
    }
}
