# Venta táctil

La pantalla `/ventas/nuevo` comparte el marco de Inicio. La ampliación del recibo agrega Recibido y Cambio a Ventas mediante migración; las ventas anteriores conservan ambos campos nulos.

- Buscador único: escribir filtra por nombre o código; Enter agrega únicamente una coincidencia exacta de código. Conserva ceros iniciales.
- La pistola también funciona fuera del buscador dentro de Nueva venta. Se reconoce una secuencia rápida de al menos tres caracteres terminada en Enter. No captura dentro de otros campos, herramientas o diálogos. La distinción usa el ritmo de escritura; si el lector permite prefijo dedicado, puede configurarse después para mayor certeza.
- Todos: frecuencia en tickets distintos completados durante los últimos 30 días, sin comparar kilos con piezas. Categorías: orden español A-Z. Empates alfabéticos y agotados al final. El orden no cambia al agregar al carrito.
- Favoritos: estrella para añadir o retirar; ordenar con subir/bajar. Se guarda por usuario en el navegador.
- Catálogo con desplazamiento y lotes de 24 tarjetas, carga al acercarse al final y botón de respaldo. Conserva la posición al agregar. Cambiar búsqueda o categoría vuelve arriba.
- Divisor táctil: arrastrar, flechas del teclado o doble clic/doble toque para restablecer 60 %. Anchos mínimos de catálogo y carrito; guarda la proporción en el equipo. En espacios estrechos los paneles se apilan.
- Carrito: controles de 48 px; piezas enteras, peso hasta tres decimales y cantidades positivas; las existencias no limitan la venta. Editar peso sustituye la cantidad; agregar desde catálogo la suma.
- Elegir un producto por peso abre Báscula/Manual. Manual enfoca cantidad y abre el teclado numérico existente. Cerrar la ventana cierra solo el teclado abierto automáticamente por esa ventana.
- Código de producto por peso: agrega directamente solo con tres muestras consecutivas recientes y estables. Se estiman estables si difieren como máximo 0.002 en la unidad configurada; caducan después de cuatro segundos. Las tramas US y las lecturas no positivas no son válidas. Si falta peso estable, abre captura. No se añaden conversiones entre unidades distintas; producto y báscula deben usar la misma unidad.
- Cancelar venta confirma cuando hay productos y limpia solamente el borrador. No escribe existencias ni crea una venta cancelada.
- Efectivo conserva el registro de venta existente. Recibido y Cambio se muestran en la ventana de cobro; Confirmar exige un importe válido que cubra el total. Ambos importes quedan guardados con la venta para consultarlos en su detalle; el cambio lo calcula el servicio. Esto no sustituye un módulo de conciliación de pagos. Tarjeta, Transferencia y Crédito muestran el diseño y bloquean Cobrar; sus reglas y contabilización se implementarán en la siguiente etapa.

## Organización

`Components/Ventas` contiene catálogo y filas; `Views/Ventas/Nuevo` coordina la pantalla. `Presentation/Ventas` contiene carrito, orden y validación de peso. `CatalogoVentaService` consulta únicamente frecuencia por producto con autorización de Admin/Supervisor/Cajero. `venta-ui.mjs` controla divisor, captura rápida, foco y desplazamiento; `venta.css` contiene estilos limitados a esta pantalla.

## Comprobaciones

- Compilación .NET sin errores ni advertencias.
- `node tests/venta-ui.test.mjs` y `node tests/touch-tools.test.mjs`.
- `dotnet run --project tests/InventarioChecks/Checks.csproj --no-restore -p:NuGetAudit=false` usa bases temporales: carrito, permisos, movimientos, idempotencia, ranking, orden y lecturas de peso.
- `python tests/arquitectura.py PuntoAba/PuntoAba`.
- Navegador: escritorio y 1024 x 768, código exacto, captura rápida sin foco en buscador, 0.250 kg con teclado táctil, edición de peso, rechazo de cero piezas, favoritos y su orden, cancelación, resumen visible, Crédito deshabilitado y ajuste por teclado del divisor.
- No se confirmó ninguna venta ni se alteró el inventario real durante las pruebas de interfaz. La báscula física no estaba conectada: el peso estable automático se comprobó con muestras de prueba; queda pendiente probar el hardware real.

Respaldo previo de código y base: carpeta `Respaldos`, archivos `antes-rediseno-ventas-20261017-212452.*`.


## Cinco ventas en curso

La fila centrada Venta 1 a Venta 5 permite interrumpir un pedido y atender otro. Cada borrador conserva carrito, cantidades por pieza/peso, cliente, descuento, impuesto, método, recibido e identificador de operación. Las pestañas muestran cuántos productos distintos hay y su importe. Se pueden tocar o recorrer con flechas, Inicio y Fin del teclado.

