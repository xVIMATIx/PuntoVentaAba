using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using PuntoAba.DTOs;
using PuntoAba.Presentation.Ventas;
using PuntoAba.Services;
using System.Globalization;
using System.Text.Json;

namespace PuntoAba.Views.Ventas;

public partial class Nuevo : IAsyncDisposable
{
    [Inject] private IProductoService ProductoService { get; set; } = null!;
    [Inject] private IVentaService VentaService { get; set; } = null!;
    [Inject] private IClienteService ClienteService { get; set; } = null!;
    [Inject] private ICatalogoVentaService CatalogoService { get; set; } = null!;
    [Inject] private IBalanzaService BalanzaService { get; set; } = null!;
    [Inject] private IJSRuntime JS { get; set; } = null!;
    [Inject] private SesionUsuarioService Sesion { get; set; } = null!;

    [Inject] private VentasEnCurso Pedidos { get; set; } = null!;
    private BorradorVenta ventaActual => Pedidos.Actual;
    private string ultimoGuardado = "";
    private bool datosCargados, avisoGuardado;
    private int? enfocarPestana;
    private (int Indice, int VentaId)? confirmacionPendiente;
    private ElementReference raiz;
    private IJSObjectReference? modulo;
    private DotNetObjectReference<Nuevo>? referencia;
    private readonly CancellationTokenSource detener = new();
    private Task? reloj;
    private bool eliminado;
    private List<ProductoDto> productos = new(), productosFiltrados = new();
    private List<(int Id, string Nombre)> categorias = new();
    private Dictionary<int, int> frecuencia = new();
    private List<int> favoritos = new();
    private CarritoVenta estadoCarrito => ventaActual.Carrito;
    private IReadOnlyList<ItemCarrito> carrito => estadoCarrito.Items;
    private readonly PesoVenta controlPeso = new();
    private List<ClienteDto> clientes = new();
    private string filtro = "", mensajeEscaner = "", errorPeso = "";
    private string errorVenta { get => ventaActual.Error; set => ventaActual.Error = value; }
    private bool errorEscaner, cargando = true, procesando, reiniciarScroll;
    private int categoriaActual, visibles = 24;
    private Guid operacionVenta => ventaActual.OperacionId;
    private decimal descuento => ventaActual.DescuentoAplicado;
    private decimal porcentajeImpuesto => ventaActual.PorcentajeImpuesto;
    private decimal? recibido => ventaActual.Recibido;
    private bool aplicarImpuesto { get => ventaActual.AplicarImpuesto; set => ventaActual.AplicarImpuesto = value; }
    private string metodoPago { get => ventaActual.MetodoPago; set => ventaActual.MetodoPago = value; }
    private int clienteSeleccionado { get => ventaActual.ClienteId; set => ventaActual.ClienteId = value; }
    private static readonly (string Nombre, string Icono)[] metodos = { ("Efectivo", "efectivo"), ("Tarjeta", "tarjeta"), ("Transferencia", "transferencia"), ("Crédito", "credito") };
    private bool mostrarModalClientes, mostrarModalPeso, mostrarModalCobro, mostrarCancelar, mostrarFavoritos, editandoPeso;
    private string modoCapturaPeso = "manual", pesoCaptura = "", buscarCliente = "";
    private ProductoDto? productoPesoSeleccionado;
    private bool enfocarPeso;
    private bool PuedeEscanear => Pedidos.Restaurado && !cargando && !procesando && !mostrarModalPeso && !mostrarModalCobro && !mostrarModalClientes && !mostrarCancelar && !mostrarFavoritos;
    private bool UnidadCompatible(ProductoDto? producto) => producto != null && string.Equals(producto.UnidadMedida?.Trim(), BalanzaService.UnidadMedida.Trim(), StringComparison.OrdinalIgnoreCase);
    private decimal? PesoEstable => controlPeso.Obtener(BalanzaService.Conectada, DateTime.Now);
    private List<ClienteDto> clientesFiltradosBusqueda => clientes.Where(c => string.IsNullOrWhiteSpace(buscarCliente) || c.NombreCompleto.Contains(buscarCliente, StringComparison.CurrentCultureIgnoreCase) || (c.DocumentoIdentidad?.Contains(buscarCliente, StringComparison.OrdinalIgnoreCase) ?? false)).ToList();

