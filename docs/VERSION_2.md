# PuntoAba 2.0.0

Publicación de las fuentes locales actualizadas del 18 de octubre de 2026, preparada el 20 de octubre de 2026.

## Procedencia y conservación

- Origen local: `Desktop/PROYECTOS DE PROGRAMACION/PuntoAba`, con cambios sin confirmar sobre `8b9ca48e2f8bc4dfc4560159051fc18a0ed3cc73`.
- Se comprobó que los archivos de código y documentación de la copia de trabajo del 18 de octubre coinciden con las fuentes del escritorio.
- La rama `v2` parte del mismo commit que `main`. La publicación agrega un commit y una etiqueta; no modifica `main`, no elimina archivos anteriores ni reescribe el historial.
- La copia original del escritorio no se modifica.

## Cambios incluidos

Inventario con entradas, conteos, mermas, devoluciones, correcciones, resurtido y auditoría; permisos por rol; servicios y contratos compartidos entre Blazor y API; ventas táctiles, captura de códigos, recibo de efectivo, cambio y sincronización; respaldos y validación de imágenes.

## Preparación de seguridad

- Se excluyen `.env`, bases de datos, respaldos, registros, configuraciones privadas, archivos de usuario y resultados de compilación.
- No se agregan binarios ni imágenes cargadas por usuarios. Se conservan los recursos que ya estaban versionados.
- Se elimina la clave JWT predeterminada y la contraseña inicial pública. Configurar `Jwt__Key` y, para inicializar el administrador en desarrollo, `Bootstrap__AdminPassword`. Ver README.
- Se retiran las cadenas de conexión de los archivos de configuración publicados. El valor SQLite predeterminado del código solo identifica un archivo local; una conexión propia se configura externamente.
- Las credenciales de los tests son ficticias y se usan exclusivamente en bases temporales de prueba.
- La revisión de patrones de secretos y de los archivos preparados no detectó credenciales privadas. No es una certificación exhaustiva de seguridad. Los ejemplos de configuración de commits anteriores permanecen en el historial conservado.

## Verificación

- Compilación Release: correcta, sin errores.
- 69 comprobaciones de servicios, inventario, permisos, migraciones, cobro y sincronización: correctas.
- Tres archivos de pruebas JavaScript: correctos.
- Comprobación de separación entre vistas y persistencia: correcta.
- Pruebas HTTP sobre SQLite aislada: catálogos, permisos, entradas, conteos, reportes, respaldos, ventas idempotentes y carga de imágenes correctos.
- `/health` informa `2.0.0`; login y recursos web accesibles.
- El test HTTP de venta se actualiza para enviar `recibido` y comprobar el cambio según el contrato actual.

No se probaron físicamente balanza, lector ni impresora. La consulta automática de vulnerabilidades de paquetes devolvió NU1900 por falta de acceso a NuGet; la restauración y compilación usaron los paquetes disponibles. No se certifica despliegue de producción.
