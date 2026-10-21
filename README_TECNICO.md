# Documentacion tecnica - PuntoAba

## Arquitectura general
Aplicacion unica ASP.NET Core 9 que hospeda dos superficies en el mismo proceso:

- **API REST** con controladores en `PuntoAba\PuntoAba\Api\Controllers`.
- **UI Blazor Server** con paginas Razor en `PuntoAba\PuntoAba\Views`.

La capa de datos usa **EF Core** con `PuntoAbaDbContext` y, por defecto, **SQLite**. El arranque y el pipeline HTTP estan en `Program.cs`.

## Configuracion y arranque
Archivos principales:

- `appsettings.json` y `appsettings.Development.json`

Claves relevantes:

- **ConnectionStrings:DefaultConnection**: por defecto `Data Source=PuntoAba.db` (SQLite).
- **Jwt**: `Key`, `Issuer`, `Audience`, `ExpiryInDays`.
- **AppSettings**: datos de empresa (nombre, RFC, direccion, telefono, moneda, IVA).
- **Balanza**: parametros de puerto serie y lectura de peso.

En desarrollo:

- Se aplican migraciones automaticamente (`Database.Migrate()`).
- Swagger queda disponible en `/api`.

## Base de datos (EF Core + SQLite)
### DbContext y tablas
`PuntoAbaDbContext` registra:

`Categorias`, `Productos`, `Clientes`, `Usuarios`, `Ventas`, `DetallesVenta`, `MovimientosInventario`, `RegistrosSincronizacion`.

### Entidades y campos clave
| Entidad | Campos clave | Notas |
| --- | --- | --- |
| **Categoria** | `Id`, `Nombre`, `Descripcion`, `Activo`, `FechaCreacion`, `FechaActualizacion` | Agrupa productos; soft-delete con `Activo`. |
| **Producto** | `Id`, `Codigo`, `Nombre`, `PrecioCompra`, `PrecioVenta`, `Stock`, `StockMinimo`, `CategoriaId`, `EsPorPeso`, `UnidadMedida`, `Activo`, `UltimaSincronizacion` | `Codigo` es unico. `Stock` y cantidades usan `decimal(18,3)`. |
| **Cliente** | `Id`, `Nombre`, `Apellido`, `Telefono`, `Email`, `Direccion`, `DocumentoIdentidad`, `Activo` | Soft-delete con `Activo`. |
| **Usuario** | `Id`, `NombreUsuario`, `NombreCompleto`, `Email`, `PasswordHash`, `Rol`, `Telefono`, `Activo`, `UltimoAcceso` | `NombreUsuario` y `Email` son unicos. |
| **Venta** | `Id`, `NumeroVenta`, `FechaVenta`, `ClienteId`, `UsuarioId`, `Subtotal`, `Descuento`, `Impuesto`, `Total`, `MetodoPago`, `EstadoVenta`, `Sincronizado`, `OrigenVenta`, `UltimaSincronizacion` | `NumeroVenta` es unico. Montos en `decimal(18,2)`. |
| **DetalleVenta** | `Id`, `VentaId`, `ProductoId`, `Cantidad`, `PrecioUnitario`, `Descuento`, `Subtotal` | `Cantidad` en `decimal(18,3)` (soporta venta por peso). |
| **MovimientoInventario** | `Id`, `ProductoId`, `TipoMovimiento`, `Cantidad`, `StockAnterior`, `StockNuevo`, `Motivo`, `UsuarioId`, `FechaMovimiento`, `Referencia` | Registro de entradas, salidas y ajustes. |
| **RegistroSincronizacion** | `Id`, `Entidad`, `EntidadId`, `TipoOperacion`, `FechaSincronizacion`, `DispositivoOrigen`, `Exitoso`, `MensajeError`, `DatosJson` | Auditoria de sincronizacion. |

### Relaciones y reglas
Reglas definidas en `OnModelCreating`:

- **Producto -> Categoria**: `Restrict` (no elimina categoria si tiene productos).
- **Venta -> Cliente**: `SetNull` (cliente opcional).
- **Venta -> Usuario**: `Restrict`.
- **DetalleVenta -> Venta**: `Cascade`.
- **DetalleVenta -> Producto**: `Restrict`.
- **MovimientoInventario -> Producto**: `Cascade`.

### Indices unicos
- `Producto.Codigo`
- `Usuario.NombreUsuario`
- `Usuario.Email`
- `Venta.NumeroVenta`

