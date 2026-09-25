namespace PuntoAba.DTOs
{
    public class ClienteDto
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string? Apellido { get; set; }
        public string NombreCompleto => $"{Nombre} {Apellido}".Trim();
        public string? Telefono { get; set; }
        public string? Email { get; set; }
        public string? Direccion { get; set; }
        public string? DocumentoIdentidad { get; set; }
        public bool Activo { get; set; }
    }

    public class CrearClienteDto
    {
        public string Nombre { get; set; } = string.Empty;
        public string? Apellido { get; set; }
        public string? Telefono { get; set; }
        public string? Email { get; set; }
        public string? Direccion { get; set; }
        public string? DocumentoIdentidad { get; set; }
    }

    public class ActualizarClienteDto
    {
        public string? Nombre { get; set; }
        public string? Apellido { get; set; }
        public string? Telefono { get; set; }
        public string? Email { get; set; }
        public string? Direccion { get; set; }
        public string? DocumentoIdentidad { get; set; }
        public bool? Activo { get; set; }
    }
}
