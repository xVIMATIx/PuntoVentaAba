# Separación de interfaz y lógica

ABA sigue siendo un proyecto .NET con Blazor Server, organizado por responsabilidades.
No se introducen microservicios ni una segunda base de datos.

| Ubicación | Responsabilidad |
| --- | --- |
| Views y Shared | Navegación, formularios y coordinación; sin DbContext ni escritura de archivos |
| Components/Ventas | LectorCodigo, CatalogoProductos, LineasCarrito; parámetros y eventos |
| Presentation/Ventas/CarritoVenta.cs | Estado de venta en preparación, cantidades y subtotal; no persiste |
| Views/Ventas/Nuevo.razor | Composición visual |
| Views/Ventas/Nuevo.razor.cs | Coordinación con servicios, sesión y balanza |
| Views/Ventas/Nuevo.razor.css | Estilos aislados, incluyendo componentes mediante ::deep |
| Application/Services | Casos de uso, permisos, validación y transacciones |
| Api/DTOs | Contratos compartidos por Blazor y API |
| Api/Controllers | Adaptadores HTTP |
| Infrastructure | EF Core, SQLite y migraciones |
| Domain/Models | Entidades persistentes |

Clientes, Categorías, Productos, Dashboard, Ventas e Inventario usan servicios.
Las imágenes se almacenan mediante IImagenProductoService: Stream, máximo 5 MB,
validación de firma y tipo, nombre generado. La pantalla solo captura y presenta.
Editar cliente/categoría usa un DTO separado: cancelar no modifica los datos guardados.

Blazor llama a servicios directamente porque corre en el servidor. Un front independiente
llama a controladores que usan los mismos servicios. El inicio de sesión conserva su
adaptador HTTP y almacenamiento de sesión existentes.

## API para otra interfaz

En desarrollo: /api (Swagger UI), /swagger/v1/swagger.json (contratos OpenAPI).
JSON usa camelCase. Cantidades: piezas enteras, peso hasta tres decimales.
POST /api/auth/login recibe nombreUsuario y password.
Enviar Authorization: Bearer <token> en cada petición protegida.

| Recurso | Métodos y ruta |
| --- | --- |
| Clientes | GET/POST /api/clientes; GET/PUT/DELETE /api/clientes/{id}; GET /api/clientes/buscar/{termino} |
| Categorías | GET/POST /api/categorias; GET/PUT/DELETE /api/categorias/{id} |
| Productos | GET/POST /api/productos; GET/PUT/DELETE /api/productos/{id} |
| Imágenes | POST /api/productos/imagenes; multipart, campo archivo; devuelve url |
| Ventas | GET/POST /api/ventas; GET /api/ventas/{id}; GET /api/ventas/reporte |
| Dashboard | GET /api/dashboard/estadisticas, /api/dashboard/ventas-por-dia?dias=7, /api/dashboard/ventas-por-metodo-pago |
| Recepciones | GET/POST /api/inventario/entradas |
| Movimientos | GET /api/inventario/movimientos; filtros Desde, Hasta, Tipo, Buscar, Pagina, TamanoPagina |
| Resurtido | GET /api/inventario/resurtido |
| Ajustes, mermas, devoluciones | POST /api/inventario/operaciones |
| Correcciones | POST /api/inventario/movimientos/{id}/correccion |
| Conteos | GET/POST /api/inventario/conteos; GET/PUT /api/inventario/conteos/{id} |
| Autorizar conteo | POST /api/inventario/conteos/{id}/confirmacion; cuerpo con version devuelta |
| Reiniciar comparación | POST /api/inventario/conteos/{id}/actualizar-base; cuerpo con version |
| Reporte | GET /api/inventario/reporte?desde=2026-01-01&hasta=2026-12-31 |
| Respaldos | GET/POST /api/respaldos; POST /api/respaldos/{nombre}/verificacion |

Las bajas son lógicas y requieren Admin. Crear/editar catálogos requiere Admin/Supervisor.
Cajero consulta para vender y captura solo sus propios conteos; Admin/Supervisor aplican
diferencias y reciben/ajustan mercancía. Respaldos requiere Admin.
Los servicios nuevos comprueban usuario activo y rol vigente en la base.
PATCH /api/productos/{id}/stock responde 409: usar operaciones identificadas de inventario.

Los controladores adaptados usan ProblemDetails: 400 datos inválidos, 401 sin sesión,
403 sin permiso, 404 no encontrado, 409 conflicto, 500 error interno sin detalles técnicos.
Autenticación, sincronización y balanza conservan sus respuestas heredadas.

## Reglas para el nuevo front

1. Conservar códigos como texto, incluidos ceros iniciales. Capturar Enter solo en el
   lector; los productos por peso requieren cantidad antes de agregarse.
2. Generar y conservar el mismo operacionId (UUID) en los reintentos de una venta,
   recepción, operación, inicio de conteo o corrección.
3. Revisar existencias previstas antes de confirmar. Enviar stockEsperado al recibir/
   ajustar y version en conteos. Si cambian los datos, actualizar y pedir revisión.
4. Preparar carrito o resurtido no modifica existencias. El servidor confirma la transacción.
   Devoluciones no vendibles no suman disponibles; correcciones no borran movimientos.
5. La imagen se carga primero; su url se envía al crear/editar el producto. No se borran
   imágenes anteriores automáticamente; los respaldos siguen incluyéndolas.
6. La balanza permanece en el servidor. Otra interfaz puede consultar /api/balanza/leer,
   que responde 503 si el dispositivo no está disponible.
7. React/Vue u otro cliente puede consumir la API sin trasladar la lógica de inventario.
   Aún debe implementar sus formularios, navegación y sesión. No es conversión automática.

## Comprobaciones

Con ABA detenido, desde la raíz:
- dotnet run --project tests/InventarioChecks/Checks.csproj
- python tests/arquitectura.py PuntoAba/PuntoAba

Las pruebas usan una base temporal: inventario, permisos, idempotencia, respaldos,
catálogos, carrito, imágenes y Dashboard.

Para HTTP, usar exclusivamente un entorno sintético:
1. Crear carpeta nueva: dotnet run --project tests/InventarioChecks/Checks.csproj -- --api-fixture <carpeta>
2. Ejecutar ABA en puerto 5003, conexión a <carpeta>/api.db, ApiBaseUrl http://localhost:5003,
   Respaldos:Carpeta dentro de esa carpeta y Respaldos:Automaticos false.
3. Ejecutar python tests/api_checks.py http://localhost:5003 una sola vez por fixture.
   Usa usuarios ficticios admin y cajero-api, contraseña de pruebas Pruebas123.

Esta reorganización no requiere migraciones nuevas ni cambios de la base existente.