### Semantica de decimales
- Montos: `decimal(18,2)` (precios, totales, impuestos).
- Cantidades/stock: `decimal(18,3)` para soportar fracciones (productos por peso).

### Seed data
Se insertan datos iniciales:

- 6 categorias base.
- Usuario admin (password `CONFIGURAR_PASSWORD_MEDIANTE_ENTORNO`, hash BCrypt).
- Cliente generico.

## Servicios de dominio (Application/Services)
### AuthService
Autenticacion con BCrypt y JWT:

- `LoginAsync` valida usuario activo y genera token.
- CRUD de usuarios con rol y soft-delete (`Activo`).
- Cambio de password con verificacion de hash.

### ProductoService
Gestion de productos:

- CRUD con soft-delete.
- Busqueda por termino, codigo y categoria.
- Ajuste de stock con registro en `MovimientoInventario`.

### VentaService
Venta transaccional:

- Genera `NumeroVenta` por fecha (`VyyyyMMddNNNN`).
- Valida existencia, cantidad > 0, stock suficiente.
- Respeta `EsPorPeso`: si es falso, requiere cantidades enteras.
- Actualiza stock y registra `MovimientoInventario`.
- Expone reportes y consultas por fecha, cliente o usuario.

### SincronizacionService
Intercambio de datos:

- `SincronizarDatosAsync` procesa productos, clientes y ventas en una transaccion.
- `ObtenerDatosParaSincronizarAsync` filtra por `UltimaSincronizacion` o `FechaActualizacion`.
- Registra `RegistroSincronizacion` y marca ventas sincronizadas.

### SesionUsuarioService
Sesion en Blazor:

- Guarda token y usuario en `localStorage`.
- Aplica `Bearer` al `HttpClient`.

### BalanzaService
Servicio en background para lectura de balanza serial:

- Auto-deteccion de puerto (si `AutoDetect = true`).
- Comando configurable (`Balanza:Command`).
- Parseo de lectura y validacion con `MinimumValidWeight`.
- Estado expuesto a `BalanzaController`.

## API REST (controladores)
| Ruta base | Descripcion | Autorizacion |
| --- | --- | --- |
| `/api/auth` | Login y gestion de usuarios | Login anonimo; gestion Admin/Supervisor |
| `/api/productos` | CRUD, busqueda, stock bajo | Lectura autenticada; cambios Admin/Supervisor |
| `/api/ventas` | Listado, detalle, reporte, crear venta | Auth general; reporte Admin/Supervisor |
| `/api/clientes` | CRUD y busqueda | Lecturas anonimas; cambios auth; delete Admin |
| `/api/categorias` | CRUD categorias | Auth; cambios Admin/Supervisor |
| `/api/dashboard` | Estadisticas y agregados | Admin/Supervisor |
| `/api/sincronizacion` | Sync de datos | Auth |
| `/api/balanza/leer` | Lectura de peso actual | Publico (sin `[Authorize]`) |

Endpoints adicionales:

- `/api/info` (info general de API)
- `/health` (health check)

## UI Blazor Server
Paginas principales en `Views`:

- Productos, Categorias, Clientes, Ventas, Inventario, Reportes, Dashboard, Usuarios, Login.

La UI consume la API local via `HttpClient` y usa `SesionUsuarioService` para manejar la sesion y el token JWT.

## Flujos clave
### Venta
1. Se valida producto y cantidad.
2. Si `EsPorPeso` es falso, la cantidad debe ser entera.
3. Se descuenta stock.
4. Se registra `MovimientoInventario` (Salida).
5. Se persiste la venta con detalles en una transaccion.

### Ajuste manual de stock
1. `ProductoService.ActualizarStockAsync` suma o resta cantidad.
2. Registra movimiento con motivo "Ajuste manual".

### Sincronizacion
1. Cliente envía `SincronizacionDto` con cambios.
2. Se actualizan/crean entidades segun IDs o codigos.
3. Se registra `RegistroSincronizacion`.

### Balanza
1. Servicio intenta conectar a puerto serie.
2. Solicita lectura con comando configurado.
3. El controlador expone `peso`, `unidad`, `estado` y metadatos.

## Seguridad y autenticacion
JWT configurado en `Program.cs`:

- Issuer y Audience obligatorios.
- Token firmado con clave simetrica (`Jwt:Key`).
- Roles en claims (`Admin`, `Supervisor`, `Cajero`).

Swagger en desarrollo expone autenticacion Bearer para pruebas manuales.
