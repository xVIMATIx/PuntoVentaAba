using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PuntoAba.Models
{
    public class MovimientoInventario
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ProductoId { get; set; }

        [Required]
        [StringLength(50)]
        public string TipoMovimiento { get; set; } = string.Empty; // "Entrada", "Salida", "Ajuste"

        [Required]
        [Column(TypeName = "decimal(18,3)")]
        public decimal Cantidad { get; set; }

        [Column(TypeName = "decimal(18,3)")]
        public decimal StockAnterior { get; set; }

        [Column(TypeName = "decimal(18,3)")]
        public decimal StockNuevo { get; set; }

        [StringLength(500)]
        public string? Motivo { get; set; }

        public int? UsuarioId { get; set; }

        [Required]
        public DateTime FechaMovimiento { get; set; } = DateTime.UtcNow;

        [StringLength(100)]
        public string? Referencia { get; set; }
        public int? MovimientoOriginalId { get; set; }
        public MovimientoInventario? MovimientoOriginal { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal? CostoUnitario { get; set; }
        public Guid? OperacionId { get; set; }

        // Relaciones
        [ForeignKey("ProductoId")]
        public virtual Producto? Producto { get; set; }

        [ForeignKey("UsuarioId")]
        public virtual Usuario? Usuario { get; set; }
    }
}
