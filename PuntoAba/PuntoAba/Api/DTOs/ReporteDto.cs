namespace PuntoAba.DTOs;

public class ReporteVentasDto
{
    public DateTime FechaInicio { get; set; }
    public DateTime FechaFin { get; set; }
    public decimal TotalVentas { get; set; }
    public int CantidadVentas { get; set; }
    public decimal PromedioVenta { get; set; }
    public List<VentaDto> Ventas { get; set; } = new();
}
