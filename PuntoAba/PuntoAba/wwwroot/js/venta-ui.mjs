import { ConfirmacionVenta } from "./confirmacion-venta.mjs?v=10s";
const conexiones = new WeakMap();
export function limitarAncho(total, proporcion) {
    if (total <= 0) return 0;
    const espacio = total - 16;
    return Math.min(Math.max(260, total * proporcion), Math.max(260, espacio - 330));
}
export class LecturaPistola {
    constructor() { this.texto = ""; this.ultima = 0; }
    limpiar() { this.texto = ""; this.ultima = 0; }
    tecla(key, tiempo) {
        if (tiempo - this.ultima > 80) this.texto = "";
        this.ultima = tiempo;
        if (key === "Enter") { const codigo = this.texto.length >= 3 ? this.texto : null; this.limpiar(); return codigo; }
        if (key.length === 1) this.texto = (this.texto + key).slice(-100);
        else this.limpiar();
        return null;
    }
}
// Solo los campos marcados confirman; los botones conservan su Enter nativo.
export function enterEnCampo(e, raiz, preparar = () => {}) {
    if (e.key !== "Enter" || e.ctrlKey || e.altKey || e.metaKey || e.shiftKey || e.isComposing) return false;
    const campo = e.target;
    if (!raiz.contains(campo)) return false;
    const selector = campo.dataset?.posEnter || campo.dataset?.posNext;
    if (!selector) return false;
    e.preventDefault(); e.stopPropagation();
    if (e.repeat || campo.disabled) return true;
    const contenedor = campo.closest(".pos-dialog") || raiz;
    const boton = contenedor.querySelector(selector);
    if (!boton || boton.disabled || !boton.getClientRects().length) return true;
    if (campo.dataset.posNext) {
        document.dispatchEvent(new CustomEvent("aba:teclado", { detail:{ panel:null, solo:"numeros" } }));
        boton.focus({ preventScroll:true });
    } else { preparar(); boton.click(); }
    return true;
}
function leer(clave, defecto) { try { return JSON.parse(localStorage.getItem(clave)) ?? defecto; } catch { return defecto; } }
function guardar(clave, valor) { try { localStorage.setItem(clave, JSON.stringify(valor)); } catch { /* La venta sigue funcionando sin almacenamiento local. */ } }
export function conectar(raiz, dotnet, usuario) {
    desconectar(raiz);
    const abortar = new AbortController(), signal = abortar.signal;
    const layout = raiz.querySelector(".pos-layout"), barra = raiz.querySelector(".pos-divider");
    const scroll = raiz.querySelector(".pos-catalog-scroll"), arriba = raiz.querySelector("[data-pos-top]");
    const clave = "aba.venta.favoritos." + usuario, claveAncho = "aba.venta.ancho", clavePedidos = "aba.venta.pedidos." + usuario;
    const lector = new LecturaPistola(), lectorCampo = new LecturaPistola();
    let campoLectura = null;
    const confirmacion = new ConfirmacionVenta();
    function prepararAviso(e) {
        const boton = e.target.closest?.(".pos-charge, [data-pos-confirm]");
        if (boton && !boton.disabled) confirmacion.preparar();
    }
    raiz.addEventListener("pointerdown", prepararAviso, { signal });
    raiz.addEventListener("keydown", e => { if (e.key === "Enter" || e.key === " ") prepararAviso(e); }, { signal });
    let permitido = false, arrastre = null, toqueAnterior = 0, dialogo = null, focoAnterior = null, numerosAuto = false, cargando = false;
    const guardado = leer(claveAncho, .60);
    let proporcion = typeof guardado === "number" && Number.isFinite(guardado) && guardado > 0 && guardado < 1 ? guardado : .60;
    function aplicarAncho() {
        const total = layout.clientWidth;
        const ancho = limitarAncho(total, proporcion);
        layout.style.setProperty("--pos-catalog-width", ancho + "px");
        barra.setAttribute("aria-valuenow", String(Math.round(ancho / total * 100)));
        barra.setAttribute("aria-valuemin", String(Math.round(260 / total * 100)));
        barra.setAttribute("aria-valuemax", String(Math.round(Math.max(260, total - 346) / total * 100)));
    }
    function restablecer() { proporcion = .60; guardar(claveAncho, proporcion); aplicarAncho(); }
    const tamano = new ResizeObserver(aplicarAncho); tamano.observe(layout); aplicarAncho();
    barra.addEventListener("pointerdown", e => {
        if (e.button !== 0) return;
        e.preventDefault(); barra.setPointerCapture(e.pointerId);
        arrastre = { id:e.pointerId, x:e.clientX, movido:false }; barra.classList.add("is-dragging");
    }, { signal });
    barra.addEventListener("pointermove", e => {
        if (!arrastre || e.pointerId !== arrastre.id) return;
        if (Math.abs(e.clientX - arrastre.x) > 4) arrastre.movido = true;
        if (!arrastre.movido) return;
        const limites = layout.getBoundingClientRect();
        proporcion = limitarAncho(limites.width, (e.clientX - limites.left) / limites.width) / limites.width;
        aplicarAncho();
    }, { signal });
    function soltar(e) {
        if (!arrastre || e.pointerId !== arrastre.id) return;
        if (e.type === "pointerup" && !arrastre.movido) {
            const ahora = performance.now();
            if (ahora - toqueAnterior < 350) { restablecer(); toqueAnterior = 0; } else toqueAnterior = ahora;
        } else toqueAnterior = 0;
        guardar(claveAncho, proporcion); arrastre = null; barra.classList.remove("is-dragging");
    }
    barra.addEventListener("pointerup", soltar, { signal });
    barra.addEventListener("pointercancel", soltar, { signal });
    barra.addEventListener("lostpointercapture", e => { if (arrastre?.id === e.pointerId) { arrastre = null; barra.classList.remove("is-dragging"); } }, { signal });
    barra.addEventListener("dblclick", restablecer, { signal });
    barra.addEventListener("keydown", e => {
        if (!["ArrowLeft", "ArrowRight", "Home", "End"].includes(e.key)) return;
        e.preventDefault(); e.stopPropagation();
        proporcion = e.key === "Home" ? 0 : e.key === "End" ? 1 : proporcion + (e.key === "ArrowRight" ? 1 : -1) * (e.shiftKey ? .05 : .02);
        proporcion = limitarAncho(layout.clientWidth, proporcion) / layout.clientWidth;
        guardar(claveAncho, proporcion); aplicarAncho();
    }, { signal });
    scroll.addEventListener("scroll", () => { arriba.hidden = scroll.scrollTop < 350; }, { passive:true, signal });
    arriba.addEventListener("click", () => scroll.scrollTo({ top:0, behavior:matchMedia("(prefers-reduced-motion: reduce)").matches ? "instant" : "smooth" }), { signal });
    const observador = new IntersectionObserver(async entries => {
        if (!entries.some(x => x.isIntersecting) || cargando || !scroll.querySelector("[data-pos-sentinel] button")) return;
        cargando = true;
        try { await dotnet.invokeMethodAsync("CargarMas"); } finally { cargando = false; }
    }, { root:scroll, rootMargin:"150px" });
    observador.observe(scroll.querySelector("[data-pos-sentinel]"));
    function abrirNumeros(campo) {
        campo.focus({ preventScroll:true });
        const numeros = document.querySelector('.aba-tools [data-panel="numeros"]');
        if (campo.closest(".pos-dialog") && numeros?.hidden) numerosAuto = true;
        document.dispatchEvent(new CustomEvent("aba:teclado", { detail:{ panel:"numeros" } }));
    }
    function cerrarNumeros() {
        if (!numerosAuto) return;
        document.dispatchEvent(new CustomEvent("aba:teclado", { detail:{ panel:null, solo:"numeros" } }));
        numerosAuto = false;
    }
    raiz.addEventListener("focusin", e => { if (e.target.matches("[data-pos-numeric]") && !e.target.readOnly) abrirNumeros(e.target); }, { signal });
    raiz.addEventListener("keydown", e => {
        const tab = e.target.closest("[data-sale-tab]");
        if (!tab || !["ArrowLeft", "ArrowRight", "Home", "End"].includes(e.key)) return;
        e.preventDefault();
        const actual = Number(tab.dataset.saleTab);
        const indice = e.key === "Home" ? 0 : e.key === "End" ? 4 : (actual + (e.key === "ArrowRight" ? 1 : 4)) % 5;
        dotnet.invokeMethodAsync("SeleccionarVenta", indice);
    }, { signal });
    document.addEventListener("keydown", e => {
        if (!document.contains(raiz)) return;
        // Una ráfaga de código en un campo de confirmación no debe cobrar ni agregar peso.
        const campoEnter = e.target.closest?.("[data-pos-enter]");
        if (campoEnter && !e.ctrlKey && !e.altKey && !e.metaKey && !e.isComposing && !e.repeat) {
            if (campoLectura !== campoEnter) lectorCampo.limpiar();
            campoLectura = campoEnter;
            const codigo = lectorCampo.tecla(e.key, performance.now());
            if (codigo?.length >= 6) { e.preventDefault(); e.stopPropagation(); return; }
        } else { lectorCampo.limpiar(); campoLectura = null; }
        // Evita que mantener Enter presionado confirme la ventana que acaba de abrirse.
        if (e.key === "Enter" && e.repeat && raiz.contains(e.target)) { e.preventDefault(); e.stopPropagation(); return; }
        if (enterEnCampo(e, raiz, () => confirmacion.preparar())) return;
        const ventana = raiz.querySelector(".pos-dialog");
        if (ventana) {
            lector.limpiar();
            if (e.key === "Escape") { e.preventDefault(); dotnet.invokeMethodAsync("CerrarVentanas"); }
            if (e.key === "Tab") {
                const campos = [...ventana.querySelectorAll('button:not(:disabled), input:not(:disabled):not([readonly]), [tabindex="0"]')].filter(x => x.getClientRects().length);
                const primero = campos[0], ultimo = campos.at(-1);
                if (!primero) { e.preventDefault(); ventana.focus(); }
                else if (e.shiftKey && (document.activeElement === primero || document.activeElement === ventana)) { e.preventDefault(); ultimo.focus(); }
                else if (!e.shiftKey && document.activeElement === ultimo) { e.preventDefault(); primero.focus(); }
            }
            return;
        }
        const editable = e.target.closest?.('input, textarea, select, [contenteditable="true"], .aba-tools');
        if (!permitido || editable || e.ctrlKey || e.altKey || e.metaKey || e.isComposing || e.repeat) { lector.limpiar(); return; }
        if (e.key === "Shift") return;
        const codigo = lector.tecla(e.key, performance.now());
        if (codigo) { e.preventDefault(); e.stopPropagation(); dotnet.invokeMethodAsync("LeerGlobal", codigo); }
    }, { capture:true, signal });
    conexiones.set(raiz, {
        actualizar(habilitado, reiniciar) {
            permitido = habilitado;
            if (!habilitado) lector.limpiar();
            if (reiniciar) { scroll.scrollTop = 0; arriba.hidden = true; }
            const actual = raiz.querySelector(".pos-dialog");
            if (actual !== dialogo) {
                if (actual) { focoAnterior = document.activeElement; (actual.querySelector('input:not([readonly]):not(:disabled)') || actual.querySelector('button:not(:disabled)') || actual).focus({ preventScroll:true }); }
                else {
                    if (dialogo?.classList.contains("pos-cash-dialog")) {
                        document.dispatchEvent(new CustomEvent("aba:teclado", { detail:{ panel:null, solo:"numeros" } })); numerosAuto = false;
                    } else cerrarNumeros();
                    if (document.contains(focoAnterior) && !focoAnterior.disabled) focoAnterior.focus({ preventScroll:true });
                    else raiz.querySelector("#pos-busqueda")?.focus({ preventScroll:true });
                }
                dialogo = actual;
            }
        },
        capturar() {
            const campo = raiz.querySelector("#pos-peso");
            if (campo && !campo.readOnly) { abrirNumeros(campo); campo.select(); }
        },
        cerrar:cerrarNumeros,
        confirmar(indice, ventaId) { confirmacion.confirmar(raiz.querySelector(`[data-sale-tab="${indice}"]`), ventaId); },
        cambiarPedido() {
            lector.limpiar();
            if (raiz.contains(document.activeElement)) document.activeElement.blur();
            document.dispatchEvent(new CustomEvent("aba:teclado", { detail:{ panel:null } }));
        },
        recuperarPedidos() { try { const json = sessionStorage.getItem(clavePedidos) || ""; return json.length <= 250000 ? json : ""; } catch { return ""; } },
        guardarPedidos(json) { try { sessionStorage.setItem(clavePedidos, json); return true; } catch { return false; } },
        guardar(ids) { guardar(clave, ids); },
        liberar() { confirmacion.liberar(); cerrarNumeros(); abortar.abort(); tamano.disconnect(); observador.disconnect(); }
    });
    const ids = leer(clave, []);
    return Array.isArray(ids) ? ids.filter(x => Number.isInteger(x) && x > 0).slice(0, 10000) : [];
}
export function actualizar(raiz, permitido, reiniciar) { conexiones.get(raiz)?.actualizar(permitido, reiniciar); }
export function capturarCantidad(raiz) { conexiones.get(raiz)?.capturar(); }
export function cerrarCantidad(raiz) { conexiones.get(raiz)?.cerrar(); }
export function guardarFavoritos(raiz, ids) { conexiones.get(raiz)?.guardar(ids); }
export function desconectar(raiz) { conexiones.get(raiz)?.liberar(); conexiones.delete(raiz); }

export function cambiarPedido(raiz) { conexiones.get(raiz)?.cambiarPedido(); }
export function enfocarPestana(raiz, indice) { raiz.querySelector(`[data-sale-tab="${indice}"]`)?.focus({ preventScroll:true }); }
export function recuperarPedidos(raiz) { return conexiones.get(raiz)?.recuperarPedidos() || ""; }
export function guardarPedidos(raiz, json) { return conexiones.get(raiz)?.guardarPedidos(json) ?? false; }

export function confirmarVenta(raiz, indice, ventaId) { conexiones.get(raiz)?.confirmar(indice, ventaId); }
