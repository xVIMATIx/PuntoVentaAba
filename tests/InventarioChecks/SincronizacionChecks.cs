using Microsoft.EntityFrameworkCore;
using PuntoAba.Data;
using PuntoAba.DTOs;
using PuntoAba.Models;
using PuntoAba.Services;
static class SincronizacionChecks
{
    public static async Task RunAsync(string carpeta)
    {
        var opciones=new DbContextOptionsBuilder<PuntoAbaDbContext>().UseSqlite($"Data Source={Path.Combine(carpeta,"sync-pendientes.db")}").Options;
        var actor=new Acceso();
        await using var db=new PuntoAbaDbContext(opciones);await db.Database.MigrateAsync();
        db.Productos.Add(new Producto{Id=401,Codigo="SYNC-PRUEBA",Nombre="Producto prueba sincronización",CategoriaId=1,Stock=10,PrecioCompra=2,PrecioVenta=5});await db.SaveChangesAsync();
        var ventaService=new VentaService(db,actor);
        var venta=await ventaService.CrearAsync(new(){Recibido=10,Detalles=new(){new(){ProductoId=401,Cantidad=1,PrecioUnitario=5}}});
        var guardada=await db.Ventas.FindAsync(venta.Id);guardada!.FechaVenta=DateTime.UtcNow.AddDays(-60);await db.SaveChangesAsync();
        var servicio=new SincronizacionService(db,new ProductoService(db,actor),ventaService,actor);
        var antesStock=(await db.Productos.FindAsync(401))!.Stock;var antesMovimientos=await db.MovimientosInventario.CountAsync();
        var datos=await servicio.ObtenerDatosParaSincronizarAsync(DateTime.UtcNow);
        if(datos.Ventas?.Count!=1 || datos.Ventas[0].Id!=venta.Id || datos.Ventas[0].Recibido!=10 || datos.Ventas[0].Cambio!=5 || datos.Ventas[0].Detalles.Count!=1)throw new Exception("La exportación omitió pendientes antiguos o los datos del recibo");
        var r=await servicio.SincronizarDatosAsync(datos);if(!r.Exitoso||r.VentasSincronizadas!=1||(await servicio.ObtenerEstadoSincronizacionAsync()).RegistrosPendientes!=0)throw new Exception("La cola no quedó sincronizada");
        await servicio.SincronizarDatosAsync(datos);
        var siguiente=await servicio.ObtenerDatosParaSincronizarAsync(DateTime.UtcNow);
        if(siguiente.Ventas?.Count!=0 || await db.Ventas.CountAsync()!=1 || (await db.Productos.FindAsync(401))!.Stock!=antesStock || await db.MovimientosInventario.CountAsync()!=antesMovimientos || guardada.Recibido!=10 || guardada.Cambio!=5)throw new Exception("La sincronización duplicó operaciones o alteró el cobro/stock");
        Console.WriteLine("OK: Sincronización incluye ventas pendientes anteriores al último proceso, conserva recibo y limpia la cola sin duplicar ventas ni descontar stock otra vez.");
    }
    private class Acceso:IAccesoOperacion{public Task<UsuarioDto> ExigirAsync(params string[] roles)=>Task.FromResult(new UsuarioDto{Id=1,Rol="Admin",Activo=true});}
}
