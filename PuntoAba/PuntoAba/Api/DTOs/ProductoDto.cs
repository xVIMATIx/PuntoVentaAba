namespace PuntoAba.DTOs
{
    public class ProductoDto
    {
        public int Id { get; set; }
        public string Codigo { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string? Descripcion { get; set; }
        public decimal PrecioCompra { get; set; }
        public decimal PrecioVenta { get; set; }
        public decimal Stock { get; set; }
        public decimal StockMinimo { get; set; }
        public int CategoriaId { get; set; }
        public string? CategoriaNombre { get; set; }
        public string? UnidadMedida { get; set; }
        public string? ImagenUrl { get; set; }
        public bool Activo { get; set; }
        public bool EsPorPeso { get; set; }
    }

    public class CrearProductoDto
    {
        public string Codigo { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string? Descripcion { get; set; }
        public decimal PrecioCompra { get; set; }
        public decimal PrecioVenta { get; set; }
        public decimal Stock { get; set; }
        public decimal StockMinimo { get; set; }
        public int CategoriaId { get; set; }
        public string? UnidadMedida { get; set; }
        public string? ImagenUrl { get; set; }
        public bool EsPorPeso { get; set; }
    }

    public class ActualizarProductoDto
    {
        public string? Nombre { get; set; }
        public string? Descripcion { get; set; }
        public decimal? PrecioCompra { get; set; }
        public decimal? PrecioVenta { get; set; }
        public decimal? Stock { get; set; }
        public decimal? StockMinimo { get; set; }
        public int? CategoriaId { get; set; }
        public string? UnidadMedida { get; set; }
        public string? ImagenUrl { get; set; }
        public bool? Activo { get; set; }
        public bool? EsPorPeso { get; set; }
    }
}
