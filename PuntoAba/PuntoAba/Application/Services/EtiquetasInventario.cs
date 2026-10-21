namespace PuntoAba.Services;
public static class EtiquetasInventario
{
    public static string Tipo(string tipo) => tipo switch { "DevolucionCliente" => "Devolución vendible", "DevolucionNoVendible" => "Devolución no vendible", "DevolucionProveedor" => "Devolución a proveedor", "Correccion" => "Corrección", _ => tipo };
}
