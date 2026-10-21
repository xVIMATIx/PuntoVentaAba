using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using PuntoAba.DTOs;
using PuntoAba.Services;
namespace PuntoAba.Views.Inventario;
public partial class Entradas
{
    [Inject] private IInventarioService InventarioService { get; set; } = null!;
    [Inject] private IProductoService ProductoService { get; set; } = null!;
    [Inject] private BorradorEntradaInventario Borrador { get; set; } = null!;
    [Inject] private SesionUsuarioService Sesion { get; set; } = null!;
    [Inject] private ILoggerFactory LoggerFactory { get; set; } = null!;
    private CrearEntradaMercanciaDto entrada = new();
    private EditForm? formulario;
    private List<ProductoDto> productos = new();
    private List<EntradaMercanciaDto> entradas = new();
    private string busqueda = "", error = "", exito = "";
    private int? detalleId;
    private bool guardando, cargando = true, mostrarRevision, mostrarDatos, verHistorial, consultando, enfocarBusqueda = true, enfocarRevision;
    private ElementReference campoBusqueda, panelRevision;
    private bool Bloqueado => guardando || mostrarRevision || consultando;
    private decimal Total => entrada.Detalles.Sum(d => d.Importe);
    private List<ProductoDto> Coincidencias => productos.Where(p => p.Codigo.Contains(busqueda.Trim(), StringComparison.OrdinalIgnoreCase) || p.Nombre.Contains(busqueda.Trim(), StringComparison.CurrentCultureIgnoreCase)).OrderByDescending(p => p.Codigo == busqueda.Trim()).Take(8).ToList();
    private static string Numero(decimal? n) => n?.ToString("0.###") ?? "Sin consultar";
    protected override async Task OnInitializedAsync()
    {
        try { await CargarListas(); entrada.Detalles = Borrador.Detalles.ToList(); Borrador.Detalles.Clear(); }
        catch (Exception ex) { InformarError(ex); }
        finally { cargando = false; }
    }
    private async Task CargarListas() { productos = (await ProductoService.ObtenerTodosAsync()).OrderBy(p => p.Nombre).ToList(); entradas = await InventarioService.ObtenerEntradasAsync(); }
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (enfocarRevision && mostrarRevision) { enfocarRevision = false; await panelRevision.FocusAsync(); }
        else if (enfocarBusqueda && !cargando && !Bloqueado && !verHistorial) { enfocarBusqueda = false; await campoBusqueda.FocusAsync(); }
    }
    private void IgnorarEnvio(EditContext contexto) { }
    private void CambiarVista(bool historial) { if (Bloqueado) return; verHistorial = historial; if (!historial) enfocarBusqueda = true; }
    private void EnviarBusqueda()
    {
        if (Bloqueado || string.IsNullOrWhiteSpace(busqueda)) return;
        var exacto = productos.FirstOrDefault(p => p.Codigo == busqueda.Trim());
        if (exacto != null) { Agregar(exacto); return; }
        var encontrados = Coincidencias;
        if (encontrados.Count == 1) Agregar(encontrados[0]);
        else error = encontrados.Count == 0 ? "No se encontró el producto. Regístralo primero en Productos." : "Selecciona el producto que recibiste entre los resultados de búsqueda.";
    }
    private void Agregar(ProductoDto p)
    {
        if (Bloqueado) return;
        error = exito = "";
        var existente = entrada.Detalles.FirstOrDefault(d => d.ProductoId == p.Id);
        if (existente != null) existente.Cantidad += 1;
        else entrada.Detalles.Add(new() { ProductoId=p.Id, Codigo=p.Codigo, Nombre=p.Nombre, EsPorPeso=p.EsPorPeso, UnidadMedida=p.UnidadMedida ?? "Pieza", Cantidad=1, CostoUnitario=p.PrecioCompra, StockEsperado=p.Stock });
        busqueda = ""; enfocarBusqueda = true;
    }
    private bool Validar()
    {
        if (formulario?.EditContext?.Validate() != true) { mostrarDatos = true; return false; }
        if (entrada.Detalles.Count == 0) { error = "Agrega al menos un producto."; return false; }
        foreach (var l in entrada.Detalles)
        {
            if (l.Cantidad <= 0 || l.Cantidad > 99999999.999m || decimal.Round(l.Cantidad,3) != l.Cantidad || (!l.EsPorPeso && decimal.Truncate(l.Cantidad) != l.Cantidad)) { error = $"Revisa la cantidad de {l.Nombre}: usa piezas/cajas enteras o hasta tres decimales para peso."; return false; }
            if (l.PorCaja && (l.EsPorPeso || l.PiezasPorCaja <= 0 || l.PiezasPorCaja > 100000)) { error = $"Revisa las piezas por caja de {l.Nombre}."; return false; }
            if (l.CostoUnitario < 0 || l.CostoUnitario > 99999999.99m || decimal.Round(l.CostoUnitario,2) != l.CostoUnitario) { error = $"Revisa el costo de {l.Nombre}: admite hasta dos decimales y no puede ser negativo."; return false; }
        }
        return true;
    }
    private void Revisar() { if (Bloqueado) return; error = ""; if (!Validar()) return; mostrarRevision = enfocarRevision = true; }
    private void CerrarRevision() { if (!guardando) mostrarRevision = false; }
    private void TeclaRevision(KeyboardEventArgs e) { if (e.Key == "Escape") CerrarRevision(); }
    private async Task Guardar()
    {
        if (!mostrarRevision || guardando || !Validar()) return;
        guardando = true; error = exito = "";
        try
        {
            var resultado = await InventarioService.RegistrarEntradaAsync(entrada, Sesion.UsuarioActual?.Id);
            entrada = new(); mostrarRevision = mostrarDatos = false; busqueda = ""; enfocarBusqueda = true;
            exito = $"Entrada {resultado.Folio} registrada. Las existencias ya se actualizaron.";
            try { await CargarListas(); } catch (Exception ex) { LoggerFactory.CreateLogger("EntradasMercancia").LogError(ex,"Recepción registrada; falló la recarga"); error = "La recepción se guardó, pero no se pudo recargar la información. Vuelve a abrir Entradas para consultarla."; }
        }
        catch (Exception ex) { InformarError(ex); }
        finally { guardando = false; }
    }
    private async Task ActualizarExistencias()
    {
        if (Bloqueado) return; consultando = true;
        try { productos=(await ProductoService.ObtenerTodosAsync()).OrderBy(p=>p.Nombre).ToList(); foreach(var d in entrada.Detalles) d.StockEsperado=productos.FirstOrDefault(p=>p.Id==d.ProductoId)?.Stock; error=""; }
        catch(Exception ex) { InformarError(ex); }
        finally { consultando = false; }
    }
    private void InformarError(Exception ex)
    {
        LoggerFactory.CreateLogger("EntradasMercancia").LogError(ex,"Error en entradas de mercancía");
        error = ex is ArgumentException or System.ComponentModel.DataAnnotations.ValidationException or UnauthorizedAccessException ? ex.Message.Replace("Pulsa Actualizar existencias", "Pulsa Volver a consultar existencias") : "No se pudo completar la operación. Inténtalo de nuevo; el mismo intento no duplicará la entrada.";
    }
}
