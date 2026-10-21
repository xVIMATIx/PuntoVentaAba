using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using PuntoAba.DTOs;
using PuntoAba.Services;
namespace PuntoAba.Shared;
public partial class OperacionExistencias
{
    [Inject] private IProductoService Productos { get; set; } = null!;
    [Inject] private IControlInventarioService Control { get; set; } = null!;
    [Inject] private ILoggerFactory Logs { get; set; } = null!;
    [Parameter] public bool EsAjuste { get; set; }
    [Parameter] public int? ProductoInicial { get; set; }
    private List<ProductoDto> productos = new();
    private ProductoDto? seleccionado;
    private OperacionExistenciasDto operacion = new();
    private string codigo="", cantidadTexto="", errorCantidad="", error="", exito="";
    private bool ocupado, mostrarRevision, enfocarRevision, restar, parametrosRecibidos;
    private int? ultimoProductoInicial;
    private ElementReference panelRevision;
    private string Unidad => seleccionado==null ? "" : !string.IsNullOrWhiteSpace(seleccionado.UnidadMedida) ? seleccionado.UnidadMedida : seleccionado.EsPorPeso ? "unidad de peso" : "Pieza";
    private bool Bloqueado => ocupado || mostrarRevision;
    private decimal Resultado => (seleccionado?.Stock ?? 0) + (operacion.Tipo is "Ajuste" or "DevolucionCliente" ? operacion.Cantidad : operacion.Tipo=="DevolucionNoVendible" ? 0 : -operacion.Cantidad);
    private bool CantidadCorrecta => decimal.TryParse(cantidadTexto.Trim().Replace(',','.'),NumberStyles.AllowDecimalPoint,CultureInfo.InvariantCulture,out var n) && n>0 && n<=999999999999999.999m && decimal.Round(n,3)==n && (seleccionado?.EsPorPeso==true || decimal.Truncate(n)==n);
    protected override async Task OnParametersSetAsync()
    {
        if(parametrosRecibidos && ultimoProductoInicial==ProductoInicial)return;
        parametrosRecibidos=true;ultimoProductoInicial=ProductoInicial;
        operacion=new(){Tipo=EsAjuste?"Ajuste":"Merma",ProductoId=ProductoInicial??0};cantidadTexto="";errorCantidad=error=exito="";restar=false;mostrarRevision=false;
        await Actualizar();
    }
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if(mostrarRevision && enfocarRevision) {enfocarRevision=false;await panelRevision.FocusAsync();}
    }
    private async Task Cargar()
    {
        var nuevos=(await Productos.ObtenerTodosAsync()).OrderBy(p=>p.Nombre).ToList();productos=nuevos;Seleccionar();
    }
    private async Task Actualizar()
    {
        if(Bloqueado)return;ocupado=true;error=exito="";
        try {await Cargar();exito="Existencias consultadas. Revisa el resultado antes de confirmar.";}catch(Exception ex){Informar(ex);}finally{ocupado=false;}
    }
    private void Seleccionar()
    {
        seleccionado=productos.FirstOrDefault(p=>p.Id==operacion.ProductoId);operacion.StockEsperado=seleccionado?.Stock;
        if(cantidadTexto!="")ValidarCantidad();
    }
    private void BuscarCodigo()
    {
        if(Bloqueado)return;
        var p=productos.FirstOrDefault(p=>p.Codigo==codigo.Trim());
        if(p==null){error="No se encontró un producto activo con ese código.";return;}
        operacion.ProductoId=p.Id;Seleccionar();codigo="";error=exito="";
    }
    private void CambiarSentido(bool quitar)
    {
        if(Bloqueado)return;restar=quitar;if(cantidadTexto!="")ValidarCantidad();exito="";
    }
    private void CambiarCantidad(ChangeEventArgs e)
    {
        if(Bloqueado)return;cantidadTexto=e.Value?.ToString()??"";ValidarCantidad();exito="";
    }
    private bool ValidarCantidad()
    {
        if(!CantidadCorrecta){operacion.Cantidad=0;errorCantidad=seleccionado?.EsPorPeso==true?"Usa una cantidad mayor que cero con hasta tres decimales.":"Usa una cantidad entera mayor que cero.";return false;}
        var n=decimal.Parse(cantidadTexto.Trim().Replace(',','.'),NumberStyles.AllowDecimalPoint,CultureInfo.InvariantCulture);
        operacion.Cantidad=EsAjuste && restar ? -n : n;errorCantidad="";return true;
    }
    private void Revisar()
    {
        if(Bloqueado)return;error=exito="";
        if(seleccionado==null){error="Selecciona un producto activo.";return;}
        if(!ValidarCantidad()){error="Revisa la cantidad antes de continuar.";return;}
        if(operacion.Motivo.Trim().Length<3 || operacion.Motivo.Trim().Length>400){error="Escribe un motivo de entre 3 y 400 caracteres.";return;}
        if(operacion.Tipo.StartsWith("Devolucion") && string.IsNullOrWhiteSpace(operacion.Referencia)){error="Indica la referencia de la devolución.";return;}
        if(Resultado<0 || Resultado>999999999999999.999m){error="La operación dejaría existencias fuera del rango permitido. Revisa la cantidad o actualiza las existencias.";return;}
        mostrarRevision=enfocarRevision=true;
    }
    private void CerrarRevision(){if(!ocupado)mostrarRevision=false;}
    private void TeclaRevision(KeyboardEventArgs e){if(e.Key=="Escape")CerrarRevision();}
    private async Task Confirmar()
    {
        if(ocupado || !mostrarRevision)return;ocupado=true;error=exito="";
        try
        {
            var r=await Control.RegistrarAsync(operacion);
            exito=$"Movimiento {r.Id} registrado. Existencias: {r.StockAnterior:0.###} → {r.StockNuevo:0.###}. Motivo: {r.Motivo}";
            operacion=new(){ProductoId=operacion.ProductoId,Tipo=operacion.Tipo};cantidadTexto=errorCantidad="";mostrarRevision=false;
            try {await Cargar();}catch(Exception ex){Logs.CreateLogger("OperacionInventario").LogError(ex,"Movimiento guardado sin poder refrescar el catálogo");exito+=" No se pudo refrescar el catálogo; pulsa Actualizar existencias antes de otra operación.";seleccionado=null;}
        }
        catch(Exception ex){mostrarRevision=false;Informar(ex);}
        finally{ocupado=false;}
    }
    private void Informar(Exception ex)
    {
        Logs.CreateLogger("OperacionInventario").LogError(ex,"No se pudo registrar la operación");
        error=ex is ArgumentException or UnauthorizedAccessException?ex.Message:"No se pudo completar la operación. Tu captura se conserva; revisa los movimientos antes de reintentar.";
    }
}
