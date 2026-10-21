using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.IdentityModel.Tokens;
using Microsoft.JSInterop;
using PuntoAba.Data;
using PuntoAba.DTOs;
using PuntoAba.Models;
using PuntoAba.Services;

void Check(bool ok,string name){if(!ok)throw new Exception(name);Console.WriteLine("OK: "+name);}
async Task Reject<T>(Func<Task> action) where T:Exception {try{await action();}catch(T){return;}throw new Exception("Faltó rechazar "+typeof(T).Name);}
if (args.Length == 2 && args[0] == "--api-fixture") { await ModularChecks.PrepararApiAsync(args[1]); return; }
var temp=Path.Combine(Path.GetTempPath(),"PuntoAba-Checks-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(temp);
var path=Path.Combine(temp,"pruebas.db");var options=new DbContextOptionsBuilder<PuntoAbaDbContext>().UseSqlite($"Data Source={path};Default Timeout=3").Options;
var actor=new Actor();
await using(var db=new PuntoAbaDbContext(options))
{
    await db.Database.MigrateAsync();
    db.Usuarios.Add(new(){Id=2,NombreUsuario="cajero",NombreCompleto="Cajero de prueba",Email="cajero@example.test",PasswordHash="prueba",Rol="Cajero"});
    db.Productos.AddRange(new Producto{Id=101,Codigo="00111",Nombre="Piezas",CategoriaId=1,Stock=10,StockMinimo=8,StockObjetivo=30,PrecioCompra=2,PrecioVenta=5,UnidadMedida="Pieza",ProveedorHabitual="Proveedor A"},new Producto{Id=102,Codigo="00222",Nombre="Peso",CategoriaId=1,Stock=5,StockMinimo=6,EsPorPeso=true,UnidadMedida="Kg",PrecioCompra=3,PrecioVenta=7});await db.SaveChangesAsync();
}
var inventario=new InventarioService(options,actor);var control=new ControlInventarioService(options,actor);
async Task<decimal> Stock(int id){await using var db=new PuntoAbaDbContext(options);return (await db.Productos.FindAsync(id))!.Stock;}
CrearEntradaMercanciaDto Entrada()=>new(){Proveedor="Proveedor A",Documento="N-001",Detalles=new(){new(){ProductoId=101,Cantidad=2,PorCaja=true,PiezasPorCaja=12,CostoUnitario=2,StockEsperado=10}}};
actor.Rol="Cajero";actor.Id=2;
await Reject<UnauthorizedAccessException>(()=>inventario.RegistrarEntradaAsync(Entrada()));await Reject<UnauthorizedAccessException>(()=>control.RegistrarAsync(new(){ProductoId=101,Cantidad=-1,Motivo="Prueba de autorización"}));
actor.Rol="Admin";actor.Id=1;
Check(await Stock(101)==10,"Un cajero no puede recibir ni ajustar desde servicios internos");
var entrada=Entrada();var recibida=await inventario.RegistrarEntradaAsync(entrada);
Check(await Stock(101)==34 && recibida.Total==48,"Cajas a piezas y costo por unidad base");
await inventario.RegistrarEntradaAsync(entrada);Check(await Stock(101)==34,"Reintentar una recepción no duplica existencias");
await Reject<ArgumentException>(()=>inventario.RegistrarEntradaAsync(Entrada()));
var sugerencia=(await inventario.ObtenerResurtidoAsync()).Single(p=>p.ProductoId==102);Check(sugerencia.Objetivo==12 && sugerencia.Cantidad==7,"Resurtido automático conserva unidades por peso");
await using(var db=new PuntoAbaDbContext(options))
{
    var ps=new ProductoService(db,actor);
    await Reject<ArgumentException>(()=>ps.ActualizarAsync(101,new(){Stock=35}));await Reject<InvalidOperationException>(()=>ps.ActualizarStockAsync(101,1));
    await ps.ActualizarAsync(101,new(){StockObjetivo=40,ProveedorHabitual="Proveedor B"});
    var creado=await ps.CrearAsync(new(){Codigo="00333",Nombre="Nuevo",CategoriaId=1,Stock=3,StockMinimo=1,StockObjetivo=9});
    Check((await db.MovimientosInventario.SingleAsync(m=>m.ProductoId==creado.Id)).UsuarioId==1,"Apertura identificada y edición directa de existencias bloqueada");
}
actor.Id=2;actor.Rol="Cajero";
var conteo=await control.IniciarConteoAsync(new(){101},Guid.NewGuid());conteo.Detalles[0].Contada=33;conteo.Detalles[0].Motivo="Diferencia comprobada en estante";conteo=await control.GuardarConteoAsync(conteo);
await Reject<UnauthorizedAccessException>(()=>control.ConfirmarConteoAsync(conteo.Id,conteo.Version));
actor.Id=1;actor.Rol="Admin";
await control.RegistrarAsync(new(){ProductoId=101,Tipo="Merma",Cantidad=1,Motivo="Producto dañado",StockEsperado=34});
await Reject<ArgumentException>(()=>control.ConfirmarConteoAsync(conteo.Id,conteo.Version));
conteo=await control.ActualizarBaseConteoAsync(conteo.Id,conteo.Version);Check(conteo.Detalles[0].Contada==null && conteo.Detalles[0].Sistema==33,"Cambios durante el conteo exigen volver a contar");
conteo.Detalles[0].Contada=32;conteo.Detalles[0].Motivo="Diferencia revisada nuevamente";conteo=await control.GuardarConteoAsync(conteo);conteo=await control.ConfirmarConteoAsync(conteo.Id,conteo.Version);
await control.ConfirmarConteoAsync(conteo.Id,conteo.Version);Check(await Stock(101)==32 && conteo.Estado=="Confirmado" && conteo.AutorizadoPor!=null,"Conteo autorizado se aplica una sola vez y conserva responsables");
await Reject<ArgumentException>(()=>control.GuardarConteoAsync(conteo));
var merma=new OperacionExistenciasDto{ProductoId=101,Tipo="Merma",Cantidad=2,Motivo="Daño de empaque",StockEsperado=32};var movimiento=await control.RegistrarAsync(merma);await control.RegistrarAsync(merma);Check(await Stock(101)==30,"Merma idempotente");
var antes=(await control.ReporteAsync(DateTime.Today,DateTime.Today)).CostoMermas;
await control.CorregirAsync(movimiento.Id,"Se capturó dos veces el retiro",Guid.NewGuid());await Reject<ArgumentException>(()=>control.CorregirAsync(movimiento.Id,"Otro intento de corrección",Guid.NewGuid()));
var despues=(await control.ReporteAsync(DateTime.Today,DateTime.Today)).CostoMermas;Check(await Stock(101)==32 && antes-despues==4,"Corrección vinculada conserva original y corrige el costo neto de mermas");
await control.RegistrarAsync(new(){ProductoId=101,Tipo="DevolucionNoVendible",Cantidad=1,Motivo="Regresa dañado",Referencia="V-PRUEBA"});Check(await Stock(101)==32,"Devolución no vendible no incrementa disponibles");
await control.RegistrarAsync(new(){ProductoId=101,Tipo="DevolucionCliente",Cantidad=1,Motivo="Producto cerrado",Referencia="V-PRUEBA"});
await control.RegistrarAsync(new(){ProductoId=101,Tipo="DevolucionProveedor",Cantidad=1,Motivo="Retorno acordado",Referencia="N-001"});Check(await Stock(101)==32,"Devoluciones vendibles y a proveedor usan el sentido correcto");
await Reject<ArgumentException>(()=>control.RegistrarAsync(new(){ProductoId=101,Cantidad=-100,Motivo="Prueba negativa"}));
await using(var db=new PuntoAbaDbContext(options))
{
    var ventas=new VentaService(db,actor);var dto=new CrearVentaDto{Recibido=10,UsuarioId=999,Detalles=new(){new(){ProductoId=101,Cantidad=1,PrecioUnitario=5}}};
    var v=await ventas.CrearAsync(dto);await ventas.CrearAsync(dto);Check(await Stock(101)==31,"Venta confirmada usa el actor real y no duplica salidas al reintentar");
    Check((await db.Ventas.FindAsync(v.Id))!.UsuarioId==1,"La venta no acepta un responsable inventado por el cliente");
    await Reject<ArgumentException>(()=>ventas.CrearAsync(new(){Detalles=new(){new(){ProductoId=101,Cantidad=.5m,PrecioUnitario=5}}}));
}
await using(var one=new PuntoAbaDbContext(options))await using(var two=new PuntoAbaDbContext(options))
{
    var a=await one.Productos.FindAsync(101);var b=await two.Productos.FindAsync(101);a!.Stock--;a.VersionInventario++;await one.SaveChangesAsync();b!.Stock--;b.VersionInventario++;await Reject<DbUpdateConcurrencyException>(()=>two.SaveChangesAsync());Check(await Stock(101)==30,"Una escritura concurrente antigua no sobrescribe existencias");
}
var cfg=new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?>{["Jwt:Key"]="ClaveDePruebaLargaSoloParaLasPruebas123456",["ConnectionStrings:DefaultConnection"]=$"Data Source={path}",["Respaldos:Carpeta"]=Path.Combine(temp,"copias")}).Build();
string Token(int id,string role,DateTime expires,string? key=null)=>new JwtSecurityTokenHandler().WriteToken(new JwtSecurityToken(issuer:"PuntoAba",audience:"PuntoAbaClients",claims:new[]{new Claim(ClaimTypes.NameIdentifier,id.ToString()),new Claim(ClaimTypes.Role,role)},expires:expires,signingCredentials:new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key??cfg["Jwt:Key"]!)),SecurityAlgorithms.HmacSha256)));
var identidad=new IdentidadToken(options,cfg);Check((await identidad.ValidarAsync(Token(2,"Admin",DateTime.UtcNow.AddHours(1)))).Rol=="Cajero","Los permisos se leen del usuario real, no de un rol inventado");
await Reject<UnauthorizedAccessException>(()=>identidad.ValidarAsync(Token(1,"Admin",DateTime.UtcNow.AddHours(-1))));await Reject<UnauthorizedAccessException>(()=>identidad.ValidarAsync(Token(1,"Admin",DateTime.UtcNow.AddHours(1),"OtraClaveNoAutorizadaDePrueba123456789")));
var js=new Almacen(Token(2,"Admin",DateTime.UtcNow.AddHours(1)));var sesion=new SesionUsuarioService(new HttpClient(),js,NullLogger<SesionUsuarioService>.Instance,identidad);await sesion.CargarSesionGuardadaAsync();Check(sesion.UsuarioActual?.Rol=="Cajero" && sesion.UsuarioActual.Id==2,"Una sesión guardada alterada no eleva privilegios");
Directory.CreateDirectory(Path.Combine(temp,"wwwroot","images","productos"));await File.WriteAllTextAsync(Path.Combine(temp,"wwwroot","images","productos","prueba.png"),"imagen de prueba");
var entorno=new Entorno(temp);var motor=new MotorRespaldos(cfg,entorno);var respaldos=new RespaldoService(motor,actor);var nombre=await respaldos.CrearAsync();var restaurado=await respaldos.VerificarAsync(nombre);Check(restaurado.Productos==3 && restaurado.Ventas==1 && restaurado.Imagenes==1,"Respaldo recuperable de base e imágenes con verificación de integridad");
await Reject<ArgumentException>(()=>respaldos.VerificarAsync("../otro.zip"));actor.Rol="Cajero";await Reject<UnauthorizedAccessException>(()=>respaldos.CrearAsync());actor.Rol="Admin";
await ModularChecks.RunAsync(options, new Entorno(temp));
await InicioChecks.RunAsync(options);
await VentaDisenoChecks.RunAsync(temp, actor);
await StockNegativoChecks.RunAsync(temp, actor);
await ReciboVentaChecks.RunAsync(temp, actor);
await AjustesChecks.RunAsync(temp);
await MermasChecks.RunAsync(temp);
await SincronizacionChecks.RunAsync(temp);
Console.WriteLine("Pruebas terminadas. Carpeta temporal: "+temp);

