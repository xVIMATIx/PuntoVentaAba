using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PuntoAba.DTOs;
using PuntoAba.Services;
namespace PuntoAba.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ClientesController(IClienteService servicio) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<ClienteDto>>> ObtenerTodas() => Ok(await servicio.ObtenerTodosAsync());
    [HttpGet("{id:int}")]
    public async Task<ActionResult<ClienteDto>> ObtenerPorId(int id)
    {
        var item = await servicio.ObtenerPorIdAsync(id);
        return item == null ? NotFound() : Ok(item);
    }

    [HttpGet("buscar/{termino}")]
    public async Task<ActionResult<List<ClienteDto>>> Buscar(string termino) => Ok(await servicio.ObtenerTodosAsync(termino));

    [HttpPost]
    public async Task<ActionResult<ClienteDto>> Crear(CrearClienteDto dto)
    {
        var item = await servicio.CrearAsync(dto);
        return CreatedAtAction(nameof(ObtenerPorId), new { id = item.Id }, item);
    }
    [HttpPut("{id:int}")]
    public async Task<ActionResult<ClienteDto>> Actualizar(int id, ActualizarClienteDto dto)
    {
        var item = await servicio.ActualizarAsync(id, dto);
        return item == null ? NotFound() : Ok(item);
    }
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Eliminar(int id) => await servicio.EliminarAsync(id) ? NoContent() : NotFound();
}
