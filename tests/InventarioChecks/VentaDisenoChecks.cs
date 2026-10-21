using Microsoft.EntityFrameworkCore;
using PuntoAba.Data;
using PuntoAba.DTOs;
using PuntoAba.Models;
using PuntoAba.Services;
using PuntoAba.Presentation.Ventas;

static class VentaDisenoChecks
{
    public static async Task RunAsync(string temp, IAccesoOperacion acceso)
    {
        void Check(bool ok, string texto) { if (!ok) throw new Exception(texto); Console.WriteLine("OK: " + texto); }
        var p = new List<ProductoDto> {
            new() {Id=1,Nombre="Zanahoria",CategoriaId=1,Stock=2,Activo=true},
            new() {Id=2,Nombre="Arroz",CategoriaId=1,Stock=3,Activo=true},
            new() {Id=3,Nombre="Agotado",CategoriaId=1,Stock=0,Activo=true},
            new() {Id=4,Nombre="Bebida",CategoriaId=2,Stock=2,Activo=true} };
        var frecuencia = new Dictionary<int,int> { [1]=5, [2]=1, [3]=100 };
        Check(OrdenCatalogo.Aplicar(p,"",0,frecuencia,Array.Empty<int>()).Select(x=>x.Id).SequenceEqual(new[]{1,2,4,3}), "Todos ordena por tickets y coloca agotados al final");
        Check(OrdenCatalogo.Aplicar(p,"",1,frecuencia,Array.Empty<int>()).Select(x=>x.Id).SequenceEqual(new[]{2,1,3}), "Cada categoría ordena A-Z y respeta agotados");
        Check(OrdenCatalogo.Aplicar(p,"",-1,frecuencia,new[]{4,2}).Select(x=>x.Id).SequenceEqual(new[]{4,2}), "Favoritos conserva el orden elegido");
        var espacio = new VentasEnCurso(); espacio.PrepararUsuario(1);
        var pieza = new ProductoDto { Id=91,Nombre="Pieza",Stock=10,PrecioVenta=5,Activo=true };
        var granel = new ProductoDto { Id=92,Nombre="Granel",Stock=5,PrecioVenta=32,Activo=true,EsPorPeso=true,UnidadMedida="kg" };
        espacio.Actual.Carrito.Agregar(pieza,2); espacio.Actual.Descuento=1; espacio.Actual.Recibido=100;
        espacio.Actual.ClienteId=7; espacio.Actual.MetodoPago="Tarjeta"; espacio.Actual.PorcentajeImpuesto=10; espacio.Actual.AplicarImpuesto=true;
        var operacionPrimera=espacio.Actual.OperacionId;
        espacio.Seleccionar(1); espacio.Actual.Carrito.Agregar(granel,.250m);
        Check(espacio.Actual.Total==8 && espacio.Actual.Descuento==null && espacio.Actual.Recibido==null,"Venta 2 comienza independiente con campos vacíos");
        var serializado=System.Text.Json.JsonSerializer.Serialize(espacio.Exportar());
        var recuperado=new VentasEnCurso();recuperado.PrepararUsuario(1);
        Check(!recuperado.Restaurar(System.Text.Json.JsonSerializer.Deserialize<PedidosGuardados>(serializado)!,new[]{pieza,granel},new[]{1,7}),"Recuperación de pedidos desde datos de sesión válidos");
        Check(recuperado.Indice==1 && recuperado.Actual.Total==8 && recuperado.Ventas[0].Total==9.9m && recuperado.Ventas[0].ClienteId==7 && recuperado.Ventas[0].Recibido==100 && recuperado.Ventas[0].MetodoPago=="Tarjeta" && recuperado.Ventas[0].OperacionId==operacionPrimera,"Recuperar conserva cantidades, cliente, descuento, recibido, impuesto y operación de cada pestaña");
        recuperado.Actual.Reiniciar();
        Check(recuperado.Actual.Carrito.Items.Count==0 && recuperado.Ventas[0].Carrito.Items.Count==1 && recuperado.Ventas[0].Total==9.9m,"Cancelar o finalizar una venta no borra los otros pedidos");
        pieza.Stock=1;
        var conMenosStock=new VentasEnCurso();conMenosStock.PrepararUsuario(1);
        conMenosStock.Restaurar(System.Text.Json.JsonSerializer.Deserialize<PedidosGuardados>(serializado)!,new[]{pieza,granel},new[]{1,7});
        Check(conMenosStock.Ventas[0].Carrito.Items[0].Cantidad==2 && conMenosStock.Ventas[0].ProductosActivos,"Recuperar conserva cantidades superiores al stock y permite cobrar productos activos");
        conMenosStock.PrepararUsuario(2);
        Check(conMenosStock.Ventas.All(v=>v.Carrito.Items.Count==0) && !conMenosStock.Restaurado,"Cambiar de usuario separa los pedidos de la sesión anterior");
        var efectivo=new BorradorVenta();efectivo.Carrito.Agregar(new ProductoDto{Id=200,Activo=true,PrecioVenta=10},1);
        Check(!efectivo.EfectivoValido && efectivo.Cambio==0,"Cobro en efectivo exige capturar un monto; vacío no cuenta como pago");
        foreach(var n in new[]{0m,-1m,9.99m,10.001m}) { efectivo.Recibido=n;Check(!efectivo.EfectivoValido,"Recibido insuficiente, negativo o con más de dos decimales no confirma: "+n); }
        efectivo.Recibido=10;Check(efectivo.EfectivoValido && efectivo.Cambio==0,"Monto exacto permite confirmar sin cambio");
        efectivo.Recibido=20;Check(efectivo.EfectivoValido && efectivo.Cambio==10,"Efectivo suficiente calcula el cambio que debe entregarse");
        efectivo.Descuento=10;efectivo.Recibido=null;Check(!efectivo.EfectivoValido,"Incluso un total cero requiere un monto explícito");
        efectivo.Recibido=0;Check(efectivo.EfectivoValido,"Un total cero acepta cero explícito");
        efectivo.Reiniciar();efectivo.Carrito.Agregar(new ProductoDto{Id=201,Activo=true,EsPorPeso=true,PrecioVenta=1.01m},.5m);
        efectivo.Recibido=decimal.Parse(efectivo.Total.ToString("F2",System.Globalization.CultureInfo.InvariantCulture),System.Globalization.CultureInfo.InvariantCulture);
        Check(efectivo.EfectivoValido && efectivo.Cambio==0,"El cobro de productos por peso coincide con el total de dos decimales mostrado");
        var reloj = DateTime.Now; var peso = new PesoVenta();
        peso.Registrar(1m,reloj.AddSeconds(-2),true,"ST 1.000 kg");
        Check(peso.Obtener(true,reloj)==null,"Una sola lectura no autoriza peso automático");
        peso.Registrar(1.001m,reloj.AddSeconds(-1),true,"ST 1.001 kg");peso.Registrar(1m,reloj,true,"ST 1.000 kg");
        Check(peso.Obtener(true,reloj)==1m,"Tres lecturas recientes y estables permiten agregar el peso");
        Check(peso.Obtener(true,reloj.AddSeconds(5))==null && peso.Obtener(false,reloj)==null,"Lectura vieja o desconexión bloquea peso automático");
        peso.Registrar(2m,reloj.AddSeconds(1),true,"US 2.000 kg");
        Check(peso.Obtener(true,reloj.AddSeconds(1))==null,"Trama inestable descarta el peso anterior");
        var opciones=new DbContextOptionsBuilder<PuntoAbaDbContext>().UseSqlite($"Data Source={Path.Combine(temp,"catalogo.db")}").Options;
        await using(var db=new PuntoAbaDbContext(opciones)) {
            await db.Database.MigrateAsync();
            db.Productos.Add(new Producto{Id=201,Codigo="DEMO-TEST",Nombre="Prueba",CategoriaId=1,Stock=5});
            for(var i=0;i<4;i++) {
                var venta=new Venta{NumeroVenta="TEST-"+i,UsuarioId=1,OperacionId=Guid.NewGuid(),EstadoVenta=i==2?"Cancelada":"Completada",FechaVenta=DateTime.UtcNow.AddDays(i==3?-31:-1),DetallesVenta=new List<DetalleVenta>{new(){ProductoId=201,Cantidad=1,PrecioUnitario=1},new(){ProductoId=201,Cantidad=5,PrecioUnitario=1}}};
                db.Ventas.Add(venta);
            }
            await db.SaveChangesAsync();
        }
        var ranking=await new CatalogoVentaService(opciones,acceso).FrecuenciaUltimos30DiasAsync();
        Check(ranking[201]==2,"Ranking cuenta tickets distintos; excluye ventas canceladas y fuera de 30 días");
    }
}
