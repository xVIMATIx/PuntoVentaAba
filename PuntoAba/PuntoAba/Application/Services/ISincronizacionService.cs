using PuntoAba.DTOs;

namespace PuntoAba.Services
{
    public interface ISincronizacionService
    {
        Task<SincronizacionResponseDto> SincronizarDatosAsync(SincronizacionDto datos);
        Task<SincronizacionDto> ObtenerDatosParaSincronizarAsync(DateTime? ultimaSincronizacion);
        Task<EstadoSincronizacionDto> ObtenerEstadoSincronizacionAsync();
    }
}
