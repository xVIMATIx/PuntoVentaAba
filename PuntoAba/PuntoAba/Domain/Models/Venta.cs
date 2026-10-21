using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PuntoAba.Models
{
    public class Venta
    {
        [Key]
        public int Id { get; set; }
        public Guid? OperacionId { get; set; }

        [Required]
        [StringLength(50)]
        public string NumeroVenta { get; set; } = string.Empty;

        [Required]
        public DateTime FechaVenta { get; set; } = DateTime.UtcNow;

        public int? ClienteId { get; set; }

        public int UsuarioId { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Subtotal { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Descuento { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal Impuesto { get; set; } = 0;

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Total { get; set; }

        // Null distingue ventas anteriores que no registraron el efectivo.
        [Column(TypeName = "decimal(18,2)")]
        public decimal? Recibido { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? Cambio { get; set; }

        [Required]
        [StringLength(50)]
        public string MetodoPago { get; set; } = "Efectivo";

        [StringLength(100)]
        public string? EstadoVenta { get; set; } = "Completada";

        [StringLength(500)]
        public string? Notas { get; set; }

        // Para sincronización
        public DateTime UltimaSincronizacion { get; set; } = DateTime.UtcNow;

        public bool Sincronizado { get; set; } = false;

        [StringLength(100)]
        public string? OrigenVenta { get; set; } // "POS" o "WEB"

        // Relaciones
        [ForeignKey("ClienteId")]
        public virtual Cliente? Cliente { get; set; }

        [ForeignKey("UsuarioId")]
        public virtual Usuario? Usuario { get; set; }

        public virtual ICollection<DetalleVenta> DetallesVenta { get; set; } = new List<DetalleVenta>();
    }
}
