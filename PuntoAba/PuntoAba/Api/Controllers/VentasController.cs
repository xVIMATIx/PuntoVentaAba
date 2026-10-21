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
            var ventas = await _ventaService.ObtenerTodas(fechaInicio, fechaFin);
            return Ok(ventas);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<VentaDto>> ObtenerPorId(int id)
        {
            var venta = await _ventaService.ObtenerPorIdAsync(id);
            if (venta == null)
                return NotFound($"Venta con ID {id} no encontrada");

            return Ok(venta);
        }

        [HttpGet("cliente/{clienteId}")]
        public async Task<ActionResult<IEnumerable<VentaDto>>> ObtenerPorCliente(int clienteId)
        {
            var ventas = await _ventaService.ObtenerPorClienteAsync(clienteId);
            return Ok(ventas);
        }

        [HttpGet("usuario/{usuarioId}")]
        public async Task<ActionResult<IEnumerable<VentaDto>>> ObtenerPorUsuario(int usuarioId)
        {
            var ventas = await _ventaService.ObtenerPorUsuarioAsync(usuarioId);
            return Ok(ventas);
        }

        [HttpGet("total-hoy")]
        public async Task<ActionResult<decimal>> ObtenerTotalDelDia()
        {
            var total = await _ventaService.ObtenerTotalVentasDelDiaAsync();
            return Ok(new { fecha = DateTime.Today, total });
        }

        [HttpGet("reporte")]
        [Authorize(Roles = "Admin,Supervisor")]
        public async Task<ActionResult<ReporteVentasDto>> ObtenerReporte(
            [FromQuery] DateTime fechaInicio,
            [FromQuery] DateTime fechaFin)
        {
            var reporte = await _ventaService.ObtenerReporteAsync(fechaInicio, fechaFin);
            return Ok(reporte);
        }

        [HttpPost]
        public async Task<ActionResult<VentaDto>> Crear([FromBody] CrearVentaDto dto)
        {
            var venta = await _ventaService.CrearAsync(dto);
            return CreatedAtAction(nameof(ObtenerPorId), new { id = venta.Id }, venta);
        }
    }
}
