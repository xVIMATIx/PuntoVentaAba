using System.ComponentModel.DataAnnotations;
namespace PuntoAba.DTOs;

public class OperacionExistenciasDto
{
    public Guid OperacionId { get; set; } = Guid.NewGuid();
    public int ProductoId { get; set; }
    public string Tipo { get; set; } = "Ajuste";
    public decimal Cantidad { get; set; }
    public string Motivo { get; set; } = "";
    public string Referencia { get; set; } = "";
    public decimal? StockEsperado { get; set; }
}
public class LineaConteoDto
{
    public int ProductoId { get; set; }
    public string Codigo { get; set; } = "";
    public string Nombre { get; set; } = "";
    public bool EsPorPeso { get; set; }
    public decimal Sistema { get; set; }
    public decimal? Contada { get; set; }
    public string Motivo { get; set; } = "";
    public decimal? Diferencia => Contada - Sistema;
}
public class ConteoDto
{
    public int Id { get; set; }
    public string Folio => $"C-{Id:D6}";
    public DateTime Fecha { get; set; }
    public string Estado { get; set; } = "";
    public string Responsable { get; set; } = "";
    public string? AutorizadoPor { get; set; }
    public long Version { get; set; }
    public List<LineaConteoDto> Detalles { get; set; } = new();
}
public class ReporteInventarioDto
{
    public List<ProductoReporteDto> Productos { get; set; } = new();
    public List<MovimientoInventarioDto> MermasDevoluciones { get; set; } = new();
    public decimal ValorInventario => Productos.Sum(p => p.Valor);
    public decimal CostoMermas { get; set; }
    public bool HayCostosEstimados { get; set; }
}
public class ProductoReporteDto
{
    public string Codigo { get; set; } = "";
    public string Nombre { get; set; } = "";
    public string Categoria { get; set; } = "";
    public string Proveedor { get; set; } = "";
    public decimal Stock { get; set; }
    public decimal Minimo { get; set; }
    public decimal Valor { get; set; }
    public decimal Vendido { get; set; }
    public decimal? Rotacion { get; set; }
}
