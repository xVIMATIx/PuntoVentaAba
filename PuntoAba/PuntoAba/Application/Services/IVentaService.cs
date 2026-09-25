using PuntoAba.DTOs;

namespace PuntoAba.Services
{
    public interface IVentaService
    {
        Task<VentaDto?> ObtenerPorIdAsync(int id);
        Task<IEnumerable<VentaDto>> ObtenerTodas(DateTime? fechaInicio = null, DateTime? fechaFin = null);
        Task<VentaDto> CrearAsync(CrearVentaDto dto);
        Task<ReporteVentasDto> ObtenerReporteAsync(DateTime fechaInicio, DateTime fechaFin);
        Task<IEnumerable<VentaDto>> ObtenerPorClienteAsync(int clienteId);
        Task<IEnumerable<VentaDto>> ObtenerPorUsuarioAsync(int usuarioId);
        Task<decimal> ObtenerTotalVentasDelDiaAsync();
    }
}
