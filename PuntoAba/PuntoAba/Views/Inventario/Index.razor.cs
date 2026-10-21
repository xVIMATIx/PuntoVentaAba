using Microsoft.AspNetCore.Components;
using PuntoAba.DTOs;
using PuntoAba.Services;
using System.Globalization;

namespace PuntoAba.Views.Inventario;

public partial class Index
{
    [Inject] private IProductoService ProductoService { get; set; } = null!;
    [Inject] private NavigationManager Navigation { get; set; } = null!;
    [SupplyParameterFromQuery(Name = "estado")] public string? Estado { get; set; }
    private List<ProductoDto> productos = new(), filtrados = new();
    private string busqueda = "", estadoActual = "", error = "";
    private int categoria, visibles = 50;
    private bool cargando;
    private DateTime? consultado;
    private static readonly (string Valor, string Nombre)[] filtros = { ("", "Todos"), ("disponibles", "Existencia normal"), ("bajo", "Stock bajo"), ("agotados", "Sin stock") };
    private IEnumerable<(int Id, string? Nombre)> Categorias => productos.GroupBy(p => p.CategoriaId).Select(g => (g.Key, g.First().CategoriaNombre)).OrderBy(c => c.Item2, StringComparer.Create(CultureInfo.GetCultureInfo("es-MX"), true));
    protected override async Task OnInitializedAsync() => await Actualizar();
    protected override void OnParametersSet()
    {
        estadoActual = Estado is "bajo" or "agotados" or "disponibles" ? Estado : "";
        ReiniciarLista();
    }
    private async Task Actualizar()
    {
        if (cargando) return;
        cargando = true; error = "";
        try
        {
            productos = (await ProductoService.ObtenerTodosAsync()).ToList();
            consultado = DateTime.Now;
            if (!productos.Any(p => p.CategoriaId == categoria)) categoria = 0;
            ReiniciarLista();
        }
        catch (Exception) { error = "No se pudo actualizar el inventario. Intenta de nuevo con Actualizar."; }
        finally { cargando = false; }
    }
    private void ReiniciarLista()
    {
        var texto = busqueda.Trim();
        filtrados = productos.Where(p => (categoria == 0 || p.CategoriaId == categoria)
            && (texto.Length == 0 || p.Nombre.Contains(texto, StringComparison.CurrentCultureIgnoreCase) || p.Codigo.Contains(texto, StringComparison.OrdinalIgnoreCase))
            && (estadoActual switch { "bajo" => p.Stock > 0 && p.Stock <= p.StockMinimo, "agotados" => p.Stock <= 0, "disponibles" => p.Stock > 0 && p.Stock > p.StockMinimo, _ => true }))
            .OrderBy(p => p.Nombre, StringComparer.Create(CultureInfo.GetCultureInfo("es-MX"), true)).ThenBy(p => p.Codigo).ToList();
        visibles = 50;
    }
    private void CambiarEstado(string estado) => Navigation.NavigateTo("inventario" + (estado.Length > 0 ? "?estado=" + estado : ""));
    private void LimpiarBusqueda() { busqueda = ""; categoria = 0; ReiniciarLista(); }
    private void MostrarMas() => visibles = Math.Min(visibles + 50, filtrados.Count);
    private static string Cantidad(decimal cantidad, ProductoDto p) => cantidad.ToString(p.EsPorPeso ? "N3" : "N0");
    private static string Unidad(ProductoDto p) => string.IsNullOrWhiteSpace(p.UnidadMedida) ? (p.EsPorPeso ? "" : "pza") : p.UnidadMedida;
}
