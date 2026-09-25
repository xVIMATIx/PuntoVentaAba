using System.ComponentModel.DataAnnotations;

namespace PuntoAba.Models
{
    public class Cliente
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public string Nombre { get; set; } = string.Empty;

        [StringLength(100)]
        public string? Apellido { get; set; }

        [StringLength(50)]
        public string? Telefono { get; set; }

        [StringLength(100)]
        [EmailAddress]
        public string? Email { get; set; }

        [StringLength(300)]
        public string? Direccion { get; set; }

        [StringLength(50)]
        public string? DocumentoIdentidad { get; set; }

        public bool Activo { get; set; } = true;

        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

        public DateTime? FechaActualizacion { get; set; }

        // Relaciones
        public virtual ICollection<Venta> Ventas { get; set; } = new List<Venta>();
    }
}
