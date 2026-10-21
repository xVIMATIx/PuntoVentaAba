using Microsoft.EntityFrameworkCore;
using PuntoAba.Data;
using PuntoAba.DTOs;
using PuntoAba.Services;
static class InicioChecks
{
    public static async Task RunAsync(DbContextOptions<PuntoAbaDbContext> opciones)
    {
        var actor = new AccesoPrueba();
        var clientes = new ClienteService(opciones, actor);
        var primero = await clientes.CrearAsync(new() { Nombre = "Cliente ranking uno" });
        var segundo = await clientes.CrearAsync(new() { Nombre = "Cliente ranking dos" });
        foreach (var registro in new[] { (primero.Id, 100m), (segundo.Id, 500m), (primero.Id, 150m), (1, 900m) })
        {
            await using var db = new PuntoAbaDbContext(opciones);
            await new VentaService(db, actor).CrearAsync(new() { Recibido = registro.Item2, ClienteId = registro.Item1,
                Detalles = new() { new() { ProductoId = 101, Cantidad = 1, PrecioUnitario = registro.Item2 } } });
        }
        var reporte = await new DashboardService(opciones, actor).ObtenerEstadisticasAsync();
        if (reporte.ClientesFrecuentes.Any(c => c.Id == 1) || reporte.ClientesFrecuentes[0].Id != segundo.Id ||
            reporte.ClientesFrecuentes.Single(c => c.Id == primero.Id).Importe != 250m)
            throw new Exception("El ranking debe agrupar compras del mes y excluir Cliente General.");
        if (reporte.VentasRecientes.Count != 5 || reporte.VentasRecientes.Select(v => v.Id).Distinct().Count() != 5)
            throw new Exception("El inicio debe mostrar las últimas cinco ventas.");
        if (reporte.Inventario.ProductosStockBajo != reporte.Inventario.Agotados + reporte.Inventario.StockBajoSinAgotados)
            throw new Exception("No deben duplicarse agotados y stock bajo.");
        if (!PuntoAba.Presentation.Navegacion.MenuAplicacion.PuedeAbrir("dashboard", "Cajero") ||
            PuntoAba.Presentation.Navegacion.MenuAplicacion.PuedeAbrir("usuarios", "Supervisor") ||
            PuntoAba.Presentation.Navegacion.MenuAplicacion.PuedeAbrir("inventario/ajustes", "Cajero"))
            throw new Exception("La nueva navegación debe conservar permisos.");
        Console.WriteLine("OK: Inicio muestra ventas recientes, ranking mensual sin Cliente General y existencias sin duplicar categorías.");
    }
    private class AccesoPrueba : IAccesoOperacion
    {
        public Task<UsuarioDto> ExigirAsync(params string[] roles) => Task.FromResult(new UsuarioDto { Id = 1, Rol = "Admin", Activo = true });
    }
}
