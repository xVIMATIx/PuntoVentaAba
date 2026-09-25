namespace PuntoAba.DTOs
{
    public class SincronizacionDto
    {
        public List<ProductoDto>? Productos { get; set; }
        public List<VentaDto>? Ventas { get; set; }
        public List<ClienteDto>? Clientes { get; set; }
        public DateTime FechaSincronizacion { get; set; } = DateTime.UtcNow;
        public string DispositivoOrigen { get; set; } = string.Empty;
    }

    public class SincronizacionResponseDto
    {
        public bool Exitoso { get; set; }
        public string Mensaje { get; set; } = string.Empty;
        public int ProductosSincronizados { get; set; }
        public int VentasSincronizadas { get; set; }
        public int ClientesSincronizados { get; set; }
        public DateTime FechaSincronizacion { get; set; }
    }

    public class EstadoSincronizacionDto
    {
        public DateTime? UltimaSincronizacion { get; set; }
        public int RegistrosPendientes { get; set; }
        public bool HayCambiosPendientes { get; set; }
    }
}
