using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PuntoAba.DTOs;
using PuntoAba.Services;

namespace PuntoAba.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class VentasController : ControllerBase
    {
        private readonly IVentaService _ventaService;
        private readonly ILogger<VentasController> _logger;

        public VentasController(IVentaService ventaService, ILogger<VentasController> logger)
        {
            _ventaService = ventaService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<VentaDto>>> ObtenerTodas(
            [FromQuery] DateTime? fechaInicio,
            [FromQuery] DateTime? fechaFin)
        {
            try
            {
                var ventas = await _ventaService.ObtenerTodas(fechaInicio, fechaFin);
                return Ok(ventas);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener ventas");
                return StatusCode(500, "Error interno del servidor");
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<VentaDto>> ObtenerPorId(int id)
        {
            try
            {
                var venta = await _ventaService.ObtenerPorIdAsync(id);
                if (venta == null)
                    return NotFound($"Venta con ID {id} no encontrada");

                return Ok(venta);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener venta {Id}", id);
                return StatusCode(500, "Error interno del servidor");
            }
        }

        [HttpGet("cliente/{clienteId}")]
        public async Task<ActionResult<IEnumerable<VentaDto>>> ObtenerPorCliente(int clienteId)
        {
            try
            {
                var ventas = await _ventaService.ObtenerPorClienteAsync(clienteId);
                return Ok(ventas);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener ventas del cliente {ClienteId}", clienteId);
                return StatusCode(500, "Error interno del servidor");
            }
        }

        [HttpGet("usuario/{usuarioId}")]
        public async Task<ActionResult<IEnumerable<VentaDto>>> ObtenerPorUsuario(int usuarioId)
        {
            try
            {
                var ventas = await _ventaService.ObtenerPorUsuarioAsync(usuarioId);
                return Ok(ventas);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener ventas del usuario {UsuarioId}", usuarioId);
                return StatusCode(500, "Error interno del servidor");
            }
        }

        [HttpGet("total-hoy")]
        public async Task<ActionResult<decimal>> ObtenerTotalDelDia()
        {
            try
            {
                var total = await _ventaService.ObtenerTotalVentasDelDiaAsync();
                return Ok(new { fecha = DateTime.Today, total });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener total del día");
                return StatusCode(500, "Error interno del servidor");
            }
        }

        [HttpGet("reporte")]
        [Authorize(Roles = "Admin,Supervisor")]
        public async Task<ActionResult<ReporteVentasDto>> ObtenerReporte(
            [FromQuery] DateTime fechaInicio,
            [FromQuery] DateTime fechaFin)
        {
            try
            {
                var reporte = await _ventaService.ObtenerReporteAsync(fechaInicio, fechaFin);
                return Ok(reporte);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al generar reporte de ventas");
                return StatusCode(500, "Error interno del servidor");
            }
        }

        [HttpPost]
        public async Task<ActionResult<VentaDto>> Crear([FromBody] CrearVentaDto dto)
        {
            try
            {
                var venta = await _ventaService.CrearAsync(dto);
                return CreatedAtAction(nameof(ObtenerPorId), new { id = venta.Id }, venta);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear venta");
                return BadRequest(new { mensaje = ex.Message });
            }
        }
    }
}
