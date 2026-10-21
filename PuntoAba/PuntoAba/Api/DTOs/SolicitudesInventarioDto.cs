using System.ComponentModel.DataAnnotations;
namespace PuntoAba.DTOs;
public class IniciarConteoDto
{
    public Guid OperacionId { get; set; } = Guid.NewGuid();
    [Required, MinLength(1)]
    public List<int> Productos { get; set; } = new();
}
public class VersionConteoDto
{
    [Range(0, long.MaxValue)]
    public long Version { get; set; }
}
public class CorreccionMovimientoDto
{
    public Guid OperacionId { get; set; } = Guid.NewGuid();
    [Required, StringLength(400, MinimumLength = 3)]
    public string Motivo { get; set; } = "";
}
