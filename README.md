# PuntoAba

Sistema de punto de venta para una tienda de abarrotes, desarrollado con ASP.NET Core 9, Blazor Server, Entity Framework Core y SQLite.

## Funciones principales

- Punto de venta y registro de ventas.
- Productos, categorias, clientes y usuarios.
- Inventario y movimientos de existencias.
- Reportes y panel de control.
- API REST con autenticacion JWT.
- Integracion con balanza mediante puerto serie.
- Soporte para productos vendidos por peso.

## Requisitos

- .NET 9 SDK.

## Ejecutar el proyecto

```powershell
cd PuntoAba/PuntoAba
dotnet restore
# Define Jwt__Key con una clave aleatoria propia de al menos 32 bytes.
# Define Bootstrap__AdminPassword con una contraseña propia de al menos 12 caracteres.
# Usa variables de entorno en la sesión; no guardes estos valores en archivos versionados.
$env:ASPNETCORE_ENVIRONMENT = "Development"
dotnet run
```

En desarrollo, la aplicacion crea o actualiza una base de datos SQLite local. Las bases de datos, archivos temporales y resultados de compilacion no se incluyen en Git.

## Lector de codigos de barras

En Nueva Venta, usa el campo **Escanear o escribir codigo**. Los lectores configurados como teclado USB HID (o Bluetooth HID) pueden escribir el codigo y enviarlo con un sufijo **Enter**. Tambien puedes escribirlo manualmente y pulsar Enter o Agregar.

El codigo debe coincidir exactamente con el registrado en Productos, incluidos los ceros iniciales. Cada lectura agrega una unidad; los productos vendidos por peso abren la captura de peso. Se muestran avisos para codigos desconocidos y existencias insuficientes. El lector solo captura en su campo, para no interferir con cantidades, notas o ventanas de pago.

Los lectores configurados como puerto serie/COM, los que requieren un SDK del fabricante y la lectura mediante camara necesitan una integracion adicional. La compatibilidad fisica debe comprobarse con el modelo de lector que se vaya a utilizar.

## Entradas, movimientos y resurtido

- **Inventario > Entradas de mercancia**: agrega productos por codigo o desde el catalogo, captura cantidades y costos y confirma cuando los recibas. Puedes guardar proveedor, documento y notas. Cada recepcion genera un folio, actualiza existencias y deja movimientos. La casilla para actualizar el precio de compra es opcional.
- **Inventario > Movimientos**: filtra entradas, salidas y ajustes por fecha, producto, codigo o folio. Incluye existencias anteriores y posteriores; los datos previos muestran solamente los movimientos que ya estaban registrados.
- **Inventario > Resurtido**: revisa productos en su minimo o por debajo, ajusta cantidades y prepara una entrada. La sugerencia busca llegar al doble del minimo, con un objetivo de al menos una unidad; no usa pronosticos de ventas. Preparar la lista no modifica existencias: confirma la recepcion cuando la mercancia llegue.

Las tablas de recepciones se agregan mediante la migracion `EntradasMercancia`, sin reemplazar productos ni ventas existentes. Los reintentos del mismo registro conservan su identificador para evitar entradas duplicadas.

## Documentacion

- [Documentacion tecnica](README_TECNICO.md)
- [Manual tecnico](ManualTecnico.md)

## Seguridad

La clave JWT es obligatoria y se obtiene de `Jwt__Key` (al menos 32 bytes). No hay contraseña inicial pública. En desarrollo, `Bootstrap__AdminPassword` (al menos 12 caracteres) inicializa el usuario `admin` si falta o conserva el hash de ejemplo. No cambia la contraseña de usuarios existentes. SQLite usa un archivo local nuevo si no defines `ConnectionStrings__DefaultConnection`. Las bases locales y las imágenes cargadas no se distribuyen. El arranque y las migraciones automáticas descritas aquí son para desarrollo; producción requiere configurar la base, aplicar las migraciones y provisionar usuarios por separado.


## Control físico, permisos y respaldos

Inicia sesión antes de usar las pantallas. Admin y Supervisor reciben mercancía,
registran ajustes, mermas y devoluciones, autorizan conteos y consultan reportes.
Cajero puede vender, consultar existencias y capturar sus propios conteos; no
puede autorizar diferencias. Respaldos y administración de usuarios requieren Admin.
Los servicios comprueban el usuario activo y su rol real en la base de datos.

- **Productos** permite definir proveedor habitual y existencia objetivo (0 utiliza
  el doble del mínimo, al menos una unidad). Las existencias de productos existentes
  se cambian desde una operación identificada de inventario, con motivo.
- **Entradas** admite cajas completas con piezas por caja para productos por pieza.
  El costo es por pieza o unidad base; muestra existencias antes y después. Las
  cantidades por peso admiten hasta tres decimales. Confirma al recibir mercancía.
- **Conteo físico** guarda borradores y requiere Admin/Supervisor para aplicar
  diferencias. Si cambian las existencias durante el conteo, actualiza la base y
  vuelve a contar. Guarda responsable, autorización y motivo de cada diferencia.
- **Ajustes** registra cantidades positivas o negativas con motivo. **Mermas y
  devoluciones** distingue cliente, proveedor y producto no vendible. Una devolución
  dañada no aumenta lo disponible. Estas operaciones solo afectan inventario; no
  procesan reembolsos ni cambian los pagos de una venta.
- **Movimientos y correcciones** conserva el movimiento original y agrega una
  reversión vinculada con motivo. Nunca borra la historia ni admite stock negativo.
- **Resurtido** filtra por categoría y proveedor y usa el objetivo configurado.
- **Reportes de inventario** muestra valoración actual a costo, agotados, mermas,
  devoluciones y rotación orientativa del periodo basada en movimientos registrados.
  Costos históricos faltantes se estiman con el precio actual y se indican.
- **Respaldos** crea ZIP de SQLite e imágenes con huellas SHA-256. Mientras ABA
  esté abierto, revisa cada 30 minutos y crea copia si no hay una de las últimas
  24 horas. La comprobación recupera y valida una copia temporal, sin sustituir
  la base activa. Guarda otra copia en un medio adicional. `Respaldos/` está
  excluido de Git. Opciones: `Respaldos:Automaticos` y `Respaldos:Carpeta`.

Las operaciones conservan su identificador al reintentar para evitar duplicados;
la versión de inventario impide que una escritura antigua sobrescriba otra.
La migración `ControlFisicoYRespaldos` conserva productos, ventas y movimientos.
Lotes, caducidades y avisos móviles quedan fuera de esta etapa.

Validación de servicios (usa exclusivamente una base temporal):
`dotnet run --project tests/InventarioChecks/Checks.csproj`


## Interfaz y servicios

Las pantallas usan servicios compartidos y contratos DTO. La venta separa lector, catálogo, líneas del carrito, coordinación y estilos. La API incluye operaciones de inventario, conteos, reportes, imágenes y respaldos para conectar otra interfaz.

Consulta [Arquitectura y contratos para el nuevo front](docs/ARQUITECTURA_FRONTEND.md).


## Inicio y uso táctil

Inicio agrupa resúmenes y accesos a las pantallas. El menú lateral se oculta tras cinco minutos sin utilizarlo y puede volver a abrirse con la flecha. La barra de teclado, calculadora y números/símbolos permanece visible en todas las pantallas, incluido el acceso.

Consulta [Inicio y herramientas táctiles](docs/INICIO_TACTIL.md) para comportamiento, archivos y verificaciones.
