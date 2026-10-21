namespace PuntoAba.Services;
public interface IImagenProductoService
{
    Task<string> GuardarAsync(Stream archivo, string tipoContenido, CancellationToken cancelacion = default);
}
