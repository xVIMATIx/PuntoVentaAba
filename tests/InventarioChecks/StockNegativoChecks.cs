using Microsoft.EntityFrameworkCore;
using PuntoAba.Data;
using PuntoAba.DTOs;
using PuntoAba.Models;
using PuntoAba.Services;
using PuntoAba.Presentation.Ventas;

static class StockNegativoChecks
{
    public static async Task RunAsync(string temp, IAccesoOperacion actor)
    {
        void Check(bool ok, string nombre) { if (!ok) throw new Exception(nombre); Console.WriteLine("OK: "+nombre); }
        async Task Rechazar(Func<Task> accion) { try { await accion(); } catch (ArgumentException) { return; } throw new Exception("Se aceptó una cantidad o producto inválido"); }
        var opciones=new DbContextOptionsBuilder<PuntoAbaDbContext>().UseSqlite($"Data Source={Path.Combine(temp,"stock-negativo.db")}").Options;
        await using var db=new PuntoAbaDbContext(opciones); await db.Database.MigrateAsync();
        db.Productos.AddRange(new Producto{Id=201,Codigo="SIN-STOCK",Nombre="Sin existencias",CategoriaId=1,Stock=0,PrecioCompra=2,PrecioVenta=5},
            new Producto{Id=202,Codigo="PESO-BAJO",Nombre="Granel",CategoriaId=1,Stock=.25m,StockMinimo=1,EsPorPeso=true,UnidadMedida="kg",PrecioCompra=20,PrecioVenta=32},
            new Producto{Id=203,Codigo="INACTIVO",Nombre="Inactivo",CategoriaId=1,Stock=0,Activo=false});
        await db.SaveChangesAsync();
        var ventas=new VentaService(db,actor);
        CrearVentaDto Pedido(int id,decimal n)=>new(){Recibido=1000,Detalles=new(){new(){ProductoId=id,Cantidad=n,PrecioUnitario=id==202?32:5}}};
        async Task<decimal> Stock(int id) { await using var lectura=new PuntoAbaDbContext(opciones); return (await lectura.Productos.FindAsync(id))!.Stock; }
        var pedido=Pedido(201,3);var venta=await ventas.CrearAsync(pedido);await ventas.CrearAsync(pedido);
        var movimiento=await db.MovimientosInventario.AsNoTracking().SingleAsync(m=>m.ProductoId==201);
        Check(await Stock(201)==-3 && movimiento.StockAnterior==0 && movimiento.StockNuevo==-3 && movimiento.Cantidad==3 && venta.EstadoVenta=="Completada", "Venta sin stock guarda saldo negativo y salida auditada sin duplicar al reintentar");
        await ventas.CrearAsync(Pedido(201,2));Check(await Stock(201)==-5,"Se puede seguir vendiendo con stock ya negativo");
        await ventas.CrearAsync(Pedido(202,1.25m));Check(await Stock(202)==-1m,"Venta por kilo supera las existencias y conserva tres decimales");
        var repetido=Pedido(201,2);repetido.Detalles.Add(new(){ProductoId=201,Cantidad=3,PrecioUnitario=5});await ventas.CrearAsync(repetido);
        Check(await Stock(201)==-10,"Líneas repetidas del mismo producto descuentan toda la cantidad");
        await Rechazar(()=>ventas.CrearAsync(Pedido(201,0)));await Rechazar(()=>ventas.CrearAsync(Pedido(201,-1)));
        await Rechazar(()=>ventas.CrearAsync(Pedido(201,.5m)));await Rechazar(()=>ventas.CrearAsync(Pedido(202,.1234m)));
        var invalido=Pedido(201,1);invalido.Detalles.Add(new(){ProductoId=203,Cantidad=1,PrecioUnitario=5});await Rechazar(()=>ventas.CrearAsync(invalido));
        Check(await Stock(201)==-10 && await db.Ventas.CountAsync()==4 && await db.MovimientosInventario.CountAsync()==5,"Cantidades inválidas e inactivos siguen rechazados; el fallo revierte toda la operación");
        var carrito=new CarritoVenta();var p=new ProductoDto{Id=1,Activo=true,Stock=-4,PrecioVenta=5};
        Check(carrito.Agregar(p,10) && carrito.CambiarCantidad(1,20) && !carrito.CambiarCantidad(1,0),"Carrito permite agregar y editar por encima de stock negativo, conservando cantidades positivas");
        var control=new ControlInventarioService(opciones,actor);
        var conteo=await control.IniciarConteoAsync(new(){201},Guid.NewGuid());conteo.Detalles[0].Contada=5;conteo.Detalles[0].Motivo="Conteo real después de captura pendiente";
        conteo=await control.GuardarConteoAsync(conteo);await control.ConfirmarConteoAsync(conteo.Id,conteo.Version);
        Check(await Stock(201)==5,"Un conteo físico corrige después el saldo negativo sin perder trazabilidad");
    }
}
