namespace PuntoAba.Services;

public class ImagenProductoService(IWebHostEnvironment entorno, IAccesoOperacion acceso) : IImagenProductoService
{
    public const int MaximoBytes = 5 * 1024 * 1024;
    public async Task<string> GuardarAsync(Stream archivo, string tipoContenido, CancellationToken cancelacion = default)
    {
        await acceso.ExigirAsync("Admin", "Supervisor");
        using var memoria = new MemoryStream();
        var buffer = new byte[81920]; int leidos;
        while ((leidos = await archivo.ReadAsync(buffer, cancelacion)) > 0)
        {
            if (memoria.Length + leidos > MaximoBytes) throw new ArgumentException("La imagen no debe superar 5 MB.");
            await memoria.WriteAsync(buffer.AsMemory(0, leidos), cancelacion);
        }
        var bytes = memoria.ToArray();
        var extension = TipoImagen(bytes);
        var mime = tipoContenido.ToLowerInvariant();
        if (extension == null || mime != (extension switch { ".jpg" => "image/jpeg", ".png" => "image/png", ".gif" => "image/gif", ".webp" => "image/webp", _ => "" }))
        {
            if (!(extension == ".jpg" && mime == "image/jpg"))
                throw new ArgumentException("Selecciona una imagen JPG, PNG, GIF o WEBP válida.");
        }
        var carpeta = Path.Combine(entorno.WebRootPath ?? Path.Combine(entorno.ContentRootPath, "wwwroot"), "images", "productos");
        Directory.CreateDirectory(carpeta);
        var nombre = Guid.NewGuid().ToString("N") + extension;
        await File.WriteAllBytesAsync(Path.Combine(carpeta, nombre), bytes, cancelacion);
        return "/images/productos/" + nombre;
    }
    private static string? TipoImagen(byte[] b)
    {
        if (b.Length >= 3 && b[0] == 255 && b[1] == 216 && b[2] == 255) return ".jpg";
        if (b.Length >= 8 && b.Take(8).SequenceEqual(new byte[] {137,80,78,71,13,10,26,10})) return ".png";
        if (b.Length >= 6 && (System.Text.Encoding.ASCII.GetString(b, 0, 6) is "GIF87a" or "GIF89a")) return ".gif";
        if (b.Length >= 12 && System.Text.Encoding.ASCII.GetString(b, 0, 4) == "RIFF" && System.Text.Encoding.ASCII.GetString(b, 8, 4) == "WEBP") return ".webp";
        return null;
    }
}
