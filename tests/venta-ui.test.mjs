import assert from "node:assert/strict";
import { limitarAncho, LecturaPistola, enterEnCampo } from "../PuntoAba/PuntoAba/wwwroot/js/venta-ui.mjs";
assert.equal(limitarAncho(810, .6), 464);
assert.equal(limitarAncho(1200, -.2), 260);
assert.equal(limitarAncho(1200, 2), 854);
assert.equal(limitarAncho(0, .6), 0);
const l = new LecturaPistola();
let t = 1000;
for (const k of "0012345678901") { assert.equal(l.tecla(k, t), null); t += 10; }
assert.equal(l.tecla("Enter", t), "0012345678901");
assert.equal(l.tecla("Enter", t+1), null);
for (const k of "nombre") { l.tecla(k, t); t += 150; }
assert.equal(l.tecla("Enter", t), null);
l.tecla("1", t); l.tecla("2", t+10); l.limpiar();
assert.equal(l.tecla("Enter", t+20), null);
console.log("OK: divisor respeta anchos mínimos; captura conserva ceros, descarta escritura lenta y no duplica Enter.");

// Confirmar únicamente campos marcados y acciones visibles, válidas y habilitadas.
let clicks=0, preparados=0, prevenidos=0;
const boton={disabled:false,getClientRects:()=>[{}],click:()=>clicks++};
const raiz={contains:x=>x===campo,querySelector:()=>boton};
const campo={dataset:{posEnter:"[data-pos-confirm]"},disabled:false,closest:()=>null};
const evento=(cambios={})=>({key:"Enter",target:campo,preventDefault:()=>prevenidos++,stopPropagation:()=>{},...cambios});
assert.equal(enterEnCampo(evento(),raiz,()=>preparados++),true);
assert.equal(clicks,1);assert.equal(preparados,1);
boton.disabled=true;
enterEnCampo(evento(),raiz); assert.equal(clicks,1);
boton.disabled=false;
enterEnCampo(evento({repeat:true}),raiz); assert.equal(clicks,1);
for(const cambio of [{ctrlKey:true},{altKey:true},{metaKey:true},{shiftKey:true},{isComposing:true},{key:"Tab"}]) {
 assert.equal(enterEnCampo(evento(cambio),raiz),false);
}
campo.dataset={}; assert.equal(enterEnCampo(evento(),raiz),false);
campo.dataset={posEnter:".pos-client-list button:only-child"};
raiz.querySelector=()=>null; enterEnCampo(evento(),raiz); assert.equal(clicks,1);
raiz.querySelector=()=>boton; boton.getClientRects=()=>[]; enterEnCampo(evento(),raiz);assert.equal(clicks,1);
assert.equal(prevenidos,5);
console.log("OK: Enter confirma campos explícitos; bloquea acciones deshabilitadas, invisibles, ambiguas, repetidas y combinaciones de teclado.");
