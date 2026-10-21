using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using PuntoAba.DTOs;
using PuntoAba.Services;
namespace PuntoAba.Views.Inventario;
public partial class Mermas
{
    [Inject] private IProductoService Productos {get;set;}=null!;
    [Inject] private IControlInventarioService Control {get;set;}=null!;
    [Inject] private IInventarioService Inventario {get;set;}=null!;
    [Inject] private ILoggerFactory Logs {get;set;}=null!;
    private static readonly (string Id,string Nombre,string Ayuda,string Icono)[] Opciones = {
        ("merma","Merma","Retirar daño, pérdida o caducidad","caja"),
        ("cliente","Devolución de cliente","Revisar si vuelve al inventario","devolucion"),
        ("proveedor","Devolución a proveedor","Registrar mercancía que sale","transferencia"),
        ("historial","Registros anteriores","Consultar movimientos y detalles","historial")
    };
    private sealed class Borrador
    {
        public Guid OperacionId=Guid.NewGuid();
        public int ProductoId;
        public string Busqueda="",Cantidad="",Motivo="",Notas="",Referencia="",Proveedor="";
        public bool? Vendible;
    }
    private readonly Dictionary<string,Borrador> borradores=new(){["merma"]=new(),["cliente"]=new(),["proveedor"]=new()};
    private List<ProductoDto> catalogo=new();
    private List<MovimientoInventarioDto> registros=new();
    private readonly FiltroMovimientosDto filtro=new(){SoloMermasDevoluciones=true,TamanoPagina=15};
    private MovimientoInventarioDto? detalle;
    private OperacionExistenciasDto? peticion;
    private string vista="merma",error="",exito="";
    private bool ocupado,mostrarRevision,enfocarDialogo;
    private ElementReference panelDialogo;
    private int total;
    private bool Bloqueado=>ocupado || mostrarRevision || detalle!=null;
    private Borrador Captura=>borradores[vista];
    private ProductoDto? Seleccionado=>vista=="historial"?null:catalogo.FirstOrDefault(p=>p.Id==Captura.ProductoId);
    private string Titulo=>Opciones.First(o=>o.Id==vista).Nombre;
    private string NombreCorto=>vista=="merma"?"merma":"devolución";
    private string Tipo=>vista=="merma"?"Merma":vista=="proveedor"?"DevolucionProveedor":Captura.Vendible==true?"DevolucionCliente":"DevolucionNoVendible";
    private string Ayuda=>vista=="merma"?"Retira mercancía dañada, caducada o perdida de las existencias disponibles.":vista=="cliente"?"Registra lo que devuelve el cliente e indica si puede venderse otra vez.":"Registra la mercancía que regresas al proveedor; se restará del inventario.";
    private string[] Motivos=>vista=="merma"?new[]{"Caducidad","Daño de empaque","Pérdida","Otro"}:vista=="proveedor"?new[]{"Producto defectuoso","Caducidad","Producto equivocado","Otro"}:Captura.Vendible==false?new[]{"Producto defectuoso","Daño de empaque","Caducidad","Otro"}:new[]{"Producto equivocado","Compra duplicada","Cambio de producto","Otro"};
    private List<ProductoDto> Coincidencias=>catalogo.Where(p=>p.Codigo.Contains(Captura.Busqueda.Trim(),StringComparison.OrdinalIgnoreCase)||p.Nombre.Contains(Captura.Busqueda.Trim(),StringComparison.CurrentCultureIgnoreCase)).OrderByDescending(p=>p.Codigo==Captura.Busqueda.Trim()).ThenBy(p=>p.Nombre,StringComparer.Create(CultureInfo.GetCultureInfo("es-MX"),true)).Take(8).ToList();
    private decimal Cantidad=>decimal.TryParse(Captura.Cantidad.Trim().Replace(',','.'),NumberStyles.AllowDecimalPoint,CultureInfo.InvariantCulture,out var n)?n:0;
    private bool CantidadValida=>Cantidad>0 && Cantidad<=999999999999999.999m && decimal.Round(Cantidad,3)==Cantidad && (Seleccionado?.EsPorPeso==true || decimal.Truncate(Cantidad)==Cantidad);
    private decimal Resultado=>(Seleccionado?.Stock??0)+(Tipo=="DevolucionCliente"?Cantidad:Tipo=="DevolucionNoVendible"?0:-Cantidad);
    private string MotivoRegistro=>(Captura.Motivo=="Otro"?Captura.Notas.Trim():Captura.Motivo+(string.IsNullOrWhiteSpace(Captura.Notas)?"":". "+Captura.Notas.Trim()));
    private string MotivoCompleto=>vista=="proveedor"?$"Proveedor: {Captura.Proveedor.Trim()}. {MotivoRegistro}":MotivoRegistro;
    private int TotalPaginas=>Math.Max(1,(int)Math.Ceiling(total/(double)filtro.TamanoPagina));
    private static string Unidad(ProductoDto? p)=>p==null?"":!string.IsNullOrWhiteSpace(p.UnidadMedida)?p.UnidadMedida:p.EsPorPeso?"unidad de peso":"Pieza";
    protected override async Task OnInitializedAsync()=>await ActualizarCatalogo();
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {if(enfocarDialogo && (mostrarRevision || detalle!=null)){enfocarDialogo=false;await panelDialogo.FocusAsync();}}
    private async Task CambiarVista(string nueva)
    {if(Bloqueado)return;vista=nueva;error=exito="";if(nueva=="historial")await BuscarHistorial();}
    private async Task ActualizarCatalogo()
    {
        if(Bloqueado)return;ocupado=true;error=exito="";
        try{await CargarCatalogo();}catch(Exception ex){Informar(ex,"No se pudieron consultar los productos. Inténtalo de nuevo.");}finally{ocupado=false;}
    }
    private async Task CargarCatalogo()=>catalogo=(await Productos.ObtenerTodosAsync()).ToList();
    private void ElegirCondicion(bool vendible)
    {if(Bloqueado)return;Captura.Vendible=vendible;if(!Motivos.Contains(Captura.Motivo))Captura.Motivo="";error=exito="";}
    private void BuscarProducto()
    {
        if(Bloqueado || string.IsNullOrWhiteSpace(Captura.Busqueda))return;
        var exacto=catalogo.FirstOrDefault(p=>p.Codigo==Captura.Busqueda.Trim());
        if(exacto!=null){SeleccionarProducto(exacto);return;}
        var opciones=Coincidencias;
        if(opciones.Count==1)SeleccionarProducto(opciones[0]);
        else error=opciones.Count==0?"No hay coincidencias en el catálogo activo.":"Selecciona el producto entre los resultados.";
    }
    private void SeleccionarProducto(ProductoDto p)
    {
        if(Bloqueado)return;Captura.ProductoId=p.Id;Captura.Busqueda="";error=exito="";
        if(vista=="proveedor" && string.IsNullOrWhiteSpace(Captura.Proveedor))Captura.Proveedor=p.ProveedorHabitual??"";
    }
    private void CambiarCantidad(ChangeEventArgs e){if(!Bloqueado){Captura.Cantidad=e.Value?.ToString()??"";error=exito="";}}
    private void Revisar()
    {
        if(Bloqueado)return;error=exito="";
        if(Seleccionado==null){error="Selecciona un producto activo.";return;}
        if(vista=="cliente" && !Captura.Vendible.HasValue){error="Indica si el producto puede venderse otra vez.";return;}
        if(!CantidadValida){error=Seleccionado.EsPorPeso?"Usa una cantidad mayor que cero con hasta tres decimales.":"Usa una cantidad entera mayor que cero.";return;}
        if(!Motivos.Contains(Captura.Motivo) || MotivoRegistro.Trim().Length<3){error="Elige un motivo; si eliges Otro, escribe una explicación de al menos tres caracteres.";return;}
        if(vista!="merma" && string.IsNullOrWhiteSpace(Captura.Referencia)){error="Escribe la referencia de la devolución.";return;}
        if(vista=="proveedor" && string.IsNullOrWhiteSpace(Captura.Proveedor)){error="Escribe el nombre del proveedor.";return;}
        if(MotivoCompleto.Length>400){error="Acorta el nombre del proveedor o la explicación: el motivo completo admite 400 caracteres.";return;}
        if(Resultado<0 || Resultado>999999999999999.999m){error="La operación dejaría existencias fuera del rango permitido. Revisa la cantidad o actualiza las existencias.";return;}
        peticion=new(){OperacionId=Captura.OperacionId,ProductoId=Seleccionado.Id,Tipo=Tipo,Cantidad=Cantidad,Motivo=MotivoCompleto,Referencia=vista=="merma"?"":Captura.Referencia.Trim(),StockEsperado=Seleccionado.Stock};
        mostrarRevision=enfocarDialogo=true;
    }
    private async Task Confirmar()
    {
        if(ocupado || !mostrarRevision || peticion==null)return;ocupado=true;error=exito="";
        try
        {
            var r=await Control.RegistrarAsync(peticion);
            exito=$"Movimiento {r.Id} registrado: {EtiquetasInventario.Tipo(r.Tipo)}. Existencias: {r.StockAnterior:0.###} → {r.StockNuevo:0.###}.";
            if(r.Tipo=="DevolucionNoVendible")exito+=" La mercancía no se sumó al inventario disponible.";
            if(vista=="cliente")exito+=" No se procesó ningún reembolso.";
            borradores[vista]=new(){ProductoId=Captura.ProductoId,Proveedor=Captura.Proveedor};mostrarRevision=false;peticion=null;
            try{await CargarCatalogo();}catch(Exception ex){Logs.CreateLogger("Mermas").LogError(ex,"Movimiento guardado sin refrescar catálogo");catalogo=new();exito+=" Actualiza las existencias antes de registrar otra operación.";}
        }
        catch(Exception ex){mostrarRevision=false;Informar(ex,"No se pudo completar la operación. Tu captura se conserva; revisa los registros antes de reintentar.");}
        finally{ocupado=false;}
    }
    private void CerrarDialogo(){if(!ocupado){mostrarRevision=false;detalle=null;}}
    private void TeclaDialogo(KeyboardEventArgs e){if(e.Key=="Escape")CerrarDialogo();}
    private void AbrirDetalle(MovimientoInventarioDto m){if(!Bloqueado){detalle=m;enfocarDialogo=true;}}
    private async Task BuscarHistorial(){if(Bloqueado)return;filtro.Pagina=1;await CargarHistorial();}
    private async Task CambiarPagina(int delta){if(Bloqueado)return;filtro.Pagina=Math.Clamp(filtro.Pagina+delta,1,TotalPaginas);await CargarHistorial();}
    private async Task CargarHistorial()
    {
        ocupado=true;error=exito="";
        try{var pagina=await Inventario.ObtenerMovimientosAsync(filtro);registros=pagina.Movimientos;total=pagina.Total;}
        catch(Exception ex){registros=new();total=0;Informar(ex,"No se pudieron cargar los registros. Inténtalo de nuevo.");}
        finally{ocupado=false;}
    }
    private void Informar(Exception ex,string mensaje)
    {Logs.CreateLogger("Mermas").LogError(ex,"Error en mermas y devoluciones");error=ex is ArgumentException or UnauthorizedAccessException?ex.Message:mensaje;}
}
