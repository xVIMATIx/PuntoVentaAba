# Inicio y herramientas táctiles

Inicio sustituye visualmente al Dashboard, conservando /dashboard y agregando /inicio.
La información proviene de los servicios existentes:
- Ventas de hoy y del mes: importe y cantidad.
- Agotados y stock bajo sin duplicarlos.
- Últimas cinco ventas con acceso al detalle.
- Tres clientes con más compras del mes, excluyendo Cliente General.
- Cantidad de clientes activos identificados, excluyendo Cliente General.

La tabla inferior agrupa las pantallas existentes en Ventas, Inventario, Historial y Clientes.
Los enlaces se filtran según el rol. El cajero puede abrir Inicio como portal de accesos;
el servicio de estadísticas conserva la autorización de administración y no se llama para él.

La navegación lateral contiene los cinco módulos principales. Usuarios, Respaldos y
Sincronización permanecen temporalmente en Administración. Los botones de Personalizar
Inicio y Ajustes generales se muestran sin funciones, con indicación de próxima disponibilidad.
El monograma AA reserva el espacio del logo de Abarrotes Agustín.

## Estructura compartida

MainLayout compone el menú, encabezado, área desplazable y HerramientasTactiles.
BlankLayout también incorpora herramientas para poder iniciar sesión desde pantalla táctil.
La barra inferior ocupa solamente la columna de contenido, sin cubrir el menú.

El menú se oculta después de 300000 ms sin interacción dentro del propio menú.
Pulsaciones, teclado, foco y desplazamiento del menú reinician el tiempo. Puede ocultarse
manualmente; una flecha en el lugar del logo permite desplegarlo. Se utiliza CSS y el mismo
árbol de componentes, sin recrear el formulario ni el carrito al ocultar/abrir el panel.

## Herramientas

- Teclado en español con Ñ, vocales acentuadas, mayúsculas, espacio, borrado y selección.
- Panel numérico y símbolos.
- Calculadora básica con suma, resta, multiplicación, división, porcentaje y control de errores.
- Pasar resultado al campo requiere una pulsación expresa; no registra operaciones.
- Las herramientas conservan el campo y el cursor al pulsar sus teclas. No escriben en
  campos deshabilitados, de solo lectura ni controles incompatibles.
- Siguiente recorre campos. Solo los lectores marcados y el acceso tienen envío explícito
  de formulario; no guarda productos ni procesa ventas.
- Los paneles se cierran al cambiar de pantalla; la barra queda visible. El resultado
  de calculadora se conserva mientras siga montada la sesión de la aplicación.
- Los paneles reducen el espacio de trabajo y desplazan al campo activo. Los modales
  respetan el espacio de la barra y de la herramienta desplegada.

Los módulos JS se liberan al abandonar el layout: eventos, observador de tamaño y temporizador.
No se almacenan contenidos del teclado, credenciales ni valores de campos en almacenamiento local.

## Archivos principales

- Shared/MainLayout.razor y Shared/NavMenu.razor
- Shared/HerramientasTactiles.razor y Shared/Icono.razor
- Presentation/Navegacion/MenuAplicacion.cs
- Views/Dashboard/Index.razor
- wwwroot/css/shell-inicio.css
- wwwroot/js/menu-shell.mjs, touch-tools.mjs y calculadora.mjs

## Verificaciones

Desde la raíz, con ABA detenido:
- dotnet run --project tests/InventarioChecks/Checks.csproj
- node tests/touch-tools.test.mjs
- python tests/arquitectura.py PuntoAba/PuntoAba

Las pruebas utilizan datos temporales; incluyen las operaciones anteriores, el ranking
de Inicio, permisos, cálculo, edición de texto y el temporizador de cinco minutos.
La prueba HTTP existente sigue siendo válida sobre una base ficticia nueva.
La interfaz se comprobó con teclado virtual, cantidades negativas, calculadora, conservación
del carrito/formulario al ocultar el menú y una ventana de 1024 por 768.

Esta actualización no agrega migraciones ni modifica los registros existentes.
