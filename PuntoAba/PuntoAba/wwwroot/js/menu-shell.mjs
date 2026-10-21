const conexiones = new WeakMap();
export function temporizadorMenu(ocultar, opciones = {}) {
    const esperar = opciones.programar ?? setTimeout, cancelar = opciones.cancelar ?? clearTimeout;
    const minutos = opciones.retraso ?? 300000;
    let tarea = null;
    return {
        reiniciar() { if (tarea !== null) cancelar(tarea); tarea = esperar(() => { tarea = null; ocultar(); }, minutos); },
        detener() { if (tarea !== null) cancelar(tarea); tarea = null; }
    };
}
export function conectar(shell) {
    desconectar(shell);
    const eventos = new AbortController(), menu = shell.querySelector(".aba-sidebar");
    const abrir = shell.querySelector("[data-menu-open]");
    const cerrar = shell.querySelector("[data-menu-close]");
    let oculto = false;
    const temporizador = temporizadorMenu(() => cambiar(true));
    function cambiar(valor) {
        oculto = valor;
        shell.classList.toggle("aba-menu-hidden", oculto);
        menu.inert = oculto;
        menu.setAttribute("aria-hidden", String(oculto));
        abrir.setAttribute("aria-expanded", String(!oculto));
        if (oculto) {
            temporizador.detener();
            if (menu.contains(document.activeElement)) abrir.focus({ preventScroll: true });
        } else temporizador.reiniciar();
    }
    abrir.addEventListener("click", () => cambiar(false), { signal: eventos.signal });
    cerrar.addEventListener("click", () => cambiar(true), { signal: eventos.signal });
    for (const tipo of ["pointerdown", "keydown", "focusin", "scroll"])
        menu.addEventListener(tipo, () => { if (!oculto) temporizador.reiniciar(); }, { signal: eventos.signal, passive: true });
    temporizador.reiniciar();
    conexiones.set(shell, () => { eventos.abort(); temporizador.detener(); });
}
export function desconectar(shell) { conexiones.get(shell)?.(); conexiones.delete(shell); }
