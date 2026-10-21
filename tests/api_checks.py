import json,urllib.request,urllib.error,uuid,sys,datetime
base=(sys.argv[1] if len(sys.argv)>1 else 'http://localhost:5003').rstrip('/')
def req(method,path,data=None,token=None,expect=200,content=None):
    body=json.dumps(data).encode() if data is not None else content
    headers={}
    if data is not None:headers['Content-Type']='application/json'
    if token:headers['Authorization']='Bearer '+token
    r=urllib.request.Request(base+path,data=body,method=method,headers=headers)
    try:
        with urllib.request.urlopen(r,timeout=20) as response:status=response.status;raw=response.read()
    except urllib.error.HTTPError as e:status=e.code;raw=e.read()
    assert status==expect,f'{method} {path}: {status} != {expect}: {raw[:300]!r}'
    try:return json.loads(raw)
    except json.JSONDecodeError:return raw
def ok(msg):print('OK API:',msg)
req('GET','/api/inventario/entradas',expect=401)
admin=req('POST','/api/auth/login',{'nombreUsuario':'admin','password':'Pruebas123'})['token']
cajero=req('POST','/api/auth/login',{'nombreUsuario':'cajero-api','password':'Pruebas123'})['token']
swagger=req('GET','/swagger/v1/swagger.json')
for path in ['/api/inventario/entradas','/api/inventario/conteos','/api/inventario/reporte','/api/respaldos','/api/productos/imagenes']:
    assert path in swagger['paths'],path
ok('Swagger publica los contratos, incluida carga de imágenes')
for path in ['/api/clientes','/api/categorias','/api/productos']:
    assert isinstance(req('GET',path,token=cajero),list)
for path in ['/api/inventario/entradas','/api/inventario/movimientos','/api/inventario/resurtido','/api/respaldos','/api/dashboard/estadisticas']:
    req('GET',path,token=cajero,expect=403)
req('POST','/api/clientes',{'nombre':'No permitido'},cajero,403)
req('POST','/api/categorias',{'nombre':'No permitido'},cajero,403)
err=req('POST','/api/clientes',{'nombre':'  '},admin,400);assert err['status']==400 and 'detail' in err
cliente=req('POST','/api/clientes',{'nombre':'Cliente API','telefono':'555'},admin,201)
req('PUT',f"/api/clientes/{cliente['id']}",{'nombre':'Editado API','telefono':''},admin)
assert req('GET',f"/api/clientes/{cliente['id']}",token=admin)['telefono']==''
req('DELETE',f"/api/clientes/{cliente['id']}",token=admin,expect=204)
categoria=req('POST','/api/categorias',{'nombre':'Categoría API'},admin,201)
req('PUT',f"/api/categorias/{categoria['id']}",{'descripcion':'Actualizada'},admin)
req('DELETE','/api/categorias/1',token=admin,expect=400)
req('DELETE',f"/api/categorias/{categoria['id']}",token=admin,expect=204)
ok('Catálogos comparten permisos, validación y bajas lógicas')
entrada={'operacionId':str(uuid.uuid4()),'proveedor':'Prueba API','detalles':[{'productoId':101,'cantidad':1,'porCaja':True,'piezasPorCaja':12,'costoUnitario':2,'stockEsperado':10}]}
primera=req('POST','/api/inventario/entradas',entrada,admin)
segunda=req('POST','/api/inventario/entradas',entrada,admin);assert primera['id']==segunda['id']
assert req('GET','/api/productos/101',token=admin)['stock']==22
req('POST','/api/inventario/operaciones',{'productoId':101,'cantidad':-23,'motivo':'Prueba sin stock'},admin,400)
oper={'operacionId':str(uuid.uuid4()),'productoId':101,'tipo':'Merma','cantidad':1,'motivo':'Producto dañado','stockEsperado':22}
mov=req('POST','/api/inventario/operaciones',oper,admin)
assert req('POST','/api/inventario/operaciones',oper,admin)['id']==mov['id']
req('POST',f"/api/inventario/movimientos/{mov['id']}/correccion",{'operacionId':str(uuid.uuid4()),'motivo':'Corrección comprobada'},admin)
assert req('GET','/api/productos/101',token=admin)['stock']==22
ok('Entradas, operaciones y correcciones preservan idempotencia y existencias')
conteo=req('POST','/api/inventario/conteos',{'productos':[101],'operacionId':str(uuid.uuid4())},cajero,201)
otro=req('POST','/api/inventario/conteos',{'productos':[102],'operacionId':str(uuid.uuid4())},admin,201)
req('GET',f"/api/inventario/conteos/{otro['id']}",token=cajero,expect=403)
conteo['detalles'][0]['contada']=21;conteo['detalles'][0]['motivo']='Conteo real de prueba'
conteo=req('PUT',f"/api/inventario/conteos/{conteo['id']}",conteo,cajero)
req('POST',f"/api/inventario/conteos/{conteo['id']}/confirmacion",{'version':conteo['version']},cajero,403)
conteo=req('POST',f"/api/inventario/conteos/{conteo['id']}/confirmacion",{'version':conteo['version']},admin)
req('POST',f"/api/inventario/conteos/{conteo['id']}/confirmacion",{'version':conteo['version']},admin)
assert req('GET','/api/productos/101',token=admin)['stock']==21
req('POST',f"/api/inventario/conteos/{otro['id']}/actualizar-base",{'version':otro['version']},admin)
assert len(req('GET','/api/inventario/conteos',token=cajero))==1
ok('Cajero captura sus conteos y solo un responsable autorizado aplica diferencias')
for path in ['/api/inventario/movimientos?Pagina=1','/api/inventario/resurtido','/api/inventario/reporte?desde=2026-01-01&hasta=2026-12-31','/api/dashboard/estadisticas','/api/dashboard/ventas-por-dia','/api/dashboard/ventas-por-metodo-pago']:
    req('GET',path,token=admin)