`VentasEnCurso` es un servicio por circuito y usuario; `BorradorVenta` y `CarritoVenta` son independientes de Blazor y no escriben existencias. Cambiar de pantalla conserva los pedidos. La copia en sessionStorage está separada por usuario y pestaña del navegador; recupera los pedidos al recargar esa misma pestaña. Cerrar definitivamente la pestaña termina esa copia de sesión: no es todavía un archivo permanente de pedidos pendientes.

La recuperación usa identificadores de producto y cliente; los datos de producto se obtienen del catálogo actual. No restaura precios enviados por el navegador. Si las existencias bajaron, conserva la cantidad del pedido y permite cobrar; el saldo puede quedar negativo. Los productos que ya no están disponibles generan un aviso al recuperar. Los borradores no reservan mercancía: el registro valida productos activos y cantidades, y descuenta las existencias de forma transaccional.

Cancelar y finalizar limpian solo la venta activa y renuevan su identificador de operación. Cambiar pestaña está bloqueado durante el procesamiento o con una ventana de captura abierta. Se cierra el teclado al cambiar de pedido y los campos editables quedan vinculados al borrador correspondiente.

Descuento y Recibido comienzan vacíos, muestran su nombre dentro del campo y no tienen flechas de incremento. Cambio usa un importe de 27 px; el total del resumen, 20 px; Cobrar, 30 px. En 1024 x 768 el resumen se organiza en dos filas para mantener una línea completa del carrito accesible.

Pruebas adicionales: independencia de cinco borradores, recuperación JSON, permisos entre usuarios, cancelación independiente, conservación del identificador para reintentos y revisión del stock. En navegador se verificaron las cinco pestañas, cambio con teclado abierto, cantidades por peso, descuento/recibido separados, recarga, salida y regreso desde Inicio, cancelación selectiva y tamaños en 1024 x 768. No se confirmaron ventas en la base del usuario.

Respaldo antes de esta ampliación: `Respaldos/antes-pestanas-venta-20261017-223422.*`.


## Aviso de venta confirmada

Después de que CrearAsync devuelve una venta Completada con identificador válido, la pestaña de ese pedido toma color verde durante 10 segundos y suena un tono sinusoidal breve de volumen bajo. La confirmación usa el índice del pedido que se cobró y no se repite para el mismo identificador de venta. El texto con folio e importe permanece como comprobación visible.

El audio se prepara al tocar Cobrar o activarlo con teclado, sin emitir sonido en ese momento. Un intento fallido no dispara confirmación. Si el navegador bloquea el sonido o falla la animación, no cambia el resultado de la venta ni se registra de nuevo. Al salir de la pantalla se liberan el contexto de audio y las animaciones.

Pruebas: `node tests/confirmacion-venta.test.mjs`; compilación; venta completa en instancia separada de localhost:5004 con base `work/aviso-venta-fixture/api.db`. Las pruebas aisladas también verifican que cantidades inválidas e inactivos revierten la operación. La base del usuario no se usó para simular cobros.


## Ventas sin bloqueo por existencias

Stock bajo se muestra en ámbar cuando el saldo es positivo y está en el mínimo o por debajo. Sin stock se muestra en rojo con saldo cero o negativo. Las tarjetas siguen disponibles para agregar; catálogo, pistola, cantidades del carrito, peso manual y peso de báscula permiten superar las existencias registradas.

La venta descuenta la cantidad completa incluso con saldo ya negativo. Cada salida registra saldo anterior y nuevo, responsable, costo y referencia de venta. Los reintentos conservan la idempotencia. Se mantienen cantidades positivas, piezas enteras, tres decimales para peso, permisos, productos activos y transacción con control de concurrencia. No se cambian las reglas de creación de stock inicial o de ajustes manuales.

Los filtros Sin stock de Productos e Inventario incluyen saldos negativos. Al editar datos de un producto, su stock real se muestra como lectura separada del stock inicial de creación: así un saldo negativo no impide guardar nombre, precio u otros datos. Las existencias se corrigen con Entradas, Ajustes o Conteo físico y su responsable.

Pruebas: `StockNegativoChecks` comprueba venta desde cero, venta desde negativo, kilos superiores al saldo, líneas repetidas, reintento, rechazo de cantidades inválidas e inactivos, rollback y corrección posterior con conteo. En navegador de la instancia aislada se verificaron etiquetas, venta de 4 piezas desde 0 (saldo -4), venta de 6 kg desde 5 (saldo -1), filtro Sin stock y edición de metadatos sin modificar esos saldos.

Respaldo previo de código y base: `Respaldos/antes-venta-stock-negativo-20261017-235442.*`.


## Cobro en ventana

Cobrar abre una ventana con Total, Recibido, Cambio, Volver y Confirmar. Recibido recibe el foco y abre el teclado numérico automáticamente. El carrito ya no muestra esos dos campos. Confirmar exige efectivo capturado, no negativo, con hasta dos decimales y suficiente para cubrir el total redondeado a centavos con MidpointRounding.AwayFromZero, consistente con el importe mostrado. La misma validación se aplica antes de invocar el servicio de venta.

