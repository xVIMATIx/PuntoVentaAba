using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PuntoAba.Models;

public class EntradaMercancia
{
    public int Id { get; set; }
    public Guid OperacionId { get; set; }
    public DateTime Fecha { get; set; } = DateTime.UtcNow;
    [StringLength(200)] public string? Proveedor { get; set; }
    [StringLength(100)] public string? Documento { get; set; }
    [StringLength(500)] public string? Notas { get; set; }
    public int? UsuarioId { get; set; }
    public Usuario? Usuario { get; set; }
    [Column(TypeName = "decimal(18,2)")] public decimal Total { get; set; }
    public List<DetalleEntradaMercancia> Detalles { get; set; } = new();
}

public class DetalleEntradaMercancia
{
    public int Id { get; set; }
    public int EntradaMercanciaId { get; set; }
    public EntradaMercancia EntradaMercancia { get; set; } = null!;
    public int ProductoId { get; set; }
    public Producto Producto { get; set; } = null!;
    [Column(TypeName = "decimal(18,3)")] public decimal Cantidad { get; set; }
    [Column(TypeName = "decimal(18,2)")] public decimal CostoUnitario { get; set; }
    [Column(TypeName = "decimal(18,2)")] public decimal Importe { get; set; }
    public decimal? CajasRecibidas { get; set; }
    public int? PiezasPorCaja { get; set; }
    [Column(TypeName = "decimal(18,3)")] public decimal StockAnterior { get; set; }
    [Column(TypeName = "decimal(18,3)")] public decimal StockNuevo { get; set; }
}
