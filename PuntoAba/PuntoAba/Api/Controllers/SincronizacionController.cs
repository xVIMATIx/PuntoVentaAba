using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PuntoAba.DTOs;
using PuntoAba.Services;

namespace PuntoAba.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class SincronizacionController : ControllerBase
    {
        private readonly ISincronizacionService _sincronizacionService;
        private readonly ILogger<SincronizacionController> _logger;

        public SincronizacionController(
            ISincronizacionService sincronizacionService,
            ILogger<SincronizacionController> logger)
        {
            _sincronizacionService = sincronizacionService;
            _logger = logger;
        }

        [HttpPost("sincronizar")]
        public async Task<ActionResult<SincronizacionResponseDto>> Sincronizar([FromBody] SincronizacionDto datos)
        {
            try
            {
                var response = await _sincronizacionService.SincronizarDatosAsync(datos);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error durante la sincronización");
                return StatusCode(500, new SincronizacionResponseDto
                {
                    Exitoso = false,
                    Mensaje = $"Error: {ex.Message}",
                    FechaSincronizacion = DateTime.UtcNow
                });
            }
        }

        [HttpGet("obtener-datos")]
        public async Task<ActionResult<SincronizacionDto>> ObtenerDatos([FromQuery] DateTime? ultimaSincronizacion)
        {
            try
            {
                var datos = await _sincronizacionService.ObtenerDatosParaSincronizarAsync(ultimaSincronizacion);
                return Ok(datos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener datos para sincronizar");
                return StatusCode(500, "Error interno del servidor");
            }
        }

        [HttpGet("estado")]
        public async Task<ActionResult<EstadoSincronizacionDto>> ObtenerEstado()
        {
            try
            {
                var estado = await _sincronizacionService.ObtenerEstadoSincronizacionAsync();
                return Ok(estado);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener estado de sincronización");
                return StatusCode(500, "Error interno del servidor");
            }
        }
    }
}
