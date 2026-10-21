using System.ComponentModel.DataAnnotations;

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
        public decimal StockObjetivo { get; set; }
        public string? ProveedorHabitual { get; set; }
        public int CategoriaId { get; set; }
        public string? CategoriaNombre { get; set; }
        public string? UnidadMedida { get; set; }
        public string? ImagenUrl { get; set; }
        public bool Activo { get; set; }
        public bool EsPorPeso { get; set; }
    }

    public class CrearProductoDto
    {
        [Range(typeof(decimal), "0", "999999999999999.999", ErrorMessage = "El objetivo no puede ser negativo.")]
        public decimal StockObjetivo { get; set; }
        [StringLength(200)] public string? ProveedorHabitual { get; set; }
        [Required(ErrorMessage = "Ingresa o escanea el código del producto.")]
        [StringLength(100, ErrorMessage = "El código admite hasta 100 caracteres.")]
        public string Codigo { get; set; } = string.Empty;
        [Required(ErrorMessage = "Ingresa el nombre del producto.")]
        [StringLength(200, ErrorMessage = "El nombre admite hasta 200 caracteres.")]
        public string Nombre { get; set; } = string.Empty;
        [StringLength(1000, ErrorMessage = "La descripción admite hasta 1000 caracteres.")]
        public string? Descripcion { get; set; }
        [Range(typeof(decimal), "0", "9999999999999999.99", ErrorMessage = "El precio de compra no puede ser negativo.")]
        public decimal PrecioCompra { get; set; }
        [Range(typeof(decimal), "0", "9999999999999999.99", ErrorMessage = "El precio de venta no puede ser negativo.")]
        public decimal PrecioVenta { get; set; }
        [Range(typeof(decimal), "0", "999999999999999.999", ErrorMessage = "Las existencias no pueden ser negativas.")]
        public decimal Stock { get; set; }
        [Range(typeof(decimal), "0", "999999999999999.999", ErrorMessage = "El mínimo de existencias no puede ser negativo.")]
        public decimal StockMinimo { get; set; }
        [Range(1, int.MaxValue, ErrorMessage = "Selecciona una categoría.")]
        public int CategoriaId { get; set; }
        [StringLength(50, ErrorMessage = "La unidad de medida admite hasta 50 caracteres.")]
        public string? UnidadMedida { get; set; }
        public string? ImagenUrl { get; set; }
        public bool EsPorPeso { get; set; }
    }

    public class ActualizarProductoDto
    {
        public decimal? StockObjetivo { get; set; }
        public string? ProveedorHabitual { get; set; }
        public string? Codigo { get; set; }
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
