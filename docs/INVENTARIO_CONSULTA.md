# Consulta de inventario

El acceso principal y el encabezado superior se llaman Inventario. La consulta usa el título Consultar inventario. MenuInventario comparte una franja horizontal de accesos entre las rutas del grupo Inventario: consulta, productos, categorías, entradas, resurtido, conteo físico, ajustes y mermas/devoluciones. Utiliza el catálogo de navegación y sus permisos existentes. Los enlaces marcan la ruta activa y permiten navegación nativa por teclado. El menú lateral conserva seleccionado Inventario al cambiar entre estas pantallas; la franja no se muestra en Historial, reportes, respaldos ni otras áreas.

La consulta tiene cuatro resúmenes táctiles: productos activos, existencia normal, stock bajo y sin stock. Los últimos tres grupos son excluyentes: normal exige saldo positivo por encima del mínimo, bajo exige saldo positivo menor o igual al mínimo, y sin stock incluye cero y negativos. Cada resumen aplica su filtro. Los enlaces de Inicio con estado=bajo o estado=agotados siguen funcionando.

La tabla muestra todos los productos activos por defecto, ordenados alfabéticamente en español. Permite buscar por nombre o código, filtrar por categoría y existencias, limpiar búsqueda/categoría y actualizar desde el servicio. Muestra producto, código, categoría, saldo, mínimo, unidad, precio de venta y estado. Los productos por peso muestran tres decimales; las piezas, enteros. Carga 50 filas y permite mostrar otras 50, manteniendo los filtros. Un fallo al actualizar conserva los datos anteriores con un aviso; no cambia existencias.

El diseño usa fondos claros, bordes discretos, iconos SVG existentes y avisos ámbar/rojo. La franja tiene botones de 52 px y desplazamiento horizontal; la búsqueda tiene 48 px. En tamaños pequeños los resúmenes se distribuyen en dos columnas y la tabla conserva su propio desplazamiento. El panel lateral y las herramientas táctiles permanecen disponibles.

Verificación: compilación sin errores ni advertencias; control de arquitectura; diff sin errores de formato. En navegador se comprobaron búsqueda por código, categoría, estado, filtros combinados, resultados vacíos, restauración de filtros y navegación a Productos, Mermas e Historial con selección correcta. A 1024 x 768 se verificó que la página no desborda horizontalmente, los accesos tienen 52 px y el contenido se puede desplazar con el teclado táctil abierto. Las comprobaciones de interfaz fueron de consulta y navegación; no se modificaron productos ni existencias. Sin cambios de esquema de base para este rediseño.

Respaldo previo del código: Respaldos/antes-diseno-inventario-20261018-013413.zip.


## Resto de las pantallas del módulo

El contenedor inv-workspace limita el nuevo estilo a las ocho rutas de Inventario. Inventario.css unifica fondos claros, tarjetas, tablas, colores de estado, campos de 50 px y acciones principales de 48 px, con desplazamiento de tablas y rejillas adaptables. Los diálogos del catálogo conservan Cancelar y Guardar fuera del cuerpo desplazable, incluso con el teclado táctil abierto. El enlace CSS usa versión de contenido para evitar conservar una versión antigua en caché.

- Productos: resumen de activos/bajo/sin stock, filtros accesibles, búsqueda inmediata por nombre o código, catálogo ordenado, estado vacío y acciones con texto e iconos. El formulario organiza imagen/código/categoría y agrupa precios y existencias en dos columnas. El stock de edición continúa de solo lectura. Guardar y eliminar notifican en la pantalla. Las confirmaciones de eliminación y las validaciones existentes se conservan.
- Categorías: tarjetas con descripción y cantidad de productos, búsqueda, contadores, acciones legibles, estado vacío y un formulario compacto con error visible dentro del diálogo. Las categorías con productos conservan el bloqueo de eliminación.
- Entradas: resumen del borrador y recepciones recientes, captura por código o catálogo separada de la revisión, metadatos, cantidades, costos y total. El Enter del escáner agrega una línea y no confirma la recepción.
- Resurtido: filtros agrupados, resumen uniforme y encabezado de sugerencias con selección y cantidades editables. Se mantiene el traslado del borrador a Entradas y se aclara cuando los filtros no devuelven productos.
- Conteos: resumen de seleccionados y registros pendientes, selección en filas con controles amplios, historial con estado vacío y formulario de comparación existente. No cambia autorización ni aplicación de diferencias.
- Ajustes y Mermas/devoluciones: formulario común con vista previa de existencias y una guía contextual que diferencia cada operación. En pantallas estrechas la guía pasa debajo del formulario. Las confirmaciones y el registro de movimientos se mantienen en los servicios existentes.

