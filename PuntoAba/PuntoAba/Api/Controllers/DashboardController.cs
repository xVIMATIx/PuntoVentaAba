using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PuntoAba.DTOs;
using PuntoAba.Services;
namespace PuntoAba.Controllers;
[ApiController]
[Route("api/dashboard")]
[Authorize]
public class DashboardController(IDashboardService servicio) : ControllerBase
{
    [HttpGet("estadisticas")]
    public async Task<ActionResult<EstadisticasDashboard>> ObtenerEstadisticas() => Ok(await servicio.ObtenerEstadisticasAsync());
    [HttpGet("ventas-por-dia")]
    public async Task<ActionResult<List<VentasDiaDto>>> ObtenerVentasPorDia([FromQuery] int dias = 7) => Ok(await servicio.ObtenerVentasPorDiaAsync(dias));
    [HttpGet("ventas-por-metodo-pago")]
    public async Task<ActionResult<List<VentasMetodoDto>>> ObtenerVentasPorMetodoPago() => Ok(await servicio.ObtenerVentasPorMetodoAsync());
}
