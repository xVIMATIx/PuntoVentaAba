namespace PuntoAba.DTOs
{
    public class VentaDto
    {
        public int Id { get; set; }
        public string NumeroVenta { get; set; } = string.Empty;
        public DateTime FechaVenta { get; set; }
        public int? ClienteId { get; set; }
        public string? ClienteNombre { get; set; }
        public string? UsuarioNombre { get; set; }
        public decimal Subtotal { get; set; }
        public decimal Descuento { get; set; }
        public decimal Impuesto { get; set; }
        public decimal Total { get; set; }
        public string MetodoPago { get; set; } = string.Empty;
        public string? EstadoVenta { get; set; }
        public List<DetalleVentaDto> Detalles { get; set; } = new();
    }

    public class CrearVentaDto
    {
        public int? ClienteId { get; set; }
        public int UsuarioId { get; set; }
        public decimal Descuento { get; set; } = 0;
        public decimal Impuesto { get; set; } = 0;
        public string MetodoPago { get; set; } = "Efectivo";
        public string? Notas { get; set; }
        public List<CrearDetalleVentaDto> Detalles { get; set; } = new();
    }

    public class DetalleVentaDto
    {
        public int Id { get; set; }
        public int ProductoId { get; set; }
        public string ProductoNombre { get; set; } = string.Empty;
        public string? ProductoCodigo { get; set; }
        public decimal Cantidad { get; set; }
        public decimal PrecioUnitario { get; set; }
        public decimal Descuento { get; set; }
        public decimal Subtotal { get; set; }
        public bool EsPorPeso { get; set; }
    }

    public class CrearDetalleVentaDto
    {
        public int ProductoId { get; set; }
        public decimal Cantidad { get; set; }
        public decimal PrecioUnitario { get; set; }
        public decimal Descuento { get; set; } = 0;
    }
}
