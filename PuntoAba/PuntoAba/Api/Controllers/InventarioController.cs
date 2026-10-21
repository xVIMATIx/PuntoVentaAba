using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PuntoAba.DTOs;
using PuntoAba.Services;
namespace PuntoAba.Controllers;

[ApiController]
[Route("api/inventario")]
[Authorize]
public class InventarioController(IInventarioService entradas, IControlInventarioService control) : ControllerBase
{
    [HttpGet("entradas")]
    public async Task<ActionResult<List<EntradaMercanciaDto>>> Entradas() => Ok(await entradas.ObtenerEntradasAsync());
    [HttpPost("entradas")]
    public async Task<ActionResult<EntradaMercanciaDto>> Recibir(CrearEntradaMercanciaDto dto) => Ok(await entradas.RegistrarEntradaAsync(dto));
    [HttpGet("movimientos")]
    public async Task<ActionResult<PaginaMovimientosDto>> Movimientos([FromQuery] FiltroMovimientosDto filtro) => Ok(await entradas.ObtenerMovimientosAsync(filtro));
    [HttpGet("resurtido")]
    public async Task<ActionResult<List<ProductoResurtidoDto>>> Resurtido() => Ok(await entradas.ObtenerResurtidoAsync());
    [HttpPost("operaciones")]
    public async Task<ActionResult<MovimientoInventarioDto>> Operacion(OperacionExistenciasDto dto) => Ok(await control.RegistrarAsync(dto));
    [HttpPost("movimientos/{id:int}/correccion")]
    public async Task<ActionResult<MovimientoInventarioDto>> Corregir(int id, CorreccionMovimientoDto dto) => Ok(await control.CorregirAsync(id, dto.Motivo, dto.OperacionId));
    [HttpGet("conteos")]
    public async Task<ActionResult<List<ConteoDto>>> Conteos() => Ok(await control.ObtenerConteosAsync());
    [HttpGet("conteos/{id:int}")]
    public async Task<ActionResult<ConteoDto>> Conteo(int id)
    {
        var c = await control.ObtenerConteoAsync(id); return c == null ? NotFound() : Ok(c);
    }
    [HttpPost("conteos")]
    public async Task<ActionResult<ConteoDto>> Iniciar(IniciarConteoDto dto)
    {
        var c = await control.IniciarConteoAsync(dto.Productos, dto.OperacionId);
        return CreatedAtAction(nameof(Conteo), new { id = c.Id }, c);
    }
    [HttpPut("conteos/{id:int}")]
    public async Task<ActionResult<ConteoDto>> Guardar(int id, ConteoDto dto)
    {
        if (id != dto.Id) return BadRequest("El folio de la ruta no coincide.");
        return Ok(await control.GuardarConteoAsync(dto));
    }
    [HttpPost("conteos/{id:int}/confirmacion")]
    public async Task<ActionResult<ConteoDto>> Confirmar(int id, VersionConteoDto dto) => Ok(await control.ConfirmarConteoAsync(id, dto.Version));
    [HttpPost("conteos/{id:int}/actualizar-base")]
    public async Task<ActionResult<ConteoDto>> ActualizarBase(int id, VersionConteoDto dto) => Ok(await control.ActualizarBaseConteoAsync(id, dto.Version));
    [HttpGet("reporte")]
    public async Task<ActionResult<ReporteInventarioDto>> Reporte([FromQuery, Required] DateTime? desde, [FromQuery, Required] DateTime? hasta)
        => Ok(await control.ReporteAsync(desde!.Value, hasta!.Value));
}
