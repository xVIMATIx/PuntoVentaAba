// Aviso opcional: nunca interviene en el registro de una venta.
export class ConfirmacionVenta {
    constructor(Audio = globalThis.AudioContext || globalThis.webkitAudioContext) {
        this.Audio = Audio; this.audio = null; this.animaciones = new Set(); this.confirmadas = new Set();
    }
    preparar() {
        try {
            if (!this.Audio) return;
            this.audio ??= new this.Audio();
            if (this.audio.state === "suspended") Promise.resolve(this.audio.resume()).catch(() => {});
        } catch { /* El aviso visual funciona incluso sin audio disponible. */ }
    }
    confirmar(boton, ventaId) {
        if (!ventaId || this.confirmadas.has(ventaId)) return;
        this.confirmadas.add(ventaId);
        if (this.confirmadas.size > 100) this.confirmadas.delete(this.confirmadas.values().next().value);
        try {
            if (boton) {
                const verde = { backgroundColor:"#dff3e6", borderColor:"#5caa77", color:"#245b39", boxShadow:"inset 0 -2px 0 #5caa77" };
                const animacion = boton.animate([verde, verde], { duration:10000 });
                this.animaciones.add(animacion);
                animacion.finished.then(() => this.animaciones.delete(animacion), () => this.animaciones.delete(animacion));
            }
        } catch { /* Un fallo visual no debe modificar el resultado de la venta. */ }
        try {
            if (this.audio?.state !== "running") return;
            const audio = this.audio, ahora = audio.currentTime;
            const tono = audio.createOscillator(), volumen = audio.createGain();
            tono.type = "sine"; tono.frequency.setValueAtTime(880, ahora);
            volumen.gain.setValueAtTime(.0001, ahora);
            volumen.gain.linearRampToValueAtTime(.055, ahora + .008);
            volumen.gain.exponentialRampToValueAtTime(.0001, ahora + .14);
            tono.connect(volumen); volumen.connect(audio.destination);
            tono.onended = () => { tono.disconnect(); volumen.disconnect(); };
            tono.start(ahora); tono.stop(ahora + .15);
        } catch { /* El navegador puede bloquear o no disponer de salida de sonido. */ }
    }
    liberar() {
        for (const animacion of this.animaciones) animacion.cancel();
        this.animaciones.clear(); this.confirmadas.clear();
        try { if (this.audio && this.audio.state !== "closed") Promise.resolve(this.audio.close()).catch(() => {}); } catch { }
        this.audio = null;
    }
}