    protected override async Task OnInitializedAsync()
    {
        Pedidos.PrepararUsuario(Sesion.UsuarioActual?.Id ?? 0);
        BalanzaService.EstadoChanged += ManejarCambioBalanza;
        RegistrarPeso();
        await CargarDatos();
    }
    private async Task CargarDatos()
    {
        cargando = true;
        try
        {
            productos = (await ProductoService.ObtenerTodosAsync()).ToList();
            categorias = productos.GroupBy(p => p.CategoriaId).Select(g => (g.Key, g.First().CategoriaNombre ?? "Sin categoría"))
                .OrderBy(c => c.Item2, StringComparer.Create(CultureInfo.GetCultureInfo("es-MX"), true)).ToList();
            frecuencia = await CatalogoService.FrecuenciaUltimos30DiasAsync();
            clientes = await ClienteService.ObtenerTodosAsync();
            Pedidos.ActualizarExistencias(productos); datosCargados = true;
            Filtrar();
        }
        catch (Exception) { errorVenta = "No se pudieron cargar los datos de venta. Recarga la pantalla para volver a intentarlo."; }
        finally { cargando = false; }
    }
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            modulo = await JS.InvokeAsync<IJSObjectReference>("import", "./js/venta-ui.mjs?v=enter-1");
            referencia = DotNetObjectReference.Create(this);
            favoritos = (await modulo.InvokeAsync<int[]>("conectar", raiz, referencia, Sesion.UsuarioActual?.Id ?? 0)).Where(id => productos.Any(p => p.Id == id)).Distinct().ToList();
            Filtrar();
            reloj = RefrescarLectura();
            StateHasChanged();
        }
        if (modulo != null)
        {
            await modulo.InvokeVoidAsync("actualizar", raiz, PuedeEscanear, reiniciarScroll);
            reiniciarScroll = false;
            if (enfocarPeso) { enfocarPeso = false; await modulo.InvokeVoidAsync("capturarCantidad", raiz); }
            if (enfocarPestana.HasValue) { var indice = enfocarPestana.Value; enfocarPestana = null; await modulo.InvokeVoidAsync("enfocarPestana", raiz, indice); }
            if (confirmacionPendiente.HasValue)
            {
                var aviso = confirmacionPendiente.Value; confirmacionPendiente = null;
                try { await modulo.InvokeVoidAsync("confirmarVenta", raiz, aviso.Indice, aviso.VentaId); }
                catch (JSException) { /* El aviso es opcional: la venta ya quedó confirmada. */ }
            }
            if (datosCargados) await GuardarPedidos();
        }
    }
    private async Task GuardarPedidos()
    {
        if (!Pedidos.Restaurado)
        {
            var json = await modulo!.InvokeAsync<string>("recuperarPedidos", raiz);
            if (!string.IsNullOrEmpty(json))
            {
                try
                {
                    var estado = JsonSerializer.Deserialize<PedidosGuardados>(json);
                    if (estado != null && Pedidos.Restaurar(estado, productos, clientes.Select(c => c.Id))) Avisar("Se recuperaron los pedidos. Revisa los productos que ya no están disponibles.", true);
                }
                catch (JsonException) { Avisar("No se pudo recuperar el guardado anterior de esta pestaña.", true); }
            }
            Pedidos.Restaurado = true; StateHasChanged();
        }
        var actual = JsonSerializer.Serialize(Pedidos.Exportar());
        if (actual == ultimoGuardado) return;
        var guardado = await modulo!.InvokeAsync<bool>("guardarPedidos", raiz, actual);
        ultimoGuardado = actual;
        if (!guardado && !avisoGuardado)
        {
            avisoGuardado = true; Avisar("El navegador no permitió guardar los pedidos. Se conservan mientras esta pantalla siga abierta.", true); StateHasChanged();
        }
    }
    [JSInvokable] public async Task SeleccionarVenta(int indice)
    {
        if (!PuedeEscanear || indice == Pedidos.Indice || indice < 0 || indice >= 5) return;
        if (modulo != null) await modulo.InvokeVoidAsync("cambiarPedido", raiz);
        Pedidos.Seleccionar(indice); mensajeEscaner = ""; errorEscaner = false; enfocarPestana = indice;
        StateHasChanged();
    }
    private async Task RefrescarLectura()
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(1));
        try { while (await timer.WaitForNextTickAsync(detener.Token)) await InvokeAsync(StateHasChanged); }
        catch (OperationCanceledException) { }
    }
    private void RegistrarPeso() => controlPeso.Registrar(BalanzaService.PesoActual, BalanzaService.UltimaLectura, BalanzaService.Conectada, BalanzaService.LecturaCruda);
    private void ManejarCambioBalanza()
    {
        if (eliminado) return;
        _ = InvokeAsync(() =>
        {
            if (eliminado) return;
            RegistrarPeso();
            if (mostrarModalPeso && modoCapturaPeso == "balanza") pesoCaptura = BalanzaService.PesoActual?.ToString("0.000", CultureInfo.InvariantCulture) ?? "";
            StateHasChanged();
        });
    }
    private void Buscar(ChangeEventArgs e) { filtro = e.Value?.ToString() ?? ""; Filtrar(); }
    private void Filtrar()
    {
        productosFiltrados = OrdenCatalogo.Aplicar(productos, filtro, categoriaActual, frecuencia, favoritos);
        visibles = 24; reiniciarScroll = true;
    }
    private void Categoria(int id) { categoriaActual = id; Filtrar(); }
    private void LimpiarBusqueda() { filtro = ""; Filtrar(); }
    [JSInvokable] public Task CargarMas() { visibles = Math.Min(visibles + 24, productosFiltrados.Count); return InvokeAsync(StateHasChanged); }
    private void EnviarBusqueda()
    {
        var codigo = filtro.Trim();
        if (codigo.Length == 0) { AbrirCobro(); return; }
        EscanearCodigo(codigo);
    }
    [JSInvokable] public Task LeerGlobal(string codigo) => InvokeAsync(() => { if (!eliminado) { EscanearCodigo(codigo); StateHasChanged(); } });
    private void EscanearCodigo(string codigo)
    {
        if (!PuedeEscanear) return;
        var producto = productos.FirstOrDefault(p => p.Activo && string.Equals(p.Codigo, codigo.Trim(), StringComparison.Ordinal));
        if (producto == null) { Avisar("No se encontró un producto con ese código. Puedes buscarlo por nombre.", true); return; }
        // Una lectura global conserva la posición del catálogo; solo se limpia una búsqueda activa.
        if (filtro.Length > 0) { filtro = ""; Filtrar(); }
        if (producto.EsPorPeso)
        {
            var peso = UnidadCompatible(producto) ? PesoEstable : null;
            if (!peso.HasValue) { AbrirPeso(producto, false); Avisar("Revisa la cantidad: la báscula necesita una lectura válida y estable.", true); return; }
            AgregarCantidad(producto, peso.Value);
        }
        else AgregarCantidad(producto, 1);
    }
    private void Avisar(string mensaje, bool error = false) { mensajeEscaner = mensaje; errorEscaner = error; }
    private void AgregarCantidad(ProductoDto p, decimal n)
    {
        if (!estadoCarrito.Agregar(p, n)) { Avisar($"Revisa la cantidad de {p.Nombre}: piezas enteras o hasta tres decimales por peso.", true); return; }
        Avisar($"Agregado: {p.Nombre} · {n.ToString(p.EsPorPeso ? "N3" : "N0")} {(p.EsPorPeso ? p.UnidadMedida : "pza")}");
        errorVenta = "";
    }
    private void AgregarAlCarrito(ProductoDto p) { if (!PuedeEscanear) return; if (p.EsPorPeso) AbrirPeso(p, false); else AgregarCantidad(p, 1); }
    private void EditarPeso(ItemCarrito item) => AbrirPeso(item.Producto, true);
    private void AbrirPeso(ProductoDto p, bool editar)
    {
        productoPesoSeleccionado = p; editandoPeso = editar; mostrarModalPeso = true; errorPeso = "";
        modoCapturaPeso = BalanzaService.Conectada && UnidadCompatible(p) ? "balanza" : "manual";
        pesoCaptura = editar && modoCapturaPeso == "manual" ? (carrito.FirstOrDefault(i => i.Producto.Id == p.Id)?.Cantidad ?? 0).ToString("0.000", CultureInfo.InvariantCulture) : BalanzaService.PesoActual?.ToString("0.000", CultureInfo.InvariantCulture) ?? "";
        enfocarPeso = modoCapturaPeso == "manual";
    }
    private void CambiarModoCapturaPeso(string modo)
    {
        if (modo == "balanza" && !UnidadCompatible(productoPesoSeleccionado)) return;
        modoCapturaPeso = modo; errorPeso = "";
        if (modo == "balanza") pesoCaptura = BalanzaService.PesoActual?.ToString("0.000", CultureInfo.InvariantCulture) ?? "";
        else enfocarPeso = true;
    }
    private decimal? CantidadCapturada => decimal.TryParse(pesoCaptura.Replace(',', '.'), NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var n) ? n : null;
    private bool CantidadPesoValida
    {
        get
        {
            var n = modoCapturaPeso == "balanza" ? PesoEstable : CantidadCapturada;
            if (productoPesoSeleccionado == null || n is null or <= 0 || decimal.Round(n.Value, 3) != n) return false;
            return n <= 999999999999999.999m;
        }
    }
    private string EquivalenciaPeso => productoPesoSeleccionado?.UnidadMedida?.Equals("kg", StringComparison.OrdinalIgnoreCase) == true && CantidadCapturada.HasValue ? $"{CantidadCapturada.Value * 1000:N0} gramos" : "Hasta tres decimales";
    private decimal CalcularSubtotalPeso() => (productoPesoSeleccionado?.PrecioVenta ?? 0) * (CantidadCapturada ?? 0);
    private async Task ConfirmarPeso()
    {
        if (!CantidadPesoValida || productoPesoSeleccionado == null) { errorPeso = "Revisa el peso y la conexión de la báscula."; return; }
        var n = modoCapturaPeso == "balanza" ? PesoEstable!.Value : CantidadCapturada!.Value;
        var p = productoPesoSeleccionado;
        var ok = editandoPeso ? estadoCarrito.CambiarCantidad(p.Id, n) : estadoCarrito.Agregar(p, n);
        if (!ok) { errorPeso = "Ingresa una cantidad positiva con hasta tres decimales."; return; }
        await CerrarModalPeso(); Avisar($"Cantidad registrada: {p.Nombre} · {n:N3} {p.UnidadMedida}");
    }
    private async Task CerrarModalPeso()
    {
        mostrarModalPeso = false; productoPesoSeleccionado = null; errorPeso = "";
        if (modulo != null) await modulo.InvokeVoidAsync("cerrarCantidad", raiz);
    }
    private void AumentarCantidad(ItemCarrito i) => CambiarCantidad(new(i.Producto.Id, i.Cantidad + 1));
    private void DisminuirCantidad(ItemCarrito i) => CambiarCantidad(new(i.Producto.Id, i.Cantidad - 1));
    private void CambiarCantidad(CambioCantidadCarrito c)
    {
        if (!PuedeEscanear) return;
        if (!estadoCarrito.CambiarCantidad(c.ProductoId, c.Cantidad)) Avisar("Cantidad inválida: usa valores positivos, piezas enteras o hasta tres decimales por peso.", true);
    }
    private void EliminarDelCarrito(ItemCarrito i) => estadoCarrito.Eliminar(i.Producto.Id);
    private async Task AlternarFavorito(int id)
    {
        if (!favoritos.Remove(id)) favoritos.Add(id);
        await GuardarFavoritos(); if (categoriaActual == -1) Filtrar();
    }
    private async Task MoverFavorito(int id, int direccion)
    {
        var i = favoritos.IndexOf(id); var siguiente = i + direccion;
        if (i < 0 || siguiente < 0 || siguiente >= favoritos.Count) return;
        (favoritos[i], favoritos[siguiente]) = (favoritos[siguiente], favoritos[i]);
        await GuardarFavoritos(); Filtrar();
    }
    private async Task GuardarFavoritos() { if (modulo != null) await modulo.InvokeVoidAsync("guardarFavoritos", raiz, favoritos); }
    private void AbrirModalClientes() { buscarCliente = ""; mostrarModalClientes = true; }
    private void ElegirCliente(int id) { clienteSeleccionado = id; mostrarModalClientes = false; }
    [JSInvokable] public async Task CerrarVentanas()
    {
        if (procesando) return;
        mostrarModalClientes = mostrarCancelar = mostrarFavoritos = mostrarModalCobro = false;
        if (mostrarModalPeso) await CerrarModalPeso();
        StateHasChanged();
    }
    private void SolicitarCancelar() { if (carrito.Count > 0) mostrarCancelar = true; else ReiniciarVenta(); }
    private void CancelarVenta() { ReiniciarVenta(); Avisar("Venta cancelada."); }
    private void ReiniciarVenta()
    {
        ventaActual.Reiniciar();
        mostrarCancelar = mostrarModalCobro = false; errorVenta = "";
    }
    private void MostrarEstadoBalanza() => Avisar(BalanzaService.Conectada ? (PesoEstable.HasValue ? "Báscula conectada. Peso estable." : "Báscula conectada. Esperando una lectura estable y reciente.") : "Báscula desconectada. Puedes capturar las cantidades en modo Manual.");
    private decimal subtotal => estadoCarrito.Subtotal;
    private decimal iva => aplicarImpuesto ? Math.Max(0, subtotal - descuento) * porcentajeImpuesto / 100 : 0;
    private decimal total => subtotal - descuento + iva;
    private bool TotalesValidos => descuento >= 0 && descuento <= subtotal && porcentajeImpuesto >= 0 && porcentajeImpuesto <= 100 && total >= 0;
    private bool PuedeAbrirCobro => PuedeEscanear && carrito.Count > 0 && metodoPago == "Efectivo" && TotalesValidos && ventaActual.ProductosActivos;
    private string AyudaCobro
    {
        get
        {
            if (!recibido.HasValue) return "Captura el efectivo recibido.";
            if (recibido.Value < 0 || decimal.Round(recibido.Value, 2) != recibido.Value) return "Revisa el monto: no puede ser negativo y admite hasta dos decimales.";
            if (recibido.Value < ventaActual.TotalEfectivo) return $"Faltan ${(ventaActual.TotalEfectivo - recibido.Value):N2} para cubrir el total.";
            return "Revisa el cambio y confirma el cobro.";
        }
    }
    private void AbrirCobro()
    {
        if (!PuedeAbrirCobro) return;
        errorVenta = ""; mostrarModalCobro = true;
    }
    private void CerrarCobro()
    {
        if (!procesando) mostrarModalCobro = false;
    }
    private async Task ProcesarVenta()
    {
        // Los demás métodos son una vista previa del diseño: no se registran como pagados.
        if (!mostrarModalCobro || procesando || carrito.Count == 0 || metodoPago != "Efectivo" || !TotalesValidos || !ventaActual.ProductosActivos) return;
        if (!ventaActual.EfectivoValido) { errorVenta = "Captura un monto válido que cubra el total antes de confirmar."; return; }
        procesando = true; errorVenta = "";
        try
        {
            var dto = new CrearVentaDto { ClienteId = clienteSeleccionado, UsuarioId = Sesion.UsuarioActual?.Id ?? 0, OperacionId = operacionVenta, MetodoPago = metodoPago, Recibido = recibido,
                Descuento = descuento, Impuesto = iva, Detalles = carrito.Select(i => new CrearDetalleVentaDto { ProductoId = i.Producto.Id, Cantidad = i.Cantidad, PrecioUnitario = i.Producto.PrecioVenta }).ToList() };
            var indiceVenta = Pedidos.Indice;
            var venta = await VentaService.CrearAsync(dto);
            if (venta.Id <= 0 || !string.Equals(venta.EstadoVenta, "Completada", StringComparison.OrdinalIgnoreCase))
            {
                errorVenta = "La venta no tiene confirmación de registro. Revisa su estado antes de continuar."; return;
            }
            ReiniciarVenta(); await CargarDatos();
            confirmacionPendiente = (indiceVenta, venta.Id);
            Avisar($"Venta {venta.NumeroVenta} registrada · ${venta.Total:N2}");
        }
        catch (Exception) { errorVenta = "No se pudo registrar la venta. Revisa las existencias y la conexión antes de reintentar."; }
        finally { procesando = false; }
    }
    public async ValueTask DisposeAsync()
    {
        eliminado = true; BalanzaService.EstadoChanged -= ManejarCambioBalanza;
        detener.Cancel(); if (reloj != null) await reloj; detener.Dispose();
        if (modulo != null)
        {
            try { await modulo.InvokeVoidAsync("desconectar", raiz); await modulo.DisposeAsync(); }
            catch (JSDisconnectedException) { }
        }
        referencia?.Dispose();
    }
}
