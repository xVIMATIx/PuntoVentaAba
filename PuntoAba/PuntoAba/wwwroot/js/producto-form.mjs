const preparados = new WeakSet();
export function preparar(editor) {
    if (!preparados.has(editor)) {
        preparados.add(editor);
        editor.addEventListener("keydown", e => {
            if (e.key !== "Enter" || e.isComposing || e.ctrlKey || e.altKey || e.metaKey || e.shiftKey) return;
            if (e.repeat) { e.preventDefault(); e.stopPropagation(); return; }
            if (!e.target.matches("[data-product-next]")) return;
            e.preventDefault(); e.stopPropagation();
            const campos = [...editor.querySelectorAll("input:not([type=file]), select, textarea")].filter(c => !c.disabled && !c.readOnly && c.getClientRects().length);
            const siguiente = campos[campos.indexOf(e.target) + 1] || editor.querySelector("[data-product-save]");
            siguiente?.focus({ preventScroll:true });
        });
    }
    const codigo = editor.querySelector("#producto-codigo");
    codigo?.focus({ preventScroll:true });
    codigo?.select();
}