Volver, cerrar o Escape conservan el pedido y cierran el teclado. Mientras se cobra, no se puede cambiar de pedido; durante el guardado se bloquea la confirmación repetida. Un error conserva la ventana y el carrito, con aviso visible. Solo una venta completada cierra la ventana, limpia su pedido y dispara el aviso existente de sonido y verde de diez segundos.

Verificación: compilación sin errores ni advertencias, pruebas C# de importes vacíos, insuficientes, negativos, decimales y redondeo de peso; pruebas Node de interfaz, herramientas y confirmación. En navegador a 1024 x 768 se comprobó el foco, teclado automático, controles visibles, bloqueo por importe vacío/insuficiente y Volver conservando el pedido. Una venta ficticia de $5 con $20 recibidos mostró $15 de cambio y se registró únicamente al confirmar en la base aislada de localhost:5004. No se simularon ventas en la base del negocio.

Respaldo previo: `Respaldos/antes-ventana-cobro-20261018-001007.*`.


## Uso de Enter en Nueva venta

- Buscador: Enter agrega un código exacto; si está vacío y hay un pedido válido en efectivo, abre Cobrar.
- Recibido: Enter activa Confirmar únicamente si está habilitado. Se conservan las validaciones de importe en el servidor y el bloqueo durante el guardado.
- Cantidad por peso: Enter agrega o guarda una cantidad válida.
- Buscar cliente: Enter selecciona solo cuando hay exactamente un resultado; con varios resultados se usa Tab y Enter sobre el cliente deseado.
- Descuento e impuesto: Enter cierra el teclado numérico y lleva el foco a Cobrar; otro Enter abre la ventana.
- Botones: conservan Enter y Espacio. Tab recorre los controles y Escape cierra las ventanas sin perder el pedido. La cancelación requiere seleccionar su botón explícitamente.

Se ignoran repeticiones por mantener Enter presionado. Las combinaciones con modificadores y la composición de texto no activan confirmaciones de campo. En campos que confirman, una ráfaga de al menos seis caracteres separados por hasta 80 ms se trata como posible lectura de pistola y consume su Enter sin confirmar. Es una protección por ritmo, no una identificación física del dispositivo; si un importe largo se escribe a esa velocidad, basta revisar el monto y pulsar Enter nuevamente. El lector del buscador conserva su comportamiento existente.

Tras cobrar, el foco vuelve al buscador. Pruebas de navegador en la base aislada: código más Enter, buscador vacío más Enter, rechazo de recibido vacío e insuficiente, cobro con $20 para total $5 y cambio $15, peso manual 0.250 kg, cliente único, avance desde descuento, Escape y ráfaga de código sin cobro. Solo la venta ficticia confirmada se registró; no se usó la base del negocio para simular cobros.

Respaldo previo: `Respaldos/antes-enter-ventas-20261018-003903.zip`.


## Recibo con efectivo guardado

La migración CobroEfectivoRecibo agrega únicamente Recibido y Cambio, ambos decimales anulables, a Ventas. Los registros anteriores conservan null y el recibo muestra No registrado, sin deducir efectivo a partir del total. CrearVentaDto incorpora Recibido: en toda nueva venta en efectivo es obligatorio, debe cubrir el total mostrado en centavos, ser no negativo, admitir hasta dos decimales y caber en decimal(18,2). Cambio se calcula en el servicio, dentro de la misma transacción que registra venta y salida de inventario. La interfaz envía lo capturado en el modal. Un reintento con la misma operación devuelve el efectivo y cambio originales, aunque el nuevo intento mande otro importe.

VentaDto y la sincronización incluyen ambos campos y las notas. El detalle muestra negocio, folio, fecha y hora, cliente, vendedor, método, estado, productos con código/unidad, cantidad, precio unitario, descuento por renglón cuando exista, importes, subtotal, descuento general, impuesto, total, recibido, cambio y notas cuando existan. Los métodos distintos de Efectivo no muestran efectivo ni cambio. Cerrar y Escape cierran el detalle. El cuerpo permite desplazamiento y la tabla desplazamiento horizontal cuando haga falta.

Pruebas: ReciboVentaChecks migra una base anterior con una venta existente, comprueba persistencia al reabrir el contexto, efectivo obligatorio, rechazo de negativos/insuficientes/más de dos decimales/fuera de rango sin alterar inventario, redondeo de medio centavo para peso, cero explícito e idempotencia. Suite C# completa, compilación sin errores ni advertencias y control de arquitectura correctos. En navegador se registró una venta ficticia de $5 con $20 recibidos y $15 de cambio exclusivamente en la base aislada, y se abrió su detalle desde Historial. Se comprobó el aviso de venta anterior sin datos y el cierre por Escape. A 1024 x 768 el cuerpo tiene desplazamiento y el botón Cerrar permanece visible, con altura táctil de 48 px.

Respaldo de código y base previo a la migración: `Respaldos/antes-recibo-completo-20261018-011348.*`.
