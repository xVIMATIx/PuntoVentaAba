# Cintas de módulos y pantallas pendientes

Se agregó MenuModulo como cinta compartida de opciones para Historial, Clientes y Administración. Solo muestra las pantallas existentes y permitidas para el usuario. La cinta de Inventario conserva su implementación y su actualización de estado activo. Historial mantiene cuatro opciones: Ventas realizadas, Movimientos y correcciones, Reportes de ventas y Reportes de inventario. Su acceso lateral permanece activo dentro de las cuatro pantallas. Usuarios, Respaldos y Sincronización conservan sus accesos laterales provisionales y comparten la cinta de Administración. Inicio sigue usando los accesos de los cuatro módulos principales.

El estilo común en modulos.css aplica tarjetas, tablas, controles de 48–52 px, formularios, estados vacíos, mensajes y ventanas al resto de módulos, separado del diseño ya establecido de Inventario y Nueva venta. Los menús se desplazan horizontalmente cuando falta ancho. Nueva venta conserva sus cinco pedidos, catálogo, carrito, lector, cobro y herramientas táctiles sin cinta de opciones adicional.

Cambios funcionales:

- Clientes: búsqueda por nombre, correo, documento o teléfono actualizada por oninput, compatible con teclado táctil; formularios con etiquetas y límites, errores dentro de la ventana, creación/edición separadas y bloqueo al guardar. La desactivación existente se explica y conserva ventas; solo se ofrece al administrador, excluyendo el cliente general.
- Usuarios: la edición usa ActualizarUsuarioAsync con el ID original. Antes llamaba a CrearUsuarioAsync también al editar. El campo de usuario nuevo permanece editable mientras se escribe; el estado de edición se decide por ID, no por el texto capturado. Se conservan credenciales al editar y se restringen consultas/guardado al administrador desde la pantalla. No se modificaron reglas de roles ni contraseñas existentes.
- Ventas realizadas y reportes: filtros claros, validación del periodo, mensajes de fallo, estados vacíos y enlaces al recibo. Se conservaron los datos de recibido y cambio del detalle de venta. Reportes de inventario distingue el periodo de movimientos de los saldos actuales.
- Movimientos: corrección en ventana con motivo obligatorio, explicación del cambio inverso y éxito visible; mantiene servicio, transacción e identificador de reintento existentes.
- Respaldos: resumen de disponibilidad y fecha, carpeta plegable, actualización de lista y mensajes de comprobación; la comprobación sigue usando copia temporal sin reemplazar la base de trabajo.
- Sincronización: estado y resultado dentro de la pantalla, con bloqueo de repetición y recuperación de fallos de actualización. Se corrigió ObtenerDatosParaSincronizarAsync: no incluía las ventas pendientes en el paquete del proceso existente. Ahora incluye las pendientes independientemente de la fecha de la última sincronización, conservando recibo y detalles, y el proceso reconoce los folios existentes sin volver a descontar existencias. El contador se etiqueta Ventas pendientes.

Verificación:

- Compilación y suite InventarioChecks correctas; incluye prueba nueva SincronizacionChecks de pendientes antiguos, conservación de recibido/cambio, cola vacía e idempotencia sin duplicar venta, movimientos o saldos.
- Pruebas de JavaScript de herramientas táctiles, captura de códigos, divisor y confirmación de venta correctas. Arquitectura y diff correctos.
- En localhost:5004 se recorrieron las cuatro opciones de Historial y se comprobó el estado activo de cinta y lateral. Se consultó un recibo con recibido 20 y cambio 15, se rechazó un intervalo invertido y se consultaron ambos reportes.
- En la base aislada se registró una corrección vinculada al movimiento 14 (17 → 19), se creó y editó un cliente manteniendo dos clientes en total, y se editó el nombre del usuario de prueba manteniendo exactamente dos usuarios y su rol Cajero. Se comprobó que el campo de nuevo usuario no se bloquee al escribir; no se crearon ni cambiaron contraseñas en el navegador.
- Respaldo creado y comprobado en carpeta de pruebas independiente: 4 productos, 6 ventas, 15 movimientos y 4 imágenes. No se reemplazó ninguna base de trabajo.
- El proceso de sincronización de la base aislada pasó de seis ventas pendientes a cero; la prueba automática verifica que stock y recibos no cambien por sincronizar otra vez.
- En 1024 × 768 no hubo desbordamiento horizontal en Nueva venta. Con teclado abierto el botón Cobrar terminó en y=313 y el teclado empezó en y=365, quedando accesible. Se restableció el tamaño del navegador.

No se registraron clientes, usuarios, correcciones ni sincronizaciones de prueba en la base del negocio. No se cambiaron tablas ni migraciones. El servidor principal se deja en localhost:5000.

Respaldos de código: Respaldos/antes-cintas-y-pantallas-20261018.zip y Respaldos/antes-cola-ventas-sync-20261018.zip.

## Diseño uniforme con Inventario

MenuModulo usa exactamente las mismas reglas de MenuInventario, cambiando únicamente los nombres de las clases. Conserva fondo blanco, separación de 6 px, opciones de 52 px, iconos simples y selección azul clara; se retiraron los fondos adicionales y los marcos de los iconos. No hay cinta ni contenedor adicional en Nueva venta, que conserva sus cinco pestañas de pedidos. Se mantiene la URL versionada del CSS aislado para renovar estilos.

Verificación: compilación sin errores ni advertencias. Se compararon en navegador los estilos calculados de Inventario e Historial: relleno, separación, bordes, colores, tamaño de letra, peso y altura mínima resultaron idénticos en la franja, la opción activa y la inactiva. Ventas muestra cero cintas y conserva sus cinco pestañas. No cambiaron servicios, permisos, datos ni flujos de venta. Respaldo: Respaldos/antes-cintas-iguales-inventario-20261018.zip.