Verificación: compilación sin errores ni advertencias, arquitectura y diff correctos. En navegador se revisaron las siete pantallas, búsqueda de productos por código, búsqueda/edición de categoría sin guardar, agregado y retiro de un renglón de recepción, selección y retiro para conteo sin iniciarlo, sugerencias de resurtido y vista previa de merma/devolución (10 pasa a 8 al retirar 2, a 12 al devolver 2 vendibles y permanece en 10 para no vendibles). El botón reubicado Guardar producto conserva la validación de código, nombre y categoría obligatorios. En 1024 x 768 se verificaron campos de 50 px, acciones de 48 px, tablas con desplazamiento y ausencia de desbordamiento de página. Con el teclado abierto, Guardar y Cancelar permanecieron por encima de él. No se confirmaron entradas, conteos, ajustes, mermas, devoluciones ni cambios de catálogo durante estas pruebas.

Respaldo previo: Respaldos/antes-resto-inventario-20261018-014138.zip. No se modificaron servicios, permisos ni esquema de base en esta ampliación.


## Captura rápida de productos

Nuevo producto organiza código y nombre arriba, Datos de venta (categoría y precios) a la izquierda y Forma de venta/existencias a la derecha. Por pieza/Por peso son botones grandes; cambiar el tipo propone Pieza/Kg, con unidad editable. Descripción, foto, proveedor y objetivo de resurtido quedan en Más opciones, conservando los valores y el selector de archivo montados al plegar. Los precios empiezan vacíos en alta: se exige capturarlos, permitiendo cero explícito según las reglas existentes.

El código recibe el foco al abrir y al continuar con otro producto. Enter en código avanza al nombre; Enter en otros campos marcados avanza sin guardar, y Tab mantiene su función normal. El envío implícito del formulario no guarda. Escape y Cancelar cierran cuando no hay guardado en curso. Guardar valida y cierra; Guardar y agregar otro solo aparece al crear, conserva la categoría y limpia el resto del borrador después de confirmar la creación. Un fallo conserva los datos. La recarga del catálogo tiene un aviso diferenciado si falla después de que el producto ya quedó guardado, para no inducir un segundo guardado. En edición se precargan los precios y las existencias continúan de solo lectura.

La ventana adapta el ancho y sus columnas al espacio disponible; el cuerpo es desplazable y sus botones quedan fuera del área de campos. En 1024 x 768 con teclado abierto se verificó que los tres botones permanecen por encima del teclado. A 600 x 900 el formulario usa una columna y no desborda la página.

Pruebas en localhost:5004 con base aislada: alta de TEST-RAPIDO-01 por pieza con compra 10, venta 15 y saldo 0; Guardar y agregar otro conserva categoría y deja código/precios vacíos con foco en código; rechazo de código duplicado conservando todos los datos; alta de TEST-RAPIDO-02 por peso con compra 5, venta 8, saldo 1.250 kg y proveedor capturado en opciones plegables; edición precarga precio/saldo y no permite editar existencias. Una consulta independiente a SQLite confirmó los valores y que no se duplicó el primer código. La base del negocio no contiene esos productos de prueba. Compilación sin errores ni advertencias, control de arquitectura y comprobación de formato correctos.

