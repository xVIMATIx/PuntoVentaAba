using Microsoft.AspNetCore.Mvc;
using PuntoAba.Services;

namespace PuntoAba.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BalanzaController : ControllerBase
    {
        private readonly IBalanzaService _balanzaService;
        private readonly ILogger<BalanzaController> _logger;

        public BalanzaController(IBalanzaService balanzaService, ILogger<BalanzaController> logger)
        {
            _balanzaService = balanzaService;
            _logger = logger;
        }

        [HttpGet("leer")]
        public IActionResult LeerPeso()
        {
            try
            {
                if (!_balanzaService.Conectada || !_balanzaService.PesoActual.HasValue)
                {
                    return StatusCode(503, new
                    {
                        error = "Balanza no disponible",
                        exito = false,
                        estado = _balanzaService.Estado,
                        puerto = _balanzaService.PuertoActual
                    });
                }

                var response = new
                {
                    peso = _balanzaService.PesoActual.Value,
                    unidad = _balanzaService.UnidadMedida,
                    lectura_cruda = _balanzaService.LecturaCruda,
                    estado = _balanzaService.Estado,
                    puerto = _balanzaService.PuertoActual,
                    ultima_lectura = _balanzaService.UltimaLectura,
                    exito = true
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al leer balanza");
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }
}
