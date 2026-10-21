export class Calculadora {
    constructor() { this.limpiar(); }
    limpiar() { this.texto = "0"; this.anterior = null; this.operador = null; this.nuevo = true; this.error = false; }
    formatear(n) {
        if (!Number.isFinite(n) || Math.abs(n) > 1e15) { this.texto = "Fuera de rango"; this.error = true; return; }
        this.texto = String(Number(n.toPrecision(12))); this.error = false;
    }
    pulsar(tecla) {
        if (tecla === "AC") { this.limpiar(); return; }
        if (this.error) this.limpiar();
        if (/^\d$/.test(tecla)) {
            if (this.nuevo) this.texto = tecla;
            else if (this.texto.replace(/[-.]/g, "").length < 14) this.texto = this.texto === "0" ? tecla : this.texto + tecla;
            this.nuevo = false; return;
        }
        if (tecla === ".") {
            if (this.nuevo) this.texto = "0.";
            else if (!this.texto.includes(".")) this.texto += ".";
            this.nuevo = false; return;
        }
        if (tecla === "⌫") { if (!this.nuevo) this.texto = this.texto.length > 1 ? this.texto.slice(0, -1) : "0"; if (this.texto === "-") this.texto = "0"; return; }
        if (tecla === "±") { this.formatear(-Number(this.texto)); this.nuevo = false; return; }
        if (tecla === "%") { this.formatear(Number(this.texto) / 100); this.nuevo = false; return; }
        if (["+", "−", "×", "÷", "="].includes(tecla)) {
            if (this.operador && this.anterior !== null && !this.nuevo) {
                const a = this.anterior, b = Number(this.texto);
                if (this.operador === "÷" && b === 0) { this.texto = "No se puede dividir entre cero"; this.error = true; this.operador = null; this.anterior = null; this.nuevo = true; return; }
                const r = this.operador === "+" ? a + b : this.operador === "−" ? a - b : this.operador === "×" ? a * b : a / b;
                this.formatear(r);
                if (this.error) { this.operador = null; this.anterior = null; this.nuevo = true; return; }
            }
            this.operador = tecla === "=" ? null : tecla;
            this.anterior = tecla === "=" ? null : Number(this.texto);
            this.nuevo = true;
        }
    }
    get resultado() { return this.error ? null : this.texto; }
}
