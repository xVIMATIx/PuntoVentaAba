using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace PuntoAba.Models;
public class ConteoFisico
{
    public int Id { get; set; }
    public Guid OperacionId { get; set; }
    public DateTime FechaInicio { get; set; } = DateTime.UtcNow;
    public DateTime? FechaConfirmacion { get; set; }
    public int ResponsableId { get; set; }
    public int? AutorizadoPorId { get; set; }
    [StringLength(20)] public string Estado { get; set; } = "Pendiente";
    public long Version { get; set; }
    public List<DetalleConteoFisico> Detalles { get; set; } = new();
}
public class DetalleConteoFisico
{
    public int Id { get; set; }
    public int ConteoFisicoId { get; set; }
    public ConteoFisico ConteoFisico { get; set; } = null!;
    public int ProductoId { get; set; }
    public Producto Producto { get; set; } = null!;
    [Column(TypeName = "decimal(18,3)")] public decimal CantidadSistema { get; set; }
    [Column(TypeName = "decimal(18,3)")] public decimal? CantidadContada { get; set; }
    public long VersionProducto { get; set; }
    [StringLength(500)] public string Motivo { get; set; } = "";
}
