using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PuntoAba.Services;
namespace PuntoAba.Controllers;

[ApiController]
[Route("api/respaldos")]
[Authorize]
public class RespaldosController(RespaldoService servicio) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<ArchivoRespaldo>>> Listar() => Ok(await servicio.ListarAsync());
    [HttpPost]
    public async Task<IActionResult> Crear() => Ok(new { nombre = await servicio.CrearAsync() });
    [HttpPost("{nombre}/verificacion")]
    public async Task<ActionResult<VerificacionRespaldo>> Verificar(string nombre) => Ok(await servicio.VerificarAsync(nombre));
}
