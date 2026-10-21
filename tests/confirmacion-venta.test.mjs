import assert from "node:assert/strict";
import { ConfirmacionVenta } from "../PuntoAba/PuntoAba/wwwroot/js/confirmacion-venta.mjs";
const acciones = [], animaciones = [];
class AudioPrueba {
    constructor() { this.state="suspended";this.currentTime=10;this.destination={}; }
    resume() { this.state="running";return Promise.resolve(); }
    close() { this.state="closed";acciones.push("cerrar");return Promise.resolve(); }
    createOscillator() { return { frequency:{setValueAtTime(n){acciones.push(["frecuencia",n]);}},connect(){},disconnect(){},start(t){acciones.push(["inicio",t]);},stop(t){acciones.push(["fin",t]);} }; }
    createGain() { return { gain:{setValueAtTime(){},linearRampToValueAtTime(n){acciones.push(["volumen",n]);},exponentialRampToValueAtTime(){}},connect(){},disconnect(){} }; }
}
const boton={animate(fotogramas, opciones){ const a={fotogramas,opciones,finished:new Promise(()=>{}),cancel(){acciones.push("cancelar");}};animaciones.push(a);return a;}};
const aviso=new ConfirmacionVenta(AudioPrueba);aviso.preparar();
assert.equal(acciones.length,0,"Preparar audio no emite sonido ni confirmación");
aviso.confirmar(boton,12);
assert.equal(animaciones[0].opciones.duration,10000);
assert.equal(animaciones[0].fotogramas[0].backgroundColor,"#dff3e6");
assert.ok(acciones.some(x=>x[0]==="frecuencia"&&x[1]===880));
assert.ok(acciones.some(x=>x[0]==="volumen"&&x[1]===.055));
assert.ok(Math.abs(acciones.find(x=>x[0]==="fin")[1]-10.15)<.00001);
aviso.confirmar(boton,12);assert.equal(animaciones.length,1,"No duplica el aviso para una venta ya confirmada");
aviso.confirmar(boton,13);assert.equal(animaciones.length,2);
aviso.liberar();assert.ok(acciones.includes("cerrar"));assert.ok(acciones.includes("cancelar"));
class AudioBloqueado { constructor(){throw new Error("Sin audio");} }
const sinAudio=new ConfirmacionVenta(AudioBloqueado);sinAudio.preparar();sinAudio.confirmar(boton,14);
assert.equal(animaciones.length,3,"Sin audio conserva la confirmación visual");
assert.doesNotThrow(()=>sinAudio.confirmar({animate(){throw new Error("Sin animación");}},15));sinAudio.liberar();
console.log("OK: verde de 10 segundos, tono breve de volumen bajo, aviso único por venta, limpieza y tolerancia a fallos de audio.");