Respaldo previo: Respaldos/antes-formulario-producto-20261018-015729.zip. Archivos nuevos: Views/Productos/Index.razor.css y wwwroot/js/producto-form.mjs. No cambian servicios ni esquema de base.


## Accesos desde Categorías

Las tarjetas conservan su diseño. El contador de productos es un enlace táctil de 44 px a productos?categoriaId=ID; Productos recibe el parámetro y aplica la categoría al entrar, al cambiar el parámetro y al recargar. Retirar el parámetro restablece Todos sin dejar un filtro anterior. La búsqueda de categorías permanece visible al desplazar la lista y el orden utiliza comparación española es-MX. El bloqueo de Eliminar conserva sus reglas y muestra un motivo visible asociado al botón mediante aria-describedby. Solo Nombre está marcado como obligatorio; Descripción se identifica como opcional. Ambos respetan los límites existentes de 100/500 caracteres.

Verificación: compilación sin errores ni advertencias, arquitectura y diff correctos. En navegador se abrió Lácteos desde su contador y se obtuvieron seis productos de esa categoría; al recargar se mantuvo el filtro y al abrir Productos sin parámetro volvieron los 40 productos. La búsqueda Limpieza dejó una tarjeta con su enlace y explicación de eliminación bloqueada. Se comprobó que Nombre es obligatorio y Descripción no. No se guardaron ni eliminaron categorías o productos durante estas pruebas.

Respaldo previo: Respaldos/antes-accesos-categorias-20261018-020801.zip.


## Entradas con captura y revisión separadas

La pantalla separa Nueva recepción de Recepciones anteriores sin perder el borrador al alternar las vistas. Un buscador permite código exacto más Enter o búsqueda por nombre; los resultados son botones. Con una sola coincidencia Enter la agrega y con varias se solicita elegir, sin seleccionar automáticamente el primer resultado. El escaneo solo agrega renglones; no confirma entradas. El buscador recupera el foco después de agregar.

Cada producto usa una tarjeta con cantidad, costo por pieza o unidad de peso, importe y existencias Había/Quedarán. Recibir por cajas muestra piezas por caja y la conversión completa; el costo sigue siendo por pieza. Los productos por peso no ofrecen cajas. Proveedor, documento y comentarios están en un bloque opcional plegable. La opción de actualizar precios de compra permanece explícita y aclara que no modifica precios de venta.

El total y Revisar recepción quedan fuera del área desplazable. Volver a consultar existencias actualiza la base de comparación sin registrar entradas. Revisar valida cantidades, cajas y costos, y abre una ventana con productos, cantidades base, costos, existencias, total y efecto sobre los precios. Solo Confirmar recepción invoca el servicio existente. Volver a editar/Escape conserva el borrador. Durante la revisión se bloquea la edición; durante el guardado se bloquea la repetición. Un fallo de guardado conserva la revisión y su identificador; un fallo posterior de recarga avisa que la recepción ya quedó registrada.

El área de captura tiene desplazamiento propio. Con poco alto disponible se compacta la franja de opciones y se oculta el encabezado interior duplicado; el título superior del sistema permanece. En 1024 x 768 con el teclado abierto se comprobó que la barra termina antes del teclado y queda espacio para el campo activo.

Verificación: compilación sin errores ni advertencias, arquitectura y diff correctos. Pruebas aisladas en localhost:5004: primer código más Enter, búsqueda por nombre, cantidad cero rechazada, conservación del borrador al consultar historial y volver, conversión 2 cajas x 12 piezas, revisión sin escrituras y confirmación única. SQLite confirmó una entrada de 24 piezas, costo unitario 2, total 48 y stock de -7 a 17; el detalle conserva las 2 cajas y 12 piezas por caja. Historial mostró folio y detalle correctos. También se revisó 2.5 Kg con total 7.50 sin registrar esa segunda entrada. Ninguna prueba de recepción se confirmó en la base del negocio.

Respaldo previo: Respaldos/antes-entradas-rapidas-20261018-021549.zip. El código de la pantalla queda separado en Entradas.razor, Entradas.razor.cs y Entradas.razor.css; no cambian servicios, permisos ni esquema de base.


