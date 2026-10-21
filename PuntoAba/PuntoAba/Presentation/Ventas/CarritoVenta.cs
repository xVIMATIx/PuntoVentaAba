using PuntoAba.DTOs;
namespace PuntoAba.Presentation.Ventas;

/// <summary>Estado de una venta en preparación. No escribe existencias ni depende de Blazor.</summary>
public class CarritoVenta
{
    private readonly List<ItemCarrito> items = new();
    public IReadOnlyList<ItemCarrito> Items => items.AsReadOnly();
    public decimal Subtotal => items.Sum(i => i.Producto.PrecioVenta * i.Cantidad);

    public bool Agregar(ProductoDto producto, decimal cantidad)
    {
        var item = items.FirstOrDefault(i => i.Producto.Id == producto.Id);
        var nueva = (item?.Cantidad ?? 0) + cantidad;
        if (cantidad <= 0 || !Valida(producto, nueva)) return false;
        if (item == null) items.Add(new ItemCarrito(producto, nueva)); else item.Cantidad = nueva;
        return true;
    }
    public bool CambiarCantidad(int id, decimal cantidad)
    {
        var item = items.FirstOrDefault(i => i.Producto.Id == id);
        if (item == null || !Valida(item.Producto, cantidad)) return false;
        item.Cantidad = cantidad; return true;
    }
    // Recupera el borrador sin limitar la cantidad al stock registrado.
    internal bool Recuperar(ProductoDto producto, decimal cantidad)
    {
        if (!Valida(producto, cantidad) || items.Any(i => i.Producto.Id == producto.Id)) return false;
        items.Add(new ItemCarrito(producto, cantidad)); return true;
    }
    public void Eliminar(int id) => items.RemoveAll(i => i.Producto.Id == id);
    public void Vaciar() => items.Clear();
    private static bool Valida(ProductoDto p, decimal n) =>
        n > 0 && n <= 999999999999999.999m && decimal.Round(n, 3) == n && (p.EsPorPeso || decimal.Truncate(n) == n);
}
public class ItemCarrito(ProductoDto producto, decimal cantidad)
{
    public ProductoDto Producto { get; } = producto;
    public decimal Cantidad { get; internal set; } = cantidad;
}
public record CambioCantidadCarrito(int ProductoId, decimal Cantidad);
