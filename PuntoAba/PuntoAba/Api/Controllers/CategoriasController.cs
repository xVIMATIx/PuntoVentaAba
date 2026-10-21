using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PuntoAba.DTOs;
using PuntoAba.Services;
namespace PuntoAba.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CategoriasController(ICategoriaService servicio) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<CategoriaDto>>> ObtenerTodas() => Ok(await servicio.ObtenerTodasAsync());
    [HttpGet("{id:int}")]
    public async Task<ActionResult<CategoriaDto>> ObtenerPorId(int id)
    {
        var item = await servicio.ObtenerPorIdAsync(id);
        return item == null ? NotFound() : Ok(item);
    }

    [HttpPost]
    public async Task<ActionResult<CategoriaDto>> Crear(CrearCategoriaDto dto)
    {
        var item = await servicio.CrearAsync(dto);
        return CreatedAtAction(nameof(ObtenerPorId), new { id = item.Id }, item);
    }
    [HttpPut("{id:int}")]
    public async Task<ActionResult<CategoriaDto>> Actualizar(int id, ActualizarCategoriaDto dto)
    {
        var item = await servicio.ActualizarAsync(id, dto);
        return item == null ? NotFound() : Ok(item);
    }
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Eliminar(int id) => await servicio.EliminarAsync(id) ? NoContent() : NotFound();
}