## Resurtido orientado a la compra

Resurtido organiza las sugerencias existentes en sin stock primero y stock bajo después; dentro de stock bajo usa la proporción saldo/mínimo y orden alfabético español para priorizar sin comparar directamente kilos con piezas. Conserva la fórmula del servicio basada en mínimo y objetivo. Presenta búsqueda por nombre/código, categoría y proveedor, incluido Sin proveedor asignado. La selección táctil funciona con el botón de cada producto o tocando zonas no editables de la fila; editar cantidad no cambia la selección.

La cantidad se llama Cantidad a pedir y siempre muestra su unidad. Se valida mientras se escribe: positivos, piezas enteras y hasta tres decimales para peso. Las cantidades inválidas seleccionadas bloquean imprimir y preparar, con explicación en el renglón y el resumen. El costo usa el último precio de compra. Cambiar filtros conserva la selección global y el pie indica cuántos seleccionados quedaron fuera del filtro. Seleccionar visibles solo afecta la lista filtrada. Actualizar conserva selección y cantidades editadas para los productos que siguen en la sugerencia; los productos retirados se eliminan también de la captura auxiliar.

Imprimir lista de compra abre una vista de lo seleccionado, agrupado por proveedor, con código, cantidad/unidad, costo unitario, subtotal por proveedor y total estimado. El botón Imprimir usa el diálogo nativo del navegador; el estilo de impresión excluye menús y controles. La lista aclara que no registra compras ni modifica existencias. Preparar recepción lleva toda la selección a Entradas como borrador para revisar lo que realmente llegó, sin confirmar nada.

El área principal se desplaza y el resumen de selección/costo permanece al pie. La búsqueda se fija al desplazar la lista. En tamaños pequeños se distribuyen las filas en dos columnas y se compactan encabezados cuando aparece el teclado. Verificación en 1024 x 768: área desplazable de 416 px, barra inferior visible; con teclado, barra por encima de sus teclas y buscador de 48 px.

Pruebas aisladas: selección de dos productos por peso, cantidades 2 y 2.5, filtrado por proveedor conservando selección oculta, total 17.50, rechazo de cero, actualización conservando cambios, vista imprimible con dos grupos y subtotales 10 y 7.50, traslado a Entradas de exactamente dos renglones con costos 5 y 3. SQLite confirmó que no se registró una recepción nueva y que las existencias permanecieron iguales. Se verificó la vista y las reglas de impresión, sin enviar una impresión física. Compilación sin errores ni advertencias, arquitectura, sintaxis JavaScript y diff correctos. No cambiaron servicios, permisos ni esquema de base.

Respaldo previo: Respaldos/antes-resurtido-rapido-20261018-024711.zip. La pantalla queda separada en Resurtido.razor, Resurtido.razor.cs y Resurtido.razor.css; la apertura del diálogo de impresión está en wwwroot/js/resurtido-impresion.mjs.

## Agregar productos manualmente a Resurtido

El bloque Agregar productos aparece arriba de los renglones y permite buscar por nombre o escanear un codigo del catalogo activo, incluso con existencias suficientes. Enter agrega el codigo exacto o la unica coincidencia; con varias coincidencias se elige un resultado. La cantidad inicial es 1 y se edita en el renglon. Repetir un producto lo selecciona sin duplicarlo ni aumentar la cantidad. Al agregar se limpian filtros para mostrarlo.

Los productos con existencias suficientes muestran Agregado manualmente y no aumentan los contadores de stock bajo o sin stock. Quitar de la lista retira el agregado manual del plan sin modificar el catalogo. Actualizar lista conserva estos productos, su seleccion y cantidades dentro de la pantalla mientras sigan activos. Los agregados participan en el costo, la lista imprimible y Preparar recepcion. No se agrega persistencia del plan entre sesiones.

