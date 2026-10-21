using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PuntoAba.Services;
namespace PuntoAba.Controllers;
[ApiController]
[Route("api/productos/imagenes")]
[Authorize]
public class ImagenesProductosController(IImagenProductoService servicio) : ControllerBase
{
    [HttpPost]
    [RequestSizeLimit(6 * 1024 * 1024)]
    public async Task<IActionResult> Subir(IFormFile archivo, CancellationToken cancelacion)
    {
        if (archivo.Length == 0 || archivo.Length > ImagenProductoService.MaximoBytes)
            return BadRequest("La imagen debe tener contenido y no superar 5 MB.");
        await using var stream = archivo.OpenReadStream();
        return Ok(new { url = await servicio.GuardarAsync(stream, archivo.ContentType, cancelacion) });
    }
}
