namespace PuntoAba.DTOs;
public class EstadisticasDashboard
    {
        public VentasStat VentasHoy { get; set; } = new();
        public VentasStat VentasMes { get; set; } = new();
        public InventarioStat Inventario { get; set; } = new();
        public ClientesStat Clientes { get; set; } = new();
        public List<ProductoTop> ProductosTop { get; set; } = new();
        public List<VentaRecienteInicioDto> VentasRecientes { get; set; } = new();
        public List<ClienteFrecuenteInicioDto> ClientesFrecuentes { get; set; } = new();
    }

    public class VentasStat
    {
        public decimal Total { get; set; }
        public int Cantidad { get; set; }
    }

    public class InventarioStat
    {
        public int TotalProductos { get; set; }
        public int ProductosStockBajo { get; set; }
        public int Agotados { get; set; }
        public int StockBajoSinAgotados { get; set; }
        public decimal ValorInventario { get; set; }
    }

    public class ClientesStat
    {
        public int Registrados { get; set; }
        public int Total { get; set; }
        public int NuevosEsteMes { get; set; }
    }

    public class ProductoTop
    {
        public int ProductoId { get; set; }
        public string Nombre { get; set; } = "";
        public decimal CantidadVendida { get; set; }
        public decimal TotalVentas { get; set; }
    }
public record VentasDiaDto(DateTime Fecha, decimal Total, int Cantidad);
public record VentasMetodoDto(string MetodoPago, decimal Total, int Cantidad);

public record VentaRecienteInicioDto(int Id, DateTime Fecha, string Folio, decimal Importe);
public record ClienteFrecuenteInicioDto(int Id, string Nombre, decimal Importe);