Validacion aislada en localhost:5004: producto con 17 piezas disponibles agregado por codigo, cantidad editada a 4, escaneo repetido sin duplicado, actualizacion conservando cantidad y seleccion, filtro de stock bajo conservando la seleccion oculta y busqueda por nombre quitando filtros. Vista imprimible: 4 piezas, costo 2, total 8. Preparar recepcion traslado exactamente ese renglon al borrador con existencias 17 y proyeccion 21; no se confirmo una recepcion. Compilacion sin errores ni advertencias. No cambian servicios ni esquema. Respaldo previo: Respaldos/antes-resurtido-manual-20261018-025424.zip.

## Conteo fisico: acciones y resultado visibles

Abrir consulta el registro por ID mediante el servicio y enfoca su detalle, en vez de asignar el objeto del historial sin llevar la vista a la captura. Preparar un nuevo conteo se pliega al mostrar uno. Un conteo confirmado explica que ya aplico diferencias y es de solo consulta; no muestra botones de edicion inactivos sin motivo.

Guardar borrador muestra el folio y los productos contados junto al detalle, sin cambiar stock. La captura de cantidad se actualiza al escribir, distingue vacio de cero, valida enteros para piezas y hasta tres decimales para peso. El EditContext explicito permite indicar cambios sin guardar y evita reemplazarlos al abrir o iniciar otro conteo. Actualizar lista conserva la captura y no actualiza la base de comparacion.

Actualizar base y volver a contar abre un aviso con Cancelar y Actualizar y borrar captura: explica que obtiene el stock actual y borra cantidades y motivos, incluidos los guardados. Cancelar conserva el borrador; confirmar reinicia la comparacion mediante el servicio existente. Solo Autorizar y aplicar diferencias modifica existencias, conserva permisos y validaciones del servicio; la pantalla revisa captura completa y motivos antes de solicitarlo.

Verificacion: compilacion sin errores ni advertencias, arquitectura y diff correctos. En localhost:5004 se guardaron 16 piezas, se recargo y reabrio el borrador recuperando el valor; se cancelo y despues se confirmo el reinicio, volviendo a Sin contar. Se rechazo 1.5 para piezas y se impidio abrir encima de cambios sin guardar. SQLite confirmo stock de prueba en 17 y cero ajustes por conteo. En localhost:5000 solo se consulto el registro confirmado y se verifico foco y explicacion de solo consulta. No se autorizaron conteos ni se alteraron existencias del negocio. Respaldo: Respaldos/antes-conteos-claros-20261018.zip. Cambios limitados a Conteos.razor, Conteos.razor.cs y Conteos.razor.css.

## Correcciones de inventario: conteo y ajuste manual

La franja de Inventario y el menú de Inicio muestran un solo acceso: Correcciones de inventario. La pantalla contiene Contar productos y Ajuste manual. Alternar entre ellas conserva las capturas durante la sesión de la pantalla. La ruta nueva es inventario/correcciones; inventario/conteos e inventario/ajustes siguen funcionando como accesos anteriores. Productos abre directamente Ajuste manual con productoId. Los cajeros pueden contar productos y no ven Ajuste manual; el servicio continúa restringiendo los ajustes a Admin y Supervisor.

Ajuste manual permite elegir Sumar o Restar, capturar una cantidad positiva sin cero prellenado y revisar existencias actuales y resultantes. Aclara que la cantidad es el cambio, no el stock final. Valida piezas enteras, peso hasta tres decimales, motivo y rango de existencias antes de abrir la revisión. Revisar no guarda: Confirmar ajuste registra mediante el servicio existente. Volver a editar conserva la captura. Al guardar muestra el movimiento y los saldos, limpia cantidad/motivo y obtiene las existencias nuevas. Un fallo al refrescar después de guardar informa el éxito del registro para no invitar a duplicarlo. El identificador de operación se conserva al fallar y se renueva únicamente tras el éxito.

El formulario compartido de Mermas y devoluciones mantiene sus tipos y sentido de movimiento y utiliza la misma revisión explícita. No cambiaron servicios, esquema ni reglas de inventario.