class Actor:IAccesoOperacion{public string Rol{get;set;}="Admin";public int Id{get;set;}=1;public Task<UsuarioDto> ExigirAsync(params string[] roles){if(roles.Length>0&&!roles.Contains(Rol))throw new UnauthorizedAccessException("Sin permiso");return Task.FromResult(new UsuarioDto{Id=Id,Rol=Rol,Activo=true,NombreCompleto="Usuario de prueba"});}}
class Almacen(string token):IJSRuntime{public ValueTask<TValue> InvokeAsync<TValue>(string identifier,object?[]? args)=>InvokeAsync<TValue>(identifier,CancellationToken.None,args);public ValueTask<TValue> InvokeAsync<TValue>(string identifier,CancellationToken cancellationToken,object?[]? args){object? value=args?[0]?.ToString()=="puntoaba.auth.token"?token:"{\"Id\":1,\"Rol\":\"Admin\"}";return ValueTask.FromResult((TValue)value!);}}
class Entorno(string root):IWebHostEnvironment{public string ApplicationName{get;set;}="PuntoAba";public string EnvironmentName{get;set;}="Testing";public string ContentRootPath{get;set;}=root;public IFileProvider ContentRootFileProvider{get;set;}=new NullFileProvider();public string WebRootPath{get;set;}=Path.Combine(root,"wwwroot");public IFileProvider WebRootFileProvider{get;set;}=new NullFileProvider();}
