using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using PuntoAba.DTOs;
using PuntoAba.Services;
using System.Globalization;
namespace PuntoAba.Views.Inventario;
public partial class Resurtido
{
    [Inject] private IProductoService ProductoService { get; set; } = null!;
    [Inject] private IInventarioService InventarioService { get; set; } = null!;
    [Inject] private BorradorEntradaInventario Borrador { get; set; } = null!;
    [Inject] private NavigationManager Navigation { get; set; } = null!;
    [Inject] private ILoggerFactory LoggerFactory { get; set; } = null!;
    [Inject] private IJSRuntime JS { get; set; } = null!;
    private List<ProductoResurtidoDto> productos = new();
    private List<ProductoDto> catalogo = new();
    private readonly HashSet<int> manuales = new();
    private string busquedaManual = "", mensajeManual = "";
    private bool enfocarManual;
    private ElementReference campoManual;
    private List<ProductoDto> CoincidenciasManuales => catalogo.Where(p => p.Nombre.Contains(busquedaManual.Trim(),StringComparison.CurrentCultureIgnoreCase) || p.Codigo.Contains(busquedaManual.Trim(),StringComparison.OrdinalIgnoreCase)).OrderByDescending(p => p.Codigo==busquedaManual.Trim()).ThenBy(p => p.Nombre,Comparador).Take(8).ToList();
    private static bool EsBajo(ProductoResurtidoDto p) => p.Stock>0 && p.Stock<=p.Minimo;
    private static string EstadoProducto(ProductoResurtidoDto p) => p.Stock<=0 ? "Sin stock" : EsBajo(p) ? "Stock bajo" : "Agregado manualmente";
    private readonly Dictionary<int,string> cantidades = new(), erroresCantidad = new();
    private readonly HashSet<int> editados = new();
    private string busqueda="", categoria="", proveedor="", estado="", error="";
    private bool cargando, mostrarLista, enfocarLista;
    private ElementReference panelImpresion;
    private IJSObjectReference? impresion;
    private DateTime fechaLista;
    private static StringComparer Comparador => StringComparer.Create(CultureInfo.GetCultureInfo("es-MX"),true);
    private bool Bloqueado => cargando || mostrarLista;
    private bool TieneFiltros => busqueda.Trim()!="" || categoria!="" || proveedor!="" || estado!="";
    private List<ProductoResurtidoDto> Filtrados => productos.Where(p => (categoria=="" || p.Categoria==categoria)
        && (proveedor=="" || (proveedor=="__sin_proveedor" ? p.Proveedor.Trim()=="" : p.Proveedor.Trim()==proveedor))
        && (busqueda.Trim()=="" || p.Nombre.Contains(busqueda.Trim(),StringComparison.CurrentCultureIgnoreCase) || p.Codigo.Contains(busqueda.Trim(),StringComparison.OrdinalIgnoreCase))
        && (estado=="" || (estado=="agotados" ? p.Stock<=0 : EsBajo(p))))
        .OrderBy(p => p.Stock<=0 ? 0 : EsBajo(p) ? 1 : 2).ThenBy(p => EsBajo(p) && p.Minimo>0 ? p.Stock/p.Minimo : 0).ThenBy(p => p.Nombre,Comparador).ToList();
    private List<ProductoResurtidoDto> Seleccionados => productos.Where(p => p.Seleccionado).OrderBy(p => p.Nombre,Comparador).ToList();
    private int FueraDelFiltro => Seleccionados.Count(p => !Filtrados.Any(f => f.ProductoId==p.ProductoId));
    private bool CantidadesValidas => Seleccionados.All(p => !erroresCantidad.ContainsKey(p.ProductoId) && p.Cantidad>0);
    private decimal Total => Seleccionados.Sum(p => p.Estimado);
    private static string NombreProveedor(ProductoResurtidoDto p) => string.IsNullOrWhiteSpace(p.Proveedor) ? "Sin proveedor asignado" : p.Proveedor.Trim();
    private static string Cantidad(decimal n,ProductoResurtidoDto p) => n.ToString(p.EsPorPeso ? "N3" : "N0");
    protected override async Task OnInitializedAsync() => await Cargar();
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (mostrarLista && enfocarLista) { enfocarLista=false; await panelImpresion.FocusAsync(); }
        else if(enfocarManual && !Bloqueado) { enfocarManual=false; await campoManual.FocusAsync(); }
    }
    private async Task Cargar()
    {
        if (Bloqueado) return; cargando=true;error="";
        try
        {
            var anteriores=productos.ToDictionary(p=>p.ProductoId);
            var nuevos=await InventarioService.ObtenerResurtidoAsync();
            var catalogoNuevo=(await ProductoService.ObtenerTodosAsync()).ToList();
            foreach(var id in manuales.ToList())
            {
                var producto=catalogoNuevo.FirstOrDefault(p=>p.Id==id);
                if(producto==null) { manuales.Remove(id); continue; }
                if(!nuevos.Any(p=>p.ProductoId==id)) nuevos.Add(CrearManual(producto));
            }
            catalogo=catalogoNuevo;
            foreach(var p in nuevos)
            {
                if(anteriores.TryGetValue(p.ProductoId,out var previo)) { p.Seleccionado=previo.Seleccionado; if(editados.Contains(p.ProductoId) || manuales.Contains(p.ProductoId)) p.Cantidad=previo.Cantidad; }
                if(!editados.Contains(p.ProductoId) || !cantidades.ContainsKey(p.ProductoId)) cantidades[p.ProductoId]=p.Cantidad.ToString(CultureInfo.InvariantCulture);
            }
            var ids=nuevos.Select(p=>p.ProductoId).ToHashSet();
            foreach(var id in cantidades.Keys.Where(id=>!ids.Contains(id)).ToList()) { cantidades.Remove(id); erroresCantidad.Remove(id); editados.Remove(id); }
            productos=nuevos;
            if(categoria!="" && !productos.Any(p=>p.Categoria==categoria)) categoria="";
            if(proveedor!="" && proveedor!="__sin_proveedor" && !productos.Any(p=>p.Proveedor.Trim()==proveedor)) proveedor="";
        }
        catch(Exception ex) { LoggerFactory.CreateLogger("Resurtido").LogError(ex,"Error al consultar resurtido"); error="No se pudo actualizar la lista. Inténtalo de nuevo."; }
        finally { cargando=false; }
    }
    private static ProductoResurtidoDto CrearManual(ProductoDto p) => new()
    {
        ProductoId=p.Id,Codigo=p.Codigo,Nombre=p.Nombre,Categoria=p.CategoriaNombre??"",Proveedor=p.ProveedorHabitual??"",
        EsPorPeso=p.EsPorPeso,UnidadMedida=p.UnidadMedida??(p.EsPorPeso ? "" : "Pieza"),Stock=p.Stock,Minimo=p.StockMinimo,
        Objetivo=p.StockObjetivo>0 ? p.StockObjetivo : Math.Max(p.StockMinimo*2,1),Cantidad=1,PrecioCompra=p.PrecioCompra,Seleccionado=true
    };
    private void BuscarManual()
    {
        if(Bloqueado || string.IsNullOrWhiteSpace(busquedaManual)) return;
        var exacto=catalogo.FirstOrDefault(p=>p.Codigo==busquedaManual.Trim());
        if(exacto!=null) { AgregarManual(exacto); return; }
        var opciones=CoincidenciasManuales;
        if(opciones.Count==1) AgregarManual(opciones[0]);
        else mensajeManual=opciones.Count==0 ? "No hay coincidencias en el catálogo activo." : "Selecciona el producto que quieres agregar entre los resultados.";
    }
    private void AgregarManual(ProductoDto producto)
    {
        if(Bloqueado) return;
        var existente=productos.FirstOrDefault(p=>p.ProductoId==producto.Id);
        var limpiar=TieneFiltros;
        if(existente!=null) { existente.Seleccionado=true; mensajeManual=$"{producto.Nombre} ya estaba en la lista; quedó seleccionado."; }
        else
        {
            var nuevo=CrearManual(producto); productos.Add(nuevo); manuales.Add(producto.Id);
            cantidades[producto.Id]="1"; mensajeManual=$"Se agregó {producto.Nombre}. Ajusta la cantidad a pedir en su renglón.";
        }
        LimpiarFiltros(); if(limpiar) mensajeManual+=" Se quitaron los filtros para mostrarlo.";
        busquedaManual=""; error=""; enfocarManual=true;
    }
    private void QuitarManual(ProductoResurtidoDto p)
    {
        if(Bloqueado || !manuales.Contains(p.ProductoId)) return;
        productos.Remove(p);manuales.Remove(p.ProductoId);cantidades.Remove(p.ProductoId);erroresCantidad.Remove(p.ProductoId);editados.Remove(p.ProductoId);
    }
    private void Alternar(ProductoResurtidoDto p) { if(!Bloqueado) p.Seleccionado=!p.Seleccionado; }
    private void SeleccionarVisibles(ChangeEventArgs e) { if(!Bloqueado) foreach(var p in Filtrados) p.Seleccionado=e.Value is true; }
    private void LimpiarFiltros() { busqueda=categoria=proveedor=estado=""; }
    private void CambiarCantidad(ProductoResurtidoDto p,ChangeEventArgs e)
    {
        if(Bloqueado) return;
        var texto=e.Value?.ToString()??""; cantidades[p.ProductoId]=texto; editados.Add(p.ProductoId);
        if(!decimal.TryParse(texto,NumberStyles.AllowDecimalPoint|NumberStyles.AllowLeadingSign,CultureInfo.InvariantCulture,out var n) || n<=0 || n>99999999.999m || decimal.Round(n,3)!=n || (!p.EsPorPeso && decimal.Truncate(n)!=n))
        { p.Cantidad=0; erroresCantidad[p.ProductoId]=p.EsPorPeso ? "Usa una cantidad positiva de hasta tres decimales." : "Usa una cantidad entera mayor a cero."; }
        else { p.Cantidad=n;erroresCantidad.Remove(p.ProductoId);error=""; }
    }
    private bool ValidarSeleccion()
    {
        if(Seleccionados.Count==0 || !CantidadesValidas) { error="Selecciona productos y revisa sus cantidades antes de continuar."; return false; }
        return true;
    }
    private void PrepararEntrada()
    {
        if(Bloqueado || !ValidarSeleccion()) return;
        Borrador.Detalles=Seleccionados.Select(p=>new LineaEntradaMercanciaDto { ProductoId=p.ProductoId,Codigo=p.Codigo,Nombre=p.Nombre,Cantidad=p.Cantidad,CostoUnitario=p.PrecioCompra,EsPorPeso=p.EsPorPeso,UnidadMedida=p.UnidadMedida,StockEsperado=p.Stock }).ToList();
        Navigation.NavigateTo("inventario/entradas");
    }
    private void AbrirLista() { if(Bloqueado || !ValidarSeleccion()) return; fechaLista=DateTime.Now;mostrarLista=enfocarLista=true; }
    private void CerrarLista() => mostrarLista=false;
    private void TeclaLista(KeyboardEventArgs e) { if(e.Key=="Escape") CerrarLista(); }
    private async Task Imprimir()
    {
        try { impresion ??= await JS.InvokeAsync<IJSObjectReference>("import","./js/resurtido-impresion.mjs?v=1"); await impresion.InvokeVoidAsync("imprimir"); }
        catch(JSException) { error="No se pudo abrir la impresión. Cierra la lista e inténtalo de nuevo."; mostrarLista=false; }
    }
    public async ValueTask DisposeAsync() { if(impresion!=null) try { await impresion.DisposeAsync(); } catch(JSDisconnectedException) { } }
}
