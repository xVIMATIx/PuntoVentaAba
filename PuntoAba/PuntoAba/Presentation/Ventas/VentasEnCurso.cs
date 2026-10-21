using PuntoAba.DTOs;
namespace PuntoAba.Presentation.Ventas;

public sealed class BorradorVenta
{
    public CarritoVenta Carrito { get; } = new();
    public decimal? Descuento { get; set; }
    public decimal? Recibido { get; set; }
    public decimal PorcentajeImpuesto { get; set; }
    public bool AplicarImpuesto { get; set; }
    public int ClienteId { get; set; } = 1;
    public string MetodoPago { get; set; } = "Efectivo";
    public Guid OperacionId { get; set; } = Guid.NewGuid();
    public string Error { get; set; } = "";
    public decimal DescuentoAplicado => Descuento ?? 0;
    public decimal Impuesto => AplicarImpuesto ? Math.Max(0, Carrito.Subtotal - DescuentoAplicado) * PorcentajeImpuesto / 100 : 0;
    public decimal Total => Carrito.Subtotal - DescuentoAplicado + Impuesto;
    // El efectivo se compara con el mismo total de dos decimales que ve el cajero.
    public decimal TotalEfectivo => decimal.Round(Total, 2, MidpointRounding.AwayFromZero);
    public bool EfectivoValido => Recibido.HasValue && Recibido.Value >= 0 && TotalEfectivo >= 0
        && decimal.Round(Recibido.Value, 2) == Recibido.Value && Recibido.Value >= TotalEfectivo;
    public decimal Cambio => EfectivoValido ? Recibido!.Value - TotalEfectivo : 0;
    public bool ProductosActivos => Carrito.Items.All(i => i.Producto.Activo);
    public void Reiniciar()
    {
        Carrito.Vaciar(); Descuento = Recibido = null; PorcentajeImpuesto = 0;
        AplicarImpuesto = false; ClienteId = 1; MetodoPago = "Efectivo";
        OperacionId = Guid.NewGuid(); Error = "";
    }
}

// Estado por circuito y usuario: cambiar de pantalla no destruye los cinco borradores.
public sealed class VentasEnCurso
{
    private int usuarioId = -1;
    public BorradorVenta[] Ventas { get; private set; } = Crear();
    public int Indice { get; private set; }
    public BorradorVenta Actual => Ventas[Indice];
    public bool Restaurado { get; set; }
    private static BorradorVenta[] Crear() => Enumerable.Range(0, 5).Select(_ => new BorradorVenta()).ToArray();
    public void PrepararUsuario(int id)
    {
        if (id == usuarioId) return;
        usuarioId = id; Ventas = Crear(); Indice = 0; Restaurado = false;
    }
    public void Seleccionar(int indice)
    {
        if (indice < 0 || indice >= Ventas.Length) throw new ArgumentOutOfRangeException(nameof(indice));
        Indice = indice;
    }
    public void ActualizarExistencias(IEnumerable<ProductoDto> productos)
    {
        var catalogo = productos.ToDictionary(p => p.Id);
        foreach (var item in Ventas.SelectMany(v => v.Carrito.Items))
        {
            if (catalogo.TryGetValue(item.Producto.Id, out var p)) { item.Producto.Stock = p.Stock; item.Producto.Activo = p.Activo; }
            else { item.Producto.Stock = 0; item.Producto.Activo = false; }
        }
    }
    public PedidosGuardados Exportar() => new()
    {
        Activa = Indice,
        Ventas = Ventas.Select(v => new PedidoGuardado
        {
            OperacionId = v.OperacionId, Descuento = v.Descuento, Recibido = v.Recibido,
            PorcentajeImpuesto = v.PorcentajeImpuesto, AplicarImpuesto = v.AplicarImpuesto,
            ClienteId = v.ClienteId, MetodoPago = v.MetodoPago,
            Lineas = v.Carrito.Items.Select(i => new LineaGuardada { ProductoId = i.Producto.Id, Cantidad = i.Cantidad }).ToList()
        }).ToList()
    };
    public bool Restaurar(PedidosGuardados estado, IEnumerable<ProductoDto> productos, IEnumerable<int> clientes)
    {
        var catalogo = productos.ToDictionary(p => p.Id); var idsClientes = clientes.ToHashSet();
        var aviso = false; var nuevos = Crear();
        foreach (var par in (estado.Ventas ?? new()).Take(5).Select((v,i) => (v,i)))
        {
            if (par.v == null) { aviso = true; continue; }
            var origen = par.v; var v = nuevos[par.i];
            v.OperacionId = origen.OperacionId == Guid.Empty ? Guid.NewGuid() : origen.OperacionId;
            v.Descuento = origen.Descuento >= 0 ? origen.Descuento : null;
            v.Recibido = origen.Recibido >= 0 ? origen.Recibido : null;
            v.PorcentajeImpuesto = Math.Clamp(origen.PorcentajeImpuesto, 0, 100);
            v.AplicarImpuesto = origen.AplicarImpuesto;
            v.ClienteId = idsClientes.Contains(origen.ClienteId) ? origen.ClienteId : 1;
            v.MetodoPago = new[] { "Efectivo", "Tarjeta", "Transferencia", "Crédito" }.Contains(origen.MetodoPago) ? origen.MetodoPago : "Efectivo";
            foreach (var linea in (origen.Lineas ?? new()).Take(200))
            {
                if (linea == null || !catalogo.TryGetValue(linea.ProductoId, out var p) || !v.Carrito.Recuperar(p, linea.Cantidad)) aviso = true;
            }
        }
        Ventas = nuevos; Indice = Math.Clamp(estado.Activa, 0, 4);
        return aviso;
    }
}
public sealed class PedidosGuardados
{
    public int Activa { get; set; }
    public List<PedidoGuardado> Ventas { get; set; } = new();
}
public sealed class PedidoGuardado
{
    public Guid OperacionId { get; set; }
    public decimal? Descuento { get; set; }
    public decimal? Recibido { get; set; }
    public decimal PorcentajeImpuesto { get; set; }
    public bool AplicarImpuesto { get; set; }
    public int ClienteId { get; set; } = 1;
    public string MetodoPago { get; set; } = "Efectivo";
    public List<LineaGuardada> Lineas { get; set; } = new();
}
public sealed class LineaGuardada
{
    public int ProductoId { get; set; }
    public decimal Cantidad { get; set; }
}