Verificación: compilación sin errores ni advertencias; suite InventarioChecks completa, arquitectura y diff correctos. AjustesChecks agrega pruebas aisladas de sumas/restas de piezas y peso, auditoría, idempotencia, rechazos de cero, fracciones de pieza, exceso de decimales, saldo negativo, motivo corto, stock desactualizado y permisos. En navegador localhost:5004 se registraron +5 y -2 en Piezas API (17 → 22 → 20); SQLite confirmó exactamente ambos ajustes, cantidades, motivo y usuario. Se verificaron revisión/cancelación, conservación del formulario al cambiar de pestaña, rechazo de resta superior al saldo, alias anterior y acceso desde Productos con selección automática. Se revisó una merma de 1 (20 → 19) sin confirmarla. No se registraron ajustes de prueba en la base del negocio.

Respaldo previo: Respaldos/antes-correcciones-unificadas-20261018.zip. El formulario compartido queda separado en OperacionExistencias.razor, .razor.cs y .razor.css.

## Mermas y devoluciones por operación

La pantalla reemplaza el selector genérico por cuatro opciones táctiles: Merma, Devolución de cliente, Devolución a proveedor y Registros anteriores. Cada captura mantiene su propio borrador al alternar opciones dentro de la pantalla. La búsqueda admite nombre, coincidencia exacta de código y Enter del lector; con varias coincidencias se debe elegir un resultado.

Las mermas restan existencias. Las devoluciones de cliente exigen elegir si el producto es vendible: vendible suma, no vendible registra la cantidad sin cambiar existencias disponibles. Ambas muestran que no se procesan reembolsos ni se modifica el cobro de la venta. Devolución a proveedor exige proveedor y referencia y resta existencias; se sugiere el proveedor habitual si existe. El proveedor queda incluido en el motivo del movimiento existente, sin migraciones ni cambios de esquema. Los motivos rápidos dependen de la operación y admiten una explicación opcional; Otro exige explicación. No se selecciona automáticamente una condición ni un motivo.

La revisión incluye producto, código, unidad, cantidad, saldos antes/después, motivo y referencia/proveedor cuando corresponde. Confirmar es la única acción que registra. Cancelar conserva la captura. Cantidades positivas, piezas enteras, peso hasta tres decimales, motivos y referencias se validan antes de revisar y se mantienen las reglas, permisos, transacción y protección contra reintentos del servicio. Un fallo conserva el borrador y su identificador. Tras éxito se limpia la captura y se consulta stock nuevo; un fallo de recarga posterior informa que el movimiento sí quedó guardado.

Registros anteriores consulta exclusivamente los cuatro tipos mediante SoloMermasDevoluciones en FiltroMovimientosDto, aplicado antes del total y la paginación del servicio. El historial general conserva su comportamiento por defecto. Incluye fechas, tipo, búsqueda por producto/código/referencia, páginas y detalle con responsable, saldos y motivo. No permite borrar movimientos.

Validación: compilación sin errores ni advertencias; suite InventarioChecks completa, incluyendo MermasChecks con los cuatro sentidos de stock, reintentos sin duplicar, rechazos de cantidades/referencias/base antigua, recuperación de proveedor y referencias, filtro exclusivo y paginación sin alterar historial general. En navegador localhost:5004 se registraron merma 2 (20 → 18), devolución vendible 1 (18 → 19), no vendible 1 (19 → 19), devolución a proveedor 2 (19 → 17). SQLite confirmó exactamente cuatro movimientos con referencias y usuario. Se verificaron escaneo por Enter, búsqueda por nombre, cancelación, captura conservada entre pestañas, obligatoriedad de condición, referencia y proveedor y el detalle histórico. Las pruebas no registraron movimientos en la base del negocio.

Respaldo previo: Respaldos/antes-mermas-claras-20261018.zip. Vista, lógica y estilos quedan en Mermas.razor, Mermas.razor.cs y Mermas.razor.css; el formulario de Ajuste manual permanece separado.
