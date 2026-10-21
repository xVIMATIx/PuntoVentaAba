using PuntoAba.DTOs;
namespace PuntoAba.Services;
public interface IDashboardService
{
    Task<EstadisticasDashboard> ObtenerEstadisticasAsync();
    Task<List<VentasDiaDto>> ObtenerVentasPorDiaAsync(int dias = 7);
    Task<List<VentasMetodoDto>> ObtenerVentasPorMetodoAsync();
}
