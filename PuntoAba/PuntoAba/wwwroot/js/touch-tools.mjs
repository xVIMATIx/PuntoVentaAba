import { Calculadora } from "./calculadora.mjs";
const conexiones = new WeakMap();
const tipos = new Set(["text", "search", "tel", "email", "url", "password", "number"]);
export function esCampo(elemento) {
    return elemento && !elemento.disabled && !elemento.readOnly &&
        (elemento.tagName === "TEXTAREA" || (elemento.tagName === "INPUT" && tipos.has(elemento.type)));
}
export function editar(valor, inicio, fin, texto, borrar = false) {
    if (borrar && inicio === fin && inicio > 0) inicio -= Array.from(valor.slice(0, inicio)).at(-1).length;
    return { valor: valor.slice(0, inicio) + texto + valor.slice(fin), cursor: inicio + texto.length };
}
export function conectar(raiz) {
    desconectar(raiz);
    const abortar = new AbortController(), signal = abortar.signal;
    const shell = raiz.closest(".aba-shell, .aba-login-shell");
    const calculadora = new Calculadora(), valores = new WeakMap();
    let campo = null, panel = null, mayusculas = false, escribiendo = false, seleccionarTodo = false;
    const mensaje = raiz.querySelector("[data-tool-message]");
    function informar(texto) { mensaje.textContent = texto; mensaje.hidden = !texto; }
    function existe() { return esCampo(campo) && document.contains(campo); }
    function destino() {
        let nombre = "Selecciona un campo para escribir";
        if (existe()) nombre = campo.getAttribute("aria-label") || campo.labels?.[0]?.textContent?.trim() || campo.closest("div")?.querySelector("label")?.textContent?.trim() || campo.placeholder || "Campo seleccionado";
        for (const nodo of raiz.querySelectorAll("[data-destino]")) nodo.textContent = nombre;
    }
    function visibleCampo() {
        if (existe()) requestAnimationFrame(() => campo?.scrollIntoView({ block: "nearest", inline: "nearest" }));
    }
    function enfocado(e) {
        if (raiz.contains(e.target)) return;
        if (esCampo(e.target)) { campo = e.target; seleccionarTodo = false; valores.set(campo, campo.value); destino(); if (panel) visibleCampo(); }
        else if (e.target.tagName === "INPUT" || e.target.tagName === "SELECT") { campo = null; destino(); }
    }
    document.addEventListener("focusin", enfocado, { signal });
    document.addEventListener("input", e => { if (!escribiendo && esCampo(e.target)) valores.set(e.target, e.target.value); }, { signal });
    function mostrar(nombre) {
        panel = panel === nombre ? null : nombre;
        for (const nodo of raiz.querySelectorAll("[data-panel]")) nodo.hidden = nodo.dataset.panel !== panel;
        for (const boton of raiz.querySelectorAll("[data-tool-toggle]")) {
            const activo = boton.dataset.toolToggle === panel;
            boton.classList.toggle("aba-tool-active", activo); boton.setAttribute("aria-expanded", String(activo));
            const etiqueta = boton.querySelector("[data-tool-label]");
            if (etiqueta) etiqueta.textContent = activo ? "Cerrar teclado táctil" : "Abrir teclado táctil";
        }
        informar(""); destino(); visibleCampo();
    }
    document.addEventListener("aba:teclado", e => {
        if (e.detail?.solo && panel !== e.detail.solo) return;
        const nombre = e.detail?.panel ?? null;
        if (nombre !== null && !["numeros", "letras", "calculadora"].includes(nombre)) return;
        if (panel !== nombre) mostrar(nombre);
    }, { signal });
    function escribir(texto, borrar = false, reemplazar = false) {
        if (!existe()) { campo = null; destino(); informar("Selecciona primero un campo editable."); return; }
        if (campo.type === "number" && texto && !/^[0-9.+-]+$/.test(texto)) { informar("Este campo solo admite números."); return; }
        const parcial = valores.get(campo);
        const actual = campo.type === "number" && campo.value === "" && parcial && /^[+-]?(?:[0-9]*\.)?$/.test(parcial) ? parcial : campo.value;
        let inicio = campo.selectionStart ?? actual.length, fin = campo.selectionEnd ?? actual.length;
        if (seleccionarTodo || reemplazar) { inicio = 0; fin = actual.length; }
        const cambio = editar(actual, inicio, fin, texto, borrar);
        if (campo.maxLength > -1 && cambio.valor.length > campo.maxLength) { informar("El campo alcanzó su longitud máxima."); return; }
        seleccionarTodo = false;
        valores.set(campo, cambio.valor);
        escribiendo = true;
        campo.value = cambio.valor;
        campo.dispatchEvent(new Event("input", { bubbles: true }));
        campo.dispatchEvent(new Event("change", { bubbles: true }));
        campo.dispatchEvent(new KeyboardEvent("keyup", { bubbles: true, key: borrar ? "Backspace" : texto }));
        escribiendo = false;
        const destinoActual = campo;
        destinoActual.focus({ preventScroll: true });
        if (destinoActual.type !== "number") try { destinoActual.setSelectionRange(cambio.cursor, cambio.cursor); } catch { }
        informar("");
    }
    function siguiente() {
        const actual = existe() ? campo : document.activeElement?.tagName === "SELECT" ? document.activeElement : null;
        if (!actual) { informar("Selecciona primero un campo editable."); return; }
        // Solo los campos de lectura y el acceso pueden enviar Enter; nunca guarda productos o cobra una venta.
        if (actual.hasAttribute("data-touch-enter")) { actual.form?.requestSubmit(); return; }
        const contenedor = actual.closest(".modal.show, .modal-popup") || shell;
        const campos = [...contenedor.querySelectorAll("input, textarea, select")].filter(e => !e.disabled && !e.readOnly && e.getClientRects().length && (e.tagName === "SELECT" || esCampo(e)));
        const siguiente = campos[campos.indexOf(actual) + 1];
        if (siguiente) siguiente.focus(); else mostrar(null);
    }
    raiz.addEventListener("pointerdown", e => { if (e.target.closest("button")) e.preventDefault(); }, { signal });
    raiz.addEventListener("click", e => {
        const boton = e.target.closest("button"); if (!boton || boton.disabled) return;
        if (boton.dataset.toolToggle) { mostrar(boton.dataset.toolToggle); return; }
        if (boton.dataset.calc !== undefined) {
            calculadora.pulsar(boton.dataset.calc);
            raiz.querySelector("[data-calc-display]").textContent = calculadora.texto; return;
        }
        if (boton.dataset.key !== undefined) { escribir(mayusculas ? boton.dataset.key.toUpperCase() : boton.dataset.key); return; }
        switch (boton.dataset.action) {
            case "cerrar": mostrar(null); break;
            case "mayusculas":
                mayusculas = !mayusculas; raiz.classList.toggle("aba-uppercase", mayusculas); boton.setAttribute("aria-pressed", String(mayusculas)); break;
            case "seleccionar":
                if (existe()) { seleccionarTodo = true; try { campo.select(); } catch { } } else informar("Selecciona primero un campo editable."); break;
            case "borrar": escribir("", true); break;
            case "limpiar": escribir("", false, true); break;
            case "siguiente": siguiente(); break;
            case "resultado":
                if (calculadora.resultado === null) { informar("Primero realiza una operación válida."); break; }
                if (!existe() || campo.type === "password" || campo.type === "email") { informar("Selecciona un campo de texto o cantidad para usar el resultado."); break; }
                escribir(calculadora.resultado, false, true); break;
        }
    }, { signal });
    const tamanio = new ResizeObserver(() => { shell?.style.setProperty("--aba-tools-height", raiz.getBoundingClientRect().height + "px"); if (panel) visibleCampo(); });
    tamanio.observe(raiz);
    if (esCampo(document.activeElement)) { campo = document.activeElement; valores.set(campo, campo.value); destino(); }
    conexiones.set(raiz, {
        cerrar() { campo = null; seleccionarTodo = false; mostrar(null); },
        liberar() { abortar.abort(); tamanio.disconnect(); shell?.style.removeProperty("--aba-tools-height"); campo = null; }
    });
}
export function alNavegar(raiz) { conexiones.get(raiz)?.cerrar(); }
export function desconectar(raiz) { conexiones.get(raiz)?.liberar(); conexiones.delete(raiz); }
