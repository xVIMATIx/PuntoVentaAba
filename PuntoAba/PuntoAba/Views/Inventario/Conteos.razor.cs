using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using PuntoAba.DTOs;
using PuntoAba.Services;
using System.Globalization;

namespace PuntoAba.Views.Inventario;
public partial class Conteos
{
    [Inject] private IProductoService Productos { get; set; } = null!;
    [Inject] private IControlInventarioService Control { get; set; } = null!;
    [Inject] private SesionUsuarioService Sesion { get; set; } = null!;
    [Inject] private ILoggerFactory Logs { get; set; } = null!;
    private List<ProductoDto> productos = new();
    private List<ConteoDto> conteos = new();
    private readonly HashSet<int> seleccion = new();
    private readonly Dictionary<int,string> cantidades = new(), erroresCantidad = new();
    private ConteoDto? actual;
    private EditContext? contexto;
    private ElementReference panel, dialogo;
    private bool ocupado, enfocarDetalle, mostrarReinicio, dialogoEnfocado;
    private int productoId;
    private Guid operacion = Guid.NewGuid();
    private string codigo="", categoria="", error="", exito="";
    private bool Bloqueado => ocupado || mostrarReinicio;
    protected override async Task OnInitializedAsync() => await Ejecutar(Cargar);
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if(mostrarReinicio && !dialogoEnfocado) { dialogoEnfocado=true; await dialogo.FocusAsync(); }
        else if(!mostrarReinicio && enfocarDetalle && actual!=null) { enfocarDetalle=false; await panel.FocusAsync(); }
    }
    private RenderFragment Avisos() => builder =>
    {
        if(error!="") { builder.OpenElement(0,"div");builder.AddAttribute(1,"class","alert alert-danger");builder.AddAttribute(2,"role","alert");builder.AddContent(3,error);builder.CloseElement(); }
        if(exito!="") { builder.OpenElement(4,"div");builder.AddAttribute(5,"class","alert alert-success");builder.AddAttribute(6,"role","status");builder.AddContent(7,exito);builder.CloseElement(); }
    };
    private void Ignorar(EditContext c) { }
    private async Task Cargar()
    {
        var nuevosProductos=(await Productos.ObtenerTodosAsync()).OrderBy(p=>p.Nombre).ToList();
        var nuevosConteos=await Control.ObtenerConteosAsync();
        productos=nuevosProductos;conteos=nuevosConteos;
    }
    private Task ActualizarLista() => Ejecutar(async()=>{await Cargar(); exito="Lista actualizada. El conteo abierto conserva tu captura; para consultar su versión guardada, vuelve a abrirlo.";enfocarDetalle=actual!=null;});
    private void AgregarCodigo() { if(Bloqueado)return; var p=productos.FirstOrDefault(p=>p.Codigo==codigo.Trim()); if(p==null){error="No se encontró el código.";return;} seleccion.Add(p.Id);codigo="";error=""; }
    private void AgregarProducto() { if(!Bloqueado && productoId>0) seleccion.Add(productoId); }
    private void AgregarCategoria() { if(!Bloqueado) foreach(var p in productos.Where(p=>categoria=="" || p.CategoriaNombre==categoria)) seleccion.Add(p.Id); }
    private bool PuedeCambiarConteo()
    {
        if(contexto?.IsModified()!=true)return true;
        error="Tienes cambios sin guardar. Guarda el borrador antes de abrir otro conteo o iniciar uno nuevo.";exito="";enfocarDetalle=true;return false;
    }
    private void Mostrar(ConteoDto conteo)
    {
        actual=conteo;contexto=new EditContext(conteo);cantidades.Clear();erroresCantidad.Clear();
        foreach(var d in conteo.Detalles)cantidades[d.ProductoId]=d.Contada?.ToString("0.###",CultureInfo.InvariantCulture)??"";
        enfocarDetalle=true;
    }
    private async Task Abrir(int id)
    {
        if(Bloqueado || !PuedeCambiarConteo())return;
        await Ejecutar(async()=>{var conteo=await Control.ObtenerConteoAsync(id) ?? throw new ArgumentException("No se encontró el conteo. Actualiza la lista.");Mostrar(conteo);exito=conteo.Estado=="Pendiente"?$"{conteo.Folio} abierto. Puedes continuar la captura.":$"{conteo.Folio} abierto en modo de solo consulta.";});
    }
    private async Task Iniciar()
    {
        if(Bloqueado || seleccion.Count==0 || !PuedeCambiarConteo())return;
        await Ejecutar(async()=>{Mostrar(await Control.IniciarConteoAsync(seleccion.ToList(),operacion));operacion=Guid.NewGuid();seleccion.Clear();exito=$"{actual!.Folio} iniciado. Captura las cantidades físicas y guarda tu avance.";await RecargarDespuesDeGuardar();});
    }
    private void CambiarCantidad(LineaConteoDto d,ChangeEventArgs e)
    {
        if(Bloqueado || actual?.Estado!="Pendiente")return;
        var texto=e.Value?.ToString()??"";cantidades[d.ProductoId]=texto;
        if(string.IsNullOrWhiteSpace(texto)) {d.Contada=null;erroresCantidad.Remove(d.ProductoId);}
        else if(decimal.TryParse(texto.Trim().Replace(',','.'),NumberStyles.AllowDecimalPoint|NumberStyles.AllowLeadingSign,CultureInfo.InvariantCulture,out var n) && n>=0 && n<=999999999999999.999m && decimal.Round(n,3)==n && (d.EsPorPeso || decimal.Truncate(n)==n)) {d.Contada=n;erroresCantidad.Remove(d.ProductoId);}
        else {d.Contada=null;erroresCantidad[d.ProductoId]=d.EsPorPeso?"Usa cero o una cantidad positiva con hasta tres decimales.":"Usa cero o un número entero positivo.";}
        contexto?.NotifyFieldChanged(new FieldIdentifier(d,nameof(d.Contada)));exito="";
    }
    private bool Validar(bool completo=false)
    {
        if(Bloqueado || actual?.Estado!="Pendiente")return false;
        if(erroresCantidad.Count>0 || contexto?.Validate()!=true) {error="Revisa las cantidades indicadas antes de guardar.";exito="";enfocarDetalle=true;return false;}
        if(completo && actual.Detalles.Any(d=>!d.Contada.HasValue)) {error="Cuenta todos los productos antes de autorizar. Puedes guardar un borrador incompleto.";exito="";enfocarDetalle=true;return false;}
        if(completo && actual.Detalles.Any(d=>d.Diferencia!=0 && d.Motivo.Trim().Length<3)) {error="Explica cada diferencia con un motivo de al menos tres caracteres antes de autorizar.";exito="";enfocarDetalle=true;return false;}
        return true;
    }
    private async Task Guardar()
    {
        if(!Validar())return;
        await Ejecutar(async()=>{Mostrar(await Control.GuardarConteoAsync(actual!));exito=$"Borrador {actual!.Folio} guardado: {actual.Detalles.Count(d=>d.Contada.HasValue)} de {actual.Detalles.Count} productos contados. Las existencias no cambiaron.";await RecargarDespuesDeGuardar();});
    }
    private async Task Confirmar()
    {
        if(!Validar(true))return;
        await Ejecutar(async()=>{Mostrar(await Control.GuardarConteoAsync(actual!));Mostrar(await Control.ConfirmarConteoAsync(actual!.Id,actual.Version));exito=$"{actual.Folio} confirmado. Diferencias aplicadas con su motivo y autorización.";await RecargarDespuesDeGuardar();});
    }
    private void CancelarReinicio() {if(ocupado)return;mostrarReinicio=false;dialogoEnfocado=false;enfocarDetalle=true;}
    private void TeclaReinicio(KeyboardEventArgs e) {if(e.Key=="Escape")CancelarReinicio();}
    private async Task Reiniciar()
    {
        if(ocupado || !mostrarReinicio || actual?.Estado!="Pendiente")return;
        await Ejecutar(async()=>{Mostrar(await Control.ActualizarBaseConteoAsync(actual.Id,actual.Version));mostrarReinicio=false;dialogoEnfocado=false;exito=$"Base de {actual.Folio} actualizada con las existencias actuales. Cantidades y motivos borrados; vuelve a contar. El inventario no cambió.";await RecargarDespuesDeGuardar();});
        if(error!="") {mostrarReinicio=false;dialogoEnfocado=false;enfocarDetalle=true;}
    }
    private async Task RecargarDespuesDeGuardar()
    {
        try {await Cargar();}catch(Exception ex){Logs.CreateLogger("Conteos").LogError(ex,"El conteo se guardó pero no se pudo refrescar la lista");exito+=" No se pudo actualizar la lista reciente; pulsa Actualizar lista.";}
    }
    private async Task Ejecutar(Func<Task> accion)
    {
        if(ocupado)return;ocupado=true;error=exito="";
        try{await accion();}catch(Exception ex){Logs.CreateLogger("Conteos").LogError(ex,"Error en conteos");error=ex is ArgumentException or UnauthorizedAccessException?ex.Message:"No se pudo completar la operación. Tu captura se conserva; actualiza la lista y revisa si hubo otra operación.";enfocarDetalle=actual!=null;}finally{ocupado=false;}
    }
}
