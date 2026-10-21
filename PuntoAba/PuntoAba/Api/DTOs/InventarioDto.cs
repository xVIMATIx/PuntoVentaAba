using System.ComponentModel.DataAnnotations;

namespace PuntoAba.DTOs;

public class CrearEntradaMercanciaDto
{
    public Guid OperacionId { get; set; } = Guid.NewGuid();
    [StringLength(200, ErrorMessage = "El proveedor admite hasta 200 caracteres.")]
    public string? Proveedor { get; set; }
    [StringLength(100, ErrorMessage = "El documento admite hasta 100 caracteres.")]
    public string? Documento { get; set; }
    [StringLength(500, ErrorMessage = "Las notas admiten hasta 500 caracteres.")]
    public string? Notas { get; set; }
    public bool ActualizarPrecioCompra { get; set; }
    public List<LineaEntradaMercanciaDto> Detalles { get; set; } = new();
}

public class LineaEntradaMercanciaDto
{
    public int ProductoId { get; set; }
    public string Codigo { get; set; } = "";
    public string Nombre { get; set; } = "";
    public bool EsPorPeso { get; set; }
    public string UnidadMedida { get; set; } = "Pieza";
    public decimal Cantidad { get; set; } = 1;
    public bool PorCaja { get; set; }
    public int PiezasPorCaja { get; set; } = 1;
    public decimal? StockEsperado { get; set; }
    public decimal CantidadBase => PorCaja ? Cantidad * PiezasPorCaja : Cantidad;
    public decimal? StockResultante => StockEsperado + CantidadBase;
    public decimal CostoUnitario { get; set; }
    public decimal Importe => decimal.Round(CantidadBase * CostoUnitario, 2, MidpointRounding.AwayFromZero);
}

public class EntradaMercanciaDto
{
    public int Id { get; set; }
    public string Folio => $"E-{Id:D6}";
    public DateTime Fecha { get; set; }
    public string? Proveedor { get; set; }
    public string? Documento { get; set; }
    public string? Notas { get; set; }
    public decimal Total { get; set; }
    public List<LineaEntradaMercanciaDto> Detalles { get; set; } = new();
}

public class FiltroMovimientosDto
{
    public bool SoloMermasDevoluciones { get; set; }
    public DateTime? Desde { get; set; }
    public DateTime? Hasta { get; set; }
    public string Tipo { get; set; } = "";
    public string Buscar { get; set; } = "";
    public int Pagina { get; set; } = 1;
    public int TamanoPagina { get; set; } = 25;
}

public class MovimientoInventarioDto
{
    public int Id { get; set; }
    public DateTime Fecha { get; set; }
    public string Codigo { get; set; } = "";
    public string Producto { get; set; } = "";
    public string Tipo { get; set; } = "";
    public decimal Cantidad { get; set; }
    public decimal StockAnterior { get; set; }
    public decimal StockNuevo { get; set; }
    public string? Motivo { get; set; }
    public string? Referencia { get; set; }
    public string? Usuario { get; set; }
    public int? MovimientoOriginalId { get; set; }
}

public record PaginaMovimientosDto(List<MovimientoInventarioDto> Movimientos, int Total);

public class ProductoResurtidoDto
{
    public int ProductoId { get; set; }
    public string Codigo { get; set; } = "";
    public string Nombre { get; set; } = "";
    public string Categoria { get; set; } = "";
    public string Proveedor { get; set; } = "";
    public bool EsPorPeso { get; set; }
    public string UnidadMedida { get; set; } = "Pieza";
    public decimal Stock { get; set; }
    public decimal Minimo { get; set; }
    public decimal Objetivo { get; set; }
    public decimal Cantidad { get; set; }
    public decimal PrecioCompra { get; set; }
    public bool Seleccionado { get; set; } = true;
    public decimal Estimado => decimal.Round(Cantidad * PrecioCompra, 2, MidpointRounding.AwayFromZero);
}
