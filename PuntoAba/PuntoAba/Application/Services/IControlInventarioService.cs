using PuntoAba.DTOs;
namespace PuntoAba.Services;
public interface IControlInventarioService
{
    Task<MovimientoInventarioDto> RegistrarAsync(OperacionExistenciasDto dto);
    Task<MovimientoInventarioDto> CorregirAsync(int movimientoId, string motivo, Guid operacionId);
    Task<ConteoDto> IniciarConteoAsync(List<int> productos, Guid operacionId);
    Task<List<ConteoDto>> ObtenerConteosAsync();
    Task<ConteoDto?> ObtenerConteoAsync(int id);
    Task<ConteoDto> GuardarConteoAsync(ConteoDto conteo);
    Task<ConteoDto> ConfirmarConteoAsync(int id, long version);
    Task<ConteoDto> ActualizarBaseConteoAsync(int id, long version);
    Task<ReporteInventarioDto> ReporteAsync(DateTime desde, DateTime hasta);
}
