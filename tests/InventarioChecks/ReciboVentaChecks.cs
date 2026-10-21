using Microsoft.EntityFrameworkCore;
using PuntoAba.Data;
using PuntoAba.DTOs;
using PuntoAba.Models;
using PuntoAba.Services;

static class ReciboVentaChecks
{
    public static async Task RunAsync(string temp, IAccesoOperacion actor)
    {
        void Check(bool ok, string nombre) { if (!ok) throw new Exception(nombre); Console.WriteLine("OK: " + nombre); }
        var options = new DbContextOptionsBuilder<PuntoAbaDbContext>().UseSqlite($"Data Source={Path.Combine(temp, "recibos.db")}").Options;
        await using (var db = new PuntoAbaDbContext(options))
        {
            // Simula una base anterior: la migración no debe inventar recibido ni cambio.
            await db.Database.MigrateAsync("20261017210542_ControlFisicoYRespaldos");
            await db.Database.ExecuteSqlRawAsync("INSERT INTO Ventas (NumeroVenta,FechaVenta,UsuarioId,Subtotal,Descuento,Impuesto,Total,MetodoPago,EstadoVenta,UltimaSincronizacion,Sincronizado) VALUES ('HISTORICA','2026-10-17',1,'10','0','0','10','Efectivo','Completada','2026-10-17',0)");
            await db.Database.MigrateAsync();
            var vieja = await new VentaService(db, actor).ObtenerPorIdAsync(1);
            Check(vieja?.Recibido == null && vieja?.Cambio == null && vieja?.Total == 10, "Migración conserva la venta anterior y deja efectivo histórico sin registrar");
            db.Productos.Add(new Producto { Id=301, Codigo="RECIBO", Nombre="Producto de recibo", CategoriaId=1, Stock=20, EsPorPeso=true, UnidadMedida="Kg", PrecioVenta=1.01m });
            await db.SaveChangesAsync();
        }
        CrearVentaDto Pedido(decimal? recibido, decimal cantidad=1, decimal precio=10) => new() { ClienteId=1, Recibido=recibido, Notas="Nota de prueba", Detalles=new() { new() { ProductoId=301, Cantidad=cantidad, PrecioUnitario=precio } } };
        int id;
        Guid operacion;
        await using (var db = new PuntoAbaDbContext(options))
        {
            var servicio = new VentaService(db, actor);
            var pedido = Pedido(20);
            operacion = pedido.OperacionId;
            var venta = await servicio.CrearAsync(pedido); id=venta.Id;
            Check(venta.Recibido==20 && venta.Cambio==10 && venta.Total==10 && venta.Notas=="Nota de prueba" && venta.Detalles[0].UnidadMedida=="Kg", "Venta devuelve efectivo, cambio calculado, unidad y notas");
        }
        await using (var db = new PuntoAbaDbContext(options))
        {
            var servicio = new VentaService(db, actor);
            var venta = await servicio.ObtenerPorIdAsync(id);
            Check(venta?.Recibido==20 && venta.Cambio==10, "Recibido y cambio sobreviven a cerrar y reabrir el contexto");
            var reintento=Pedido(100); reintento.OperacionId=operacion;
            venta=await servicio.CrearAsync(reintento);
            Check(venta.Id==id && venta.Recibido==20 && venta.Cambio==10 && await db.Ventas.CountAsync()==2, "Reintento conserva el cobro original y no duplica venta ni inventario");
            var stock=(await db.Productos.AsNoTracking().SingleAsync()).Stock;
            foreach (var recibido in new decimal?[] { null, -1m, 9.99m, 10.001m, 10000000000000000m })
            {
                try { await servicio.CrearAsync(Pedido(recibido)); throw new Exception("Se aceptó un efectivo inválido"); }
                catch (ArgumentException) { }
            }
            Check(await db.Ventas.CountAsync()==2 && await db.MovimientosInventario.CountAsync()==1 && (await db.Productos.AsNoTracking().SingleAsync()).Stock==stock, "Efectivo inválido rechaza el cobro y revierte venta y existencias");
            var peso=await servicio.CrearAsync(Pedido(.51m,.5m,1.01m));
            Check(peso.Cambio==0 && peso.Recibido==.51m && peso.Total==.505m, "Cambio de peso usa los centavos del total mostrado sin introducir diferencias de medio centavo");
            var gratis=await servicio.CrearAsync(Pedido(0,1,0));
            Check(gratis.Recibido==0 && gratis.Cambio==0, "Cobro de total cero conserva un cero explícito, distinto del histórico desconocido");
            var tarjeta=Pedido(null);tarjeta.MetodoPago="Tarjeta";
            var otro=await servicio.CrearAsync(tarjeta);
            Check(otro.Recibido==null && otro.Cambio==null, "Otros métodos no inventan efectivo ni cambio");
        }
    }
}
