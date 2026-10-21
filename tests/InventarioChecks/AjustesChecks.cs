using Microsoft.EntityFrameworkCore;
using PuntoAba.Data;
using PuntoAba.DTOs;
using PuntoAba.Models;
using PuntoAba.Services;
using PuntoAba.Presentation.Navegacion;
static class AjustesChecks
{
    public static async Task RunAsync(string carpeta)
    {
        var opciones=new DbContextOptionsBuilder<PuntoAbaDbContext>().UseSqlite($"Data Source={Path.Combine(carpeta,"ajustes.db")}").Options;
        var actor=new Acceso();
        await using(var db=new PuntoAbaDbContext(opciones))
        {
            await db.Database.MigrateAsync();
            db.Productos.AddRange(new Producto{Id=201,Codigo="AJ-P",Nombre="Ajuste piezas",CategoriaId=1,Stock=10,PrecioCompra=2,PrecioVenta=3},new Producto{Id=202,Codigo="AJ-K",Nombre="Ajuste peso",CategoriaId=1,Stock=5,EsPorPeso=true,UnidadMedida="Kg",PrecioCompra=3,PrecioVenta=5});
            await db.SaveChangesAsync();
        }
        var control=new ControlInventarioService(opciones,actor);
        var suma=new OperacionExistenciasDto{ProductoId=201,Cantidad=5,Motivo="Corrección de captura",Referencia="PRUEBA",StockEsperado=10};
        var r=await control.RegistrarAsync(suma);var repetido=await control.RegistrarAsync(suma);
        if(r.StockAnterior!=10 || r.StockNuevo!=15 || repetido.Id!=r.Id || r.Usuario!="Responsable prueba" || r.Motivo!=suma.Motivo || r.Referencia!="PRUEBA")throw new Exception("Suma, auditoría o idempotencia incorrecta");
        var resta=await control.RegistrarAsync(new(){ProductoId=201,Cantidad=-2,Motivo="Corrección de sobrante",StockEsperado=15});
        if(resta.StockNuevo!=13 || resta.StockAnterior!=15 || resta.Cantidad!=2)throw new Exception("Resta incorrecta");
        async Task Rechazar(OperacionExistenciasDto dto)
        {
            try{await control.RegistrarAsync(dto);}catch(ArgumentException){return;}throw new Exception("Faltó rechazar un ajuste inválido");
        }
        await Rechazar(new(){ProductoId=201,Cantidad=0,Motivo="Cantidad cero"});
        await Rechazar(new(){ProductoId=201,Cantidad=.5m,Motivo="Piezas fraccionarias"});
        await Rechazar(new(){ProductoId=201,Cantidad=-14,Motivo="Saldo negativo"});
        await Rechazar(new(){ProductoId=201,Cantidad=1,Motivo="ab"});
        await Rechazar(new(){ProductoId=201,Cantidad=1,Motivo="Base desactualizada",StockEsperado=10});
        await control.RegistrarAsync(new(){ProductoId=202,Cantidad=.125m,Motivo="Corrección por peso",StockEsperado=5});
        await control.RegistrarAsync(new(){ProductoId=202,Cantidad=-.025m,Motivo="Corrección por peso",StockEsperado=5.125m});
        await Rechazar(new(){ProductoId=202,Cantidad=.0001m,Motivo="Demasiados decimales"});
        actor.Rol="Cajero";
        try {await control.RegistrarAsync(new(){ProductoId=201,Cantidad=1,Motivo="Sin permiso"});throw new Exception("El cajero pudo ajustar");}catch(UnauthorizedAccessException){}
        actor.Rol="Supervisor";await control.RegistrarAsync(new(){ProductoId=201,Cantidad=1,Motivo="Supervisor autorizado",StockEsperado=13});
        await using(var db=new PuntoAbaDbContext(opciones))
        {
            if((await db.Productos.FindAsync(201))!.Stock!=14 || (await db.Productos.FindAsync(202))!.Stock!=5.100m || await db.MovimientosInventario.CountAsync()!=5 || await db.MovimientosInventario.AnyAsync(m=>m.UsuarioId!=1 || m.TipoMovimiento!="Ajuste"))throw new Exception("Persistencia o rechazo atómico incorrecto");
        }
        if(MenuAplicacion.Pantallas.Count(p=>p.Grupo=="Inventario" && MenuAplicacion.EsCorrecciones(p.Ruta))!=1 || !MenuAplicacion.PuedeAbrir("inventario/correcciones","Cajero") || MenuAplicacion.PuedeAbrir("inventario/ajustes","Cajero") || !MenuAplicacion.EsInventario("inventario/conteos"))throw new Exception("Agrupación o permisos incorrectos");
        Console.WriteLine("OK: Ajustes suman/restan piezas y peso, conservan auditoría, no duplican reintentos y rechazan datos inválidos, base antigua y usuarios sin permiso.");
    }
    private class Acceso:IAccesoOperacion
    {
        public string Rol="Admin";
        public Task<UsuarioDto> ExigirAsync(params string[] roles)
        {
            if(!roles.Contains(Rol))throw new UnauthorizedAccessException();
            return Task.FromResult(new UsuarioDto{Id=1,Rol=Rol,Activo=true,NombreCompleto="Responsable prueba"});
        }
    }
}
