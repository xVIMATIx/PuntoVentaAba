using System.ComponentModel.DataAnnotations;

namespace PuntoAba.Models
{
    public class RegistroSincronizacion
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Entidad { get; set; } = string.Empty; // "Producto", "Venta", etc.

        [Required]
        public int EntidadId { get; set; }

        [Required]
        [StringLength(50)]
        public string TipoOperacion { get; set; } = string.Empty; // "INSERT", "UPDATE", "DELETE"

        [Required]
        public DateTime FechaSincronizacion { get; set; } = DateTime.UtcNow;

        [StringLength(100)]
        public string? DispositivoOrigen { get; set; }

        public bool Exitoso { get; set; } = true;

        [StringLength(1000)]
        public string? MensajeError { get; set; }

        public string? DatosJson { get; set; }
    }
}