req('GET','/api/inventario/reporte',token=admin,expect=400)
req('GET','/api/dashboard/ventas-por-dia?dias=-1',token=admin,expect=400)
respaldo=req('POST','/api/respaldos',token=admin)['nombre']
verificacion=req('POST','/api/respaldos/'+respaldo+'/verificacion',token=admin)
assert verificacion['productos']==2
ok('Reportes y respaldo recuperable funcionan desde HTTP')

req('PATCH','/api/productos/101/stock?cantidad=1',token=admin,expect=409)
req('POST','/api/ventas',{'operacionId':str(uuid.uuid4()),'detalles':[{'productoId':101,'cantidad':.5,'precioUnitario':5}]},cajero,400)
venta={'operacionId':str(uuid.uuid4()),'usuarioId':999,'clienteId':1,'recibido':10,'detalles':[{'productoId':101,'cantidad':1,'precioUnitario':5}]}
v1=req('POST','/api/ventas',venta,cajero,201)
v2=req('POST','/api/ventas',venta,cajero,201)
assert v1['id']==v2['id'] and req('GET','/api/productos/101',token=admin)['stock']==20
assert v1['recibido']==10 and v1['cambio']==5 and v2['cambio']==5
ok('Venta HTTP conserva responsable real, valida cantidades y no duplica reintentos')
import base64
def subir(datos,tipo,expect=200):
    limite='abaPrueba'+uuid.uuid4().hex
    cuerpo=(f'--{limite}\r\nContent-Disposition: form-data; name="archivo"; filename="prueba.png"\r\nContent-Type: {tipo}\r\n\r\n').encode()+datos+f'\r\n--{limite}--\r\n'.encode()
    r=urllib.request.Request(base+'/api/productos/imagenes',data=cuerpo,method='POST',headers={'Authorization':'Bearer '+admin,'Content-Type':'multipart/form-data; boundary='+limite})
    try:
        with urllib.request.urlopen(r,timeout=20) as response:status=response.status;raw=response.read()
    except urllib.error.HTTPError as e:status=e.code;raw=e.read()
    assert status==expect,(status,raw[:300])
    return json.loads(raw)
subir(b'<script>prueba</script>','image/png',400)
imagen=subir(base64.b64decode('iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVQIHWP4z8DwHwAFgAI/ScLbtAAAAABJRU5ErkJggg=='),'image/png')
assert imagen['url'].endswith('.png')
ok('Carga multipart valida la imagen y devuelve una URL reutilizable')

req('GET','/health')
print('Pruebas HTTP terminadas con datos ficticios.')
