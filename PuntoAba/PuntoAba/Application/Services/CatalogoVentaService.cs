using Microsoft.EntityFrameworkCore;
using PuntoAba.Data;
namespace PuntoAba.Services;

public interface ICatalogoVentaService
{
    Task<Dictionary<int, int>> FrecuenciaUltimos30DiasAsync();
}
// Solo devuelve frecuencia por producto, sin datos de clientes o importes de tickets.
public class CatalogoVentaService(DbContextOptions<PuntoAbaDbContext> opciones, IAccesoOperacion acceso) : ICatalogoVentaService
{
    public async Task<Dictionary<int, int>> FrecuenciaUltimos30DiasAsync()
    {
        await acceso.ExigirAsync("Admin", "Supervisor", "Cajero");
        await using var db = new PuntoAbaDbContext(opciones);
        var fin = DateTime.UtcNow;
        var inicio = fin.AddDays(-30);
        var pares = await db.DetallesVenta.AsNoTracking()
            .Where(d => d.Venta!.EstadoVenta == "Completada" && d.Venta.FechaVenta >= inicio && d.Venta.FechaVenta <= fin)
            .Select(d => new { d.ProductoId, d.VentaId }).Distinct().ToListAsync();
        return pares.GroupBy(x => x.ProductoId).ToDictionary(g => g.Key, g => g.Count());
    }
}
