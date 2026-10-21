using Microsoft.EntityFrameworkCore;
using PuntoAba.Data;
using PuntoAba.DTOs;
using PuntoAba.Models;
using PuntoAba.Services;
static class MermasChecks
{
    public static async Task RunAsync(string carpeta)
    {
        var opciones=new DbContextOptionsBuilder<PuntoAbaDbContext>().UseSqlite($"Data Source={Path.Combine(carpeta,"mermas.db")}").Options;
        var actor=new Acceso();
        await using(var db=new PuntoAbaDbContext(opciones)){await db.Database.MigrateAsync();db.Productos.Add(new Producto{Id=301,Codigo="MD-1",Nombre="Producto merma prueba",CategoriaId=1,Stock=10,PrecioCompra=2,PrecioVenta=3});await db.SaveChangesAsync();}
        var control=new ControlInventarioService(opciones,actor);var inventario=new InventarioService(opciones,actor);
        var stock=10m;
        foreach(var dato in new[]{("Merma",2m,8m),("DevolucionCliente",1m,9m),("DevolucionNoVendible",1m,9m),("DevolucionProveedor",2m,7m)})
        {
            var dto=new OperacionExistenciasDto{ProductoId=301,Tipo=dato.Item1,Cantidad=dato.Item2,Motivo=dato.Item1=="DevolucionProveedor"?"Proveedor: Distribuidor de prueba. Producto defectuoso":"Daño de empaque",Referencia="REF-"+dato.Item1,StockEsperado=stock};
            var r=await control.RegistrarAsync(dto);var repetido=await control.RegistrarAsync(dto);
            if(r.StockAnterior!=stock || r.StockNuevo!=dato.Item3 || repetido.Id!=r.Id || r.Usuario!="Responsable prueba")throw new Exception("Movimiento de merma/devolución o reintento incorrecto");stock=dato.Item3;
        }
        async Task Rechazar(OperacionExistenciasDto dto){try{await control.RegistrarAsync(dto);}catch(ArgumentException){return;}throw new Exception("Se aceptó un retiro inválido");}
        await Rechazar(new(){ProductoId=301,Tipo="Merma",Cantidad=8,Motivo="Saldo insuficiente"});
        await Rechazar(new(){ProductoId=301,Tipo="Merma",Cantidad=.5m,Motivo="Piezas fraccionarias"});
        await Rechazar(new(){ProductoId=301,Tipo="DevolucionCliente",Cantidad=1,Motivo="Falta referencia"});
        await Rechazar(new(){ProductoId=301,Tipo="DevolucionNoVendible",Cantidad=0,Motivo="Cantidad cero",Referencia="V-1"});
        await Rechazar(new(){ProductoId=301,Tipo="Merma",Cantidad=1,Motivo="Existencias antiguas",StockEsperado=10});
        await control.RegistrarAsync(new(){ProductoId=301,Cantidad=1,Motivo="Ajuste fuera del historial"});
        var a=await inventario.ObtenerMovimientosAsync(new(){SoloMermasDevoluciones=true,TamanoPagina=2});
        var b=await inventario.ObtenerMovimientosAsync(new(){SoloMermasDevoluciones=true,TamanoPagina=2,Pagina=2});
        if(a.Total!=4 || b.Total!=4 || a.Movimientos.Concat(b.Movimientos).Select(m=>m.Id).Distinct().Count()!=4 || a.Movimientos.Concat(b.Movimientos).Any(m=>m.Tipo=="Ajuste"))throw new Exception("El historial mezcla ajustes o pagina incorrectamente");
        if((await inventario.ObtenerMovimientosAsync(new())).Total!=5 || (await inventario.ObtenerMovimientosAsync(new(){SoloMermasDevoluciones=true,Tipo="Ajuste"})).Total!=0)throw new Exception("El filtro modificó el historial general");
        var proveedor=await inventario.ObtenerMovimientosAsync(new(){SoloMermasDevoluciones=true,Buscar="REF-DevolucionProveedor"});
        if(proveedor.Total!=1 || !proveedor.Movimientos[0].Motivo!.Contains("Distribuidor de prueba") || proveedor.Movimientos[0].Referencia!="REF-DevolucionProveedor")throw new Exception("Proveedor o referencia no recuperable");
        await using(var db=new PuntoAbaDbContext(opciones)){if((await db.Productos.FindAsync(301))!.Stock!=8 || await db.MovimientosInventario.CountAsync()!=5)throw new Exception("Los rechazos modificaron existencias");}
        Console.WriteLine("OK: Merma resta, devolución vendible suma, no vendible conserva stock y devolución a proveedor resta; historial exclusivo paginado, referencias y reintentos correctos.");
    }
    private class Acceso:IAccesoOperacion
    {public Task<UsuarioDto> ExigirAsync(params string[] roles)=>Task.FromResult(new UsuarioDto{Id=1,Rol="Admin",Activo=true,NombreCompleto="Responsable prueba"});}
}
