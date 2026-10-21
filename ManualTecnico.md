# Manual tecnico - Codigo funcional (lineas y explicacion)

Nota: Las lineas corresponden al numero real en cada archivo del repositorio.

## Resumen por componentes (sin lineas)

### 1) Arranque y configuracion
| Archivo | Que hace | Explicacion breve |
|---|---|---|
| `PuntoAba\Program.cs` | Arranque de la app, DI, auth, swagger, endpoints y Blazor | Configura EF Core (SQLite), JWT, CORS, Swagger, servicios de negocio y balanza; registra Blazor Server y endpoints `/api`, `/health` y `/api/info`, ademas de sembrar el usuario admin en desarrollo. |
| `PuntoAba\appsettings*.json` | Configuracion | Define conexion SQLite, JWT, datos de empresa y parametros de balanza serial (puerto, baudrate, comando, etc.). |

### 2) Capa de datos (EF Core)
| Archivo | Que hace | Explicacion breve |
|---|---|---|
| `Infrastructure\Data\PuntoAbaDbContext.cs` | DbContext y mapeo | Declara DbSets, relaciones, indices y datos semilla (categorias, admin, cliente generico). |
| `Domain\Models\Producto.cs` | Entidad producto | Precios, stock, unidad, `EsPorPeso`, `Activo`, auditoria y sincronizacion. |
| `Domain\Models\Categoria.cs` | Entidad categoria | Nombre, descripcion, `Activo` y relacion con productos. |
| `Domain\Models\Cliente.cs` | Entidad cliente | Datos personales, `Activo`, auditoria y relacion con ventas. |
| `Domain\Models\Usuario.cs` | Entidad usuario | Credenciales, rol, `Activo` y ultimo acceso. |
| `Domain\Models\Venta.cs` | Entidad venta | Totales, metodo, cliente/usuario, sincronizacion y relacion con detalles. |
| `Domain\Models\DetalleVenta.cs` | Entidad detalle | Producto, cantidad, precio, descuento y subtotal. |
| `Domain\Models\MovimientoInventario.cs` | Registro inventario | Entrada/salida/ajuste con stock anterior/nuevo. |
| `Domain\Models\RegistroSincronizacion.cs` | Bitacora sync | Registra operaciones y estado de sincronizacion. |

### 3) Servicios de aplicacion (logica de negocio)
| Archivo | Que hace | Explicacion breve |
|---|---|---|
| `Application\Services\AuthService.cs` | Autenticacion/usuarios | Login JWT, CRUD de usuarios y cambio de password con BCrypt. |
| `Application\Services\ProductoService.cs` | Gestion de productos | CRUD, busqueda, stock bajo y ajuste de inventario con movimiento. |
| `Application\Services\VentaService.cs` | Ventas e inventario | Crea ventas con transaccion, valida stock/unidades, descuenta inventario y genera numero de venta. |
| `Application\Services\SincronizacionService.cs` | Sincronizacion | Recibe/entrega datos para sync (productos, clientes, ventas) y registra estado. |
| `Application\Services\SesionUsuarioService.cs` | Sesion en Blazor | Maneja token en localStorage y configura `HttpClient` para la UI. |
| `Application\Services\BalanzaService.cs` | Lectura de balanza | Servicio en background que detecta puerto serial, lee peso y expone estado. |

### 4) API Controllers (REST)
| Archivo | Que hace | Explicacion breve |
|---|---|---|
| `Api\Controllers\AuthController.cs` | Login y usuarios | Endpoints para autenticacion y administracion de usuarios con roles. |
| `Api\Controllers\ProductosController.cs` | Productos | CRUD, busqueda, stock bajo y ajuste de stock. |
| `Api\Controllers\VentasController.cs` | Ventas | Consulta, reporte y creacion de venta. |
| `Api\Controllers\ClientesController.cs` | Clientes | CRUD directo con DbContext (uso publico de lectura). |
| `Api\Controllers\CategoriasController.cs` | Categorias | CRUD directo con DbContext y validacion de productos activos. |
| `Api\Controllers\DashboardController.cs` | Metricas | Estadisticas de ventas, inventario y top productos. |
| `Api\Controllers\SincronizacionController.cs` | Sync | Orquesta la sincronizacion entre cliente y API. |
| `Api\Controllers\BalanzaController.cs` | Lectura balanza | Expone `/api/balanza/leer` con estado y peso actual. |
| `Api\Controllers\TestController.cs` | Ping | Endpoint simple para verificar disponibilidad. |

### 5) DTOs (contratos de API)
| Archivo | Que hace | Explicacion breve |
|---|---|---|
| `Api\DTOs\ProductoDto.cs` | Producto (create/update) | Modela los datos que entran/salen de productos. |
| `Api\DTOs\VentaDto.cs` | Venta y detalle | Define venta y sus lineas para consultas/alta. |
| `Api\DTOs\ClienteDto.cs` | Cliente (create/update) | Contrato de clientes. |
| `Api\DTOs\CategoriaDto.cs` | Categoria (create/update) | Contrato de categorias. |
| `Api\DTOs\UsuarioDto.cs` | Usuario, login y cambio de password | DTOs para auth y administracion. |
| `Api\DTOs\SincronizacionDto.cs` | Sync request/response | Paquetes de sincronizacion y estado. |
| `Api\DTOs\ReporteDto.cs` | Reporte de ventas | Resumen agregado y lista de ventas. |

### 6) UI Blazor (vistas principales)
| Archivo | Que hace | Explicacion breve |
|---|---|---|
| `App.razor` / `Routes.razor` | Enrutamiento | Configura el router Blazor y layout principal. |
| `Shared\MainLayout.razor` | Layout general | Sidebar + area de contenido. |
| `Shared\NavMenu.razor` | Menu lateral | Navegacion a modulos del POS. |
| `Shared\BlankLayout.razor` | Layout simple | Usado para login sin menu. |
| `Pages\_Host.cshtml` | Host Blazor | Pagina raiz para Blazor Server. |
| `Views\Auth\Login.razor` | Login | Autenticacion con `SesionUsuarioService` y redireccion al dashboard. |
| `Views\Dashboard\Index.razor` | Panel principal | Consume `/api/dashboard/estadisticas`, muestra metricas y accesos rapidos. |
| `Views\Ventas\Nuevo.razor` | Punto de venta | Catalogo, carrito, captura por peso (balanza), cliente/metodo de pago y creacion de venta. |
| `Views\Ventas\Index.razor` | Historial de ventas | Filtros por fecha y detalle de venta en modal. |
| `Views\Productos\Index.razor` | Gestion productos | CRUD, filtros, ajuste de stock y carga de imagen a `wwwroot/images/productos`. |
| `Views\Clientes\Index.razor` | Gestion clientes | CRUD directo con DbContext y modales. |
| `Views\Categorias\Index.razor` | Gestion categorias | CRUD directo con DbContext. |
| `Views\Usuarios\Index.razor` | Gestion usuarios | Alta y edicion via `AuthService`. |
| `Views\Inventario\Index.razor` | Inventario | Muestra stock bajo y resumen. |
| `Views\Reportes\Index.razor` | Reportes | Genera reporte de ventas por rango. |
| `Views\Sincronizacion\Index.razor` | Sync manual | Consulta estado y ejecuta sincronizacion. |

## Backend

### Archivo: `PuntoAba\Program.cs`

**Lineas 11-23 (configuracion de base de datos)**
```csharp
11. // Configuración de base de datos
12. builder.Services.AddDbContext<PuntoAbaDbContext>(options =>
13. {
14.     var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
15.     
16.     // Si no hay connection string configurada, usar SQLite para desarrollo
17.     if (string.IsNullOrEmpty(connectionString))
18.     {
19.         connectionString = "Data Source=PuntoAba.db";
20.     }
21.     
22.     options.UseSqlite(connectionString);
23. });
```
**Que hace:** Registra EF Core con SQLite y aplica un fallback si no existe connection string.

**Lineas 25-47 (autenticacion JWT)**
```csharp
25. // Configuración de autenticación JWT
26. var jwtKey = builder.Configuration["Jwt:Key"] ?? "CONFIGURAR_JWT_MEDIANTE_ENTORNO";
27. var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "PuntoAba";
28. var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "PuntoAbaClients";
29. 
30. builder.Services.AddAuthentication(options =>
31. {
32.     options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
33.     options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
34. })
35. .AddJwtBearer(options =>
36. {
37.     options.TokenValidationParameters = new TokenValidationParameters
38.     {
39.         ValidateIssuer = true,
40.         ValidateAudience = true,
41.         ValidateLifetime = true,
42.         ValidateIssuerSigningKey = true,
43.         ValidIssuer = jwtIssuer,
44.         ValidAudience = jwtAudience,
45.         IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
46.     };
47. });
```
**Que hace:** Configura autenticacion JWT (emisor, audiencia, firma y expiracion).

**Lineas 62-79 (registro de servicios + Blazor)**
```csharp
62. // Registro de servicios
63. builder.Services.AddScoped<IProductoService, ProductoService>();
64. builder.Services.AddScoped<IVentaService, VentaService>();
65. builder.Services.AddScoped<IAuthService, AuthService>();
66. builder.Services.AddScoped<ISincronizacionService, SincronizacionService>();
67. builder.Services.Configure<BalanzaSettings>(builder.Configuration.GetSection("Balanza"));
68. builder.Services.AddSingleton<BalanzaService>();
69. builder.Services.AddSingleton<IBalanzaService>(sp => sp.GetRequiredService<BalanzaService>());
70. builder.Services.AddHostedService(sp => sp.GetRequiredService<BalanzaService>());
71. 
72. // Configuración de Blazor Server
73. builder.Services.AddRazorPages();
74. builder.Services.AddServerSideBlazor();
75. 
76. // Registrar HttpClient
77. builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.Configuration["ApiBaseUrl"] ?? "http://localhost:5000") });
78. builder.Services.AddScoped<SesionUsuarioService>();
```
**Que hace:** Registra servicios de dominio, balanza en background, Blazor Server y HttpClient para la UI.

**Lineas 90-130 (Swagger con JWT)**
```csharp
90. // Configuración de Swagger con autenticación JWT
91. builder.Services.AddSwaggerGen(c =>
92. {
93.     c.SwaggerDoc("v1", new OpenApiInfo
94.     {
95.         Title = "PuntoAba API",
96.         Version = "v1",
97.         Description = "API para sistema de punto de venta de tienda de abarrotes",
98.         Contact = new OpenApiContact
99.         {
100.             Name = "PuntoAba",
101.             Email = "contacto@puntoaba.com"
102.         }
103.     });
104. 
105.     // Configurar autenticación JWT en Swagger
106.     c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
107.     {
108.         Description = "Autenticación JWT. Ingrese 'Bearer' seguido de un espacio y el token JWT.\r\n\r\nEjemplo: 'Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...'",
109.         Name = "Authorization",
110.         In = ParameterLocation.Header,
111.         Type = SecuritySchemeType.ApiKey,
112.         Scheme = "Bearer",
113.         BearerFormat = "JWT"
114.     });
115. 
116.     c.AddSecurityRequirement(new OpenApiSecurityRequirement
117.     {
118.         {
119.             new OpenApiSecurityScheme
120.             {
121.                 Reference = new OpenApiReference
122.                 {
123.                     Type = ReferenceType.SecurityScheme,
124.                     Id = "Bearer"
125.                 }
126.             },
127.             Array.Empty<string>()
128.         }
129.     });
130. });
```
**Que hace:** Habilita Swagger y agrega autenticacion JWT para probar endpoints protegidos.

**Lineas 134-150 (migraciones automaticas en desarrollo)**
```csharp
134. // Aplicar migraciones automáticamente en desarrollo
135. if (app.Environment.IsDevelopment())
136. {
137.     using (var scope = app.Services.CreateScope())
138.     {
139.         var dbContext = scope.ServiceProvider.GetRequiredService<PuntoAbaDbContext>();
140.         var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
141.         try
142.         {
143.             dbContext.Database.Migrate();
144.             AsegurarUsuarioAdminDesarrollo(dbContext, logger);
145.         }
146.         catch (Exception ex)
147.         {
148.             logger.LogWarning(ex, "No se pudieron aplicar las migraciones automáticamente. Ejecute 'dotnet ef database update' manualmente.");
149.         }
150.     }
151. }
```
**Que hace:** Aplica migraciones y crea usuario admin en ambiente de desarrollo.

**Lineas 153-178 (pipeline HTTP)**
```csharp
153. // Configuración del pipeline HTTP
154. if (app.Environment.IsDevelopment())
155. {
156.     app.UseSwagger();
157.     app.UseSwaggerUI(c =>
158.     {
159.         c.SwaggerEndpoint("/swagger/v1/swagger.json", "PuntoAba API v1");
160.         c.RoutePrefix = "api"; // Swagger en /api
161.     });
162. }
163. 
164. // app.UseHttpsRedirection();  // Deshabilitado para desarrollo
165. 
166. app.UseStaticFiles();
167. 
168. app.UseCors("AllowAll");
169. 
170. app.UseRouting();
171. 
172. app.UseAuthentication();
173. app.UseAuthorization();
174. 
175. app.MapControllers();
176. app.MapRazorPages();
177. app.MapBlazorHub();
178. app.MapFallbackToPage("/_Host");
```
**Que hace:** Arma el pipeline, habilita CORS, auth, controllers y Blazor.

**Lineas 180-199 (endpoints de info y salud)**
```csharp
180. // Endpoint informativo de la API
181. app.MapGet("/api/info", () => Results.Ok(new {
182.     mensaje = "PuntoAba API - Balanza Operativa",
183.     endpoints = new {
184.         balanza = "/api/balanza/leer",
185.         clientes = "/api/clientes",
186.         productos = "/api/productos",
187.         swagger = "/api"
188.     }
189. }));
190. 
191. // Endpoint de salud
192. app.MapGet("/health", () => Results.Ok(new
193. {
194.     status = "Healthy",
195.     timestamp = DateTime.UtcNow,
196.     version = "1.0.0"
197. }))
198. .WithName("HealthCheck")
199. .WithTags("Health");
```
**Que hace:** Expone endpoints de verificacion basicos.

**Lineas 203-236 (usuario admin de desarrollo)**
```csharp
203. static void AsegurarUsuarioAdminDesarrollo(PuntoAbaDbContext dbContext, ILogger logger)
204. {
205.     const string usuarioAdmin = "admin";
206.     const string emailAdmin = "admin@puntoaba.com";
207.     const string passwordAdmin = "CONFIGURAR_PASSWORD_MEDIANTE_ENTORNO";
208.     const string hashPlaceholder = "$2a$11$9vJZ5qZ5qZ5qZ5qZ5qZ5qOxK3kZ4kZ4kZ4kZ4kZ4kZ4kZ4kZ4kZ4m";
209. 
210.     var admin = dbContext.Usuarios.FirstOrDefault(u => u.NombreUsuario == usuarioAdmin);
211.     if (admin == null)
212.     {
213.         dbContext.Usuarios.Add(new PuntoAba.Models.Usuario
214.         {
215.             NombreUsuario = usuarioAdmin,
216.             NombreCompleto = "Administrador del Sistema",
217.             Email = emailAdmin,
218.             PasswordHash = BCrypt.Net.BCrypt.HashPassword(passwordAdmin),
219.             Rol = "Admin",
220.             Activo = true,
221.             FechaCreacion = DateTime.UtcNow
222.         });
223.         dbContext.SaveChanges();
224.         logger.LogInformation("Se creó usuario admin de desarrollo.");
225.         return;
226.     }
227. 
228.     if (admin.PasswordHash == hashPlaceholder)
229.     {
230.         admin.PasswordHash = BCrypt.Net.BCrypt.HashPassword(passwordAdmin);
231.         admin.Activo = true;
232.         admin.Rol = "Admin";
233.         admin.FechaActualizacion = DateTime.UtcNow;
234.         dbContext.SaveChanges();
235.         logger.LogInformation("Se actualizó hash placeholder del usuario admin de desarrollo.");
236.     }
237. }
```
**Que hace:** Crea o actualiza el usuario admin para entorno local.

### Archivo: `Infrastructure\Data\PuntoAbaDbContext.cs`

**Lineas 12-20 (DbSets)**
```csharp
12.         public DbSet<Categoria> Categorias { get; set; }
13.         public DbSet<Producto> Productos { get; set; }
14.         public DbSet<Cliente> Clientes { get; set; }
15.         public DbSet<Usuario> Usuarios { get; set; }
16.         public DbSet<Venta> Ventas { get; set; }
17.         public DbSet<DetalleVenta> DetallesVenta { get; set; }
18.         public DbSet<MovimientoInventario> MovimientosInventario { get; set; }
19.         public DbSet<RegistroSincronizacion> RegistrosSincronizacion { get; set; }
```
**Que hace:** Define las tablas principales del sistema.

**Lineas 25-78 (indices y relaciones)**
```csharp
25.             // Configuración de índices
26.             modelBuilder.Entity<Producto>()
27.                 .HasIndex(p => p.Codigo)
28.                 .IsUnique();
29. 
30.             modelBuilder.Entity<Usuario>()
31.                 .HasIndex(u => u.NombreUsuario)
32.                 .IsUnique();
33. 
34.             modelBuilder.Entity<Usuario>()
35.                 .HasIndex(u => u.Email)
36.                 .IsUnique();
37. 
38.             modelBuilder.Entity<Venta>()
39.                 .HasIndex(v => v.NumeroVenta)
40.                 .IsUnique();
41. 
42.             // Configuración de relaciones
43.             modelBuilder.Entity<Producto>()
44.                 .HasOne(p => p.Categoria)
45.                 .WithMany(c => c.Productos)
46.                 .HasForeignKey(p => p.CategoriaId)
47.                 .OnDelete(DeleteBehavior.Restrict);
48. 
49.             modelBuilder.Entity<Venta>()
50.                 .HasOne(v => v.Cliente)
51.                 .WithMany(c => c.Ventas)
52.                 .HasForeignKey(v => v.ClienteId)
53.                 .OnDelete(DeleteBehavior.SetNull);
54. 
55.             modelBuilder.Entity<Venta>()
56.                 .HasOne(v => v.Usuario)
57.                 .WithMany(u => u.Ventas)
58.                 .HasForeignKey(v => v.UsuarioId)
59.                 .OnDelete(DeleteBehavior.Restrict);
60. 
61.             modelBuilder.Entity<DetalleVenta>()
62.                 .HasOne(d => d.Venta)
63.                 .WithMany(v => v.DetallesVenta)
64.                 .HasForeignKey(d => d.VentaId)
65.                 .OnDelete(DeleteBehavior.Cascade);
66. 
67.             modelBuilder.Entity<DetalleVenta>()
68.                 .HasOne(d => d.Producto)
69.                 .WithMany(p => p.DetallesVenta)
70.                 .HasForeignKey(d => d.ProductoId)
71.                 .OnDelete(DeleteBehavior.Restrict);
72. 
73.             modelBuilder.Entity<MovimientoInventario>()
74.                 .HasOne(m => m.Producto)
75.                 .WithMany(p => p.MovimientosInventario)
76.                 .HasForeignKey(m => m.ProductoId)
77.                 .OnDelete(DeleteBehavior.Cascade);
```
**Que hace:** Define indices unicos y las relaciones entre entidades.

**Lineas 83-125 (datos semilla)**
```csharp
83.         private void SeedData(ModelBuilder modelBuilder)
84.         {
85.             var fechaCreacion = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
86. 
87.             // Categorías iniciales
88.             modelBuilder.Entity<Categoria>().HasData(
89.                 new Categoria { Id = 1, Nombre = "Bebidas", Descripcion = "Bebidas y refrescos", FechaCreacion = fechaCreacion },
90.                 new Categoria { Id = 2, Nombre = "Abarrotes", Descripcion = "Productos de abarrotes", FechaCreacion = fechaCreacion },
91.                 new Categoria { Id = 3, Nombre = "Lácteos", Descripcion = "Productos lácteos", FechaCreacion = fechaCreacion },
92.                 new Categoria { Id = 4, Nombre = "Panadería", Descripcion = "Pan y pastelería", FechaCreacion = fechaCreacion },
93.                 new Categoria { Id = 5, Nombre = "Snacks", Descripcion = "Botanas y dulces", FechaCreacion = fechaCreacion },
94.                 new Categoria { Id = 6, Nombre = "Limpieza", Descripcion = "Productos de limpieza", FechaCreacion = fechaCreacion }
95.             );
96. 
97.             // Usuario administrador inicial
98.             // Password: CONFIGURAR_PASSWORD_MEDIANTE_ENTORNO (hasheado con BCrypt)
99.             modelBuilder.Entity<Usuario>().HasData(
100.                 new Usuario
101.                 {
102.                     Id = 1,
103.                     NombreUsuario = "admin",
104.                     NombreCompleto = "Administrador del Sistema",
105.                     Email = "admin@puntoaba.com",
106.                     PasswordHash = "$2a$11$9vJZ5qZ5qZ5qZ5qZ5qZ5qOxK3kZ4kZ4kZ4kZ4kZ4kZ4kZ4kZ4kZ4m",
107.                     Rol = "Admin",
108.                     Activo = true,
109.                     FechaCreacion = fechaCreacion
110.                 }
111.             );
112. 
113.             // Cliente genérico
114.             modelBuilder.Entity<Cliente>().HasData(
115.                 new Cliente
116.                 {
117.                     Id = 1,
118.                     Nombre = "Cliente",
119.                     Apellido = "Genérico",
120.                     Telefono = "0000000000",
121.                     Email = "cliente@general.com",
122.                     Activo = true,
123.                     FechaCreacion = fechaCreacion
124.                 }
125.             );
126.         }
```
**Que hace:** Crea datos iniciales para categorias, usuario admin y cliente general.

### Archivo: `Domain\Models\Producto.cs`
**Lineas 22-37 (precios, stock y venta por peso)**
```csharp
22.         [Required]
23.         [Column(TypeName = "decimal(18,2)")]
24.         public decimal PrecioCompra { get; set; }
25. 
26.         [Required]
27.         [Column(TypeName = "decimal(18,2)")]
28.         public decimal PrecioVenta { get; set; }
29. 
30.         [Required]
31.         [Column(TypeName = "decimal(18,3)")]
32.         public decimal Stock { get; set; }
33. 
34.         [Column(TypeName = "decimal(18,3)")]
35.         public decimal StockMinimo { get; set; } = 0;
36. 
37.         public bool EsPorPeso { get; set; } = false;
```
**Que hace:** Define precision de dinero/stock y si el producto se vende por peso.

### Archivo: `Domain\Models\Venta.cs`
**Lineas 22-39 (totales y metodo de pago)**
```csharp
22.         [Required]
23.         [Column(TypeName = "decimal(18,2)")]
24.         public decimal Subtotal { get; set; }
25. 
26.         [Column(TypeName = "decimal(18,2)")]
27.         public decimal Descuento { get; set; } = 0;
28. 
29.         [Column(TypeName = "decimal(18,2)")]
30.         public decimal Impuesto { get; set; } = 0;
31. 
32.         [Required]
33.         [Column(TypeName = "decimal(18,2)")]
34.         public decimal Total { get; set; }
35. 
36.         [Required]
37.         [StringLength(50)]
38.         public string MetodoPago { get; set; } = "Efectivo";
39. 
```
**Que hace:** Modela los montos principales de la venta y el metodo de pago.

### Archivo: `Domain\Models\DetalleVenta.cs`
**Lineas 18-30 (cantidad y precios)**
```csharp
18.         [Column(TypeName = "decimal(18,3)")]
19.         public decimal Cantidad { get; set; }
20. 
21.         [Required]
22.         [Column(TypeName = "decimal(18,2)")]
23.         public decimal PrecioUnitario { get; set; }
24. 
25.         [Column(TypeName = "decimal(18,2)")]
26.         public decimal Descuento { get; set; } = 0;
27. 
28.         [Required]
29.         [Column(TypeName = "decimal(18,2)")]
30.         public decimal Subtotal { get; set; }
```
**Que hace:** Define cantidades con precision y el subtotal de cada linea.

## Servicios (logica de negocio)

### Archivo: `Application\Services\AuthService.cs`
**Lineas 23-45 (login y token)**
```csharp
23.         public async Task<LoginResponseDto?> LoginAsync(LoginDto dto)
24.         {
25.             var usuario = await _context.Usuarios
26.                 .FirstOrDefaultAsync(u => u.NombreUsuario == dto.NombreUsuario && u.Activo);
27. 
28.             if (usuario == null || !BCrypt.Net.BCrypt.Verify(dto.Password, usuario.PasswordHash))
29.                 return null;
30. 
31.             // Actualizar último acceso
32.             usuario.UltimoAcceso = DateTime.UtcNow;
33.             await _context.SaveChangesAsync();
34. 
35.             var token = GenerarToken(usuario);
36. 
37.             return new LoginResponseDto
38.             {
39.                 Id = usuario.Id,
40.                 NombreUsuario = usuario.NombreUsuario,
41.                 NombreCompleto = usuario.NombreCompleto,
42.                 Email = usuario.Email,
43.                 Rol = usuario.Rol,
44.                 Token = token
45.             };
46.         }
```
**Que hace:** Valida credenciales, actualiza ultimo acceso y genera JWT.

**Lineas 48-73 (crear usuario)**
```csharp
48.         public async Task<UsuarioDto> CrearUsuarioAsync(CrearUsuarioDto dto)
49.         {
50.             // Verificar que no exista el usuario
51.             if (await _context.Usuarios.AnyAsync(u => u.NombreUsuario == dto.NombreUsuario))
52.                 throw new Exception("El nombre de usuario ya existe");
53. 
54.             if (await _context.Usuarios.AnyAsync(u => u.Email == dto.Email))
55.                 throw new Exception("El email ya está registrado");
56. 
57.             var usuario = new Usuario
58.             {
59.                 NombreUsuario = dto.NombreUsuario,
60.                 NombreCompleto = dto.NombreCompleto,
61.                 Email = dto.Email,
62.                 PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
63.                 Rol = dto.Rol,
64.                 Telefono = dto.Telefono,
65.                 Activo = true,
66.                 FechaCreacion = DateTime.UtcNow
67.             };
68. 
69.             _context.Usuarios.Add(usuario);
70.             await _context.SaveChangesAsync();
71. 
72.             return MapToDto(usuario);
73.         }
```
**Que hace:** Valida duplicados, hashea password y crea un usuario.

### Archivo: `Application\Services\ProductoService.cs`
**Lineas 17-25 (consulta de productos activos)**
```csharp
17.         public async Task<IEnumerable<ProductoDto>> ObtenerTodosAsync()
18.         {
19.             var productos = await _context.Productos
20.                 .Include(p => p.Categoria)
21.                 .Where(p => p.Activo)
22.                 .ToListAsync();
23. 
24.             return productos.Select(MapToDto);
25.         }
```
**Que hace:** Lista productos activos con su categoria.

**Lineas 45-68 (alta de producto)**
```csharp
45.         public async Task<ProductoDto> CrearAsync(CrearProductoDto dto)
46.         {
47.             var producto = new Producto
48.             {
49.                 Codigo = dto.Codigo,
50.                 Nombre = dto.Nombre,
51.                 Descripcion = dto.Descripcion,
52.                 PrecioCompra = dto.PrecioCompra,
53.                 PrecioVenta = dto.PrecioVenta,
54.                 Stock = dto.Stock,
55.                 StockMinimo = dto.StockMinimo,
56.                 CategoriaId = dto.CategoriaId,
57.                 UnidadMedida = dto.UnidadMedida,
58.                 EsPorPeso = dto.EsPorPeso,
59.                 ImagenUrl = dto.ImagenUrl,
60.                 FechaCreacion = DateTime.UtcNow,
61.                 UltimaSincronizacion = DateTime.UtcNow
62.             };
63. 
64.             _context.Productos.Add(producto);
65.             await _context.SaveChangesAsync();
66. 
67.             return (await ObtenerPorIdAsync(producto.Id))!;
68.         }
```
**Que hace:** Crea el producto y establece datos de sincronizacion.

**Lineas 106-129 (ajuste de stock con movimiento)**
```csharp
106.         public async Task<bool> ActualizarStockAsync(int id, decimal cantidad)
107.         {
108.             var producto = await _context.Productos.FindAsync(id);
109.             if (producto == null) return false;
110. 
111.             var stockAnterior = producto.Stock;
112.             producto.Stock += cantidad;
113.             producto.FechaActualizacion = DateTime.UtcNow;
114. 
115.             var movimiento = new MovimientoInventario
116.             {
117.                 ProductoId = id,
118.                 TipoMovimiento = cantidad > 0 ? "Entrada" : "Salida",
119.                 Cantidad = Math.Abs(cantidad),
120.                 StockAnterior = stockAnterior,
121.                 StockNuevo = producto.Stock,
122.                 FechaMovimiento = DateTime.UtcNow,
123.                 Motivo = "Ajuste manual"
124.             };
125. 
126.             _context.MovimientosInventario.Add(movimiento);
127.             await _context.SaveChangesAsync();
128. 
129.             return true;
130.         }
```
**Que hace:** Ajusta el stock y registra el movimiento de inventario.

### Archivo: `Application\Services\VentaService.cs`
**Lineas 57-148 (crear venta con transaccion)**
```csharp
57.         public async Task<VentaDto> CrearAsync(CrearVentaDto dto)
58.         {
59.             using var transaction = await _context.Database.BeginTransactionAsync();
60.             try
61.             {
62.                 // Generar número de venta
63.                 var numeroVenta = await GenerarNumeroVentaAsync();
64. 
65.                 // Calcular totales
66.                 decimal subtotal = 0;
67.                 var detalles = new List<DetalleVenta>();
68. 
69.                 foreach (var detalleDto in dto.Detalles)
70.                 {
71.                     var producto = await _context.Productos.FindAsync(detalleDto.ProductoId);
72.                     if (producto == null)
73.                         throw new Exception($"Producto con ID {detalleDto.ProductoId} no encontrado");
74. 
75.                     if (detalleDto.Cantidad <= 0)
76.                         throw new Exception($"La cantidad del producto {producto.Nombre} debe ser mayor a cero");
77. 
78.                     if (!producto.EsPorPeso && detalleDto.Cantidad != Math.Truncate(detalleDto.Cantidad))
79.                         throw new Exception($"El producto {producto.Nombre} se vende por unidad y requiere cantidades enteras");
80. 
81.                     if (producto.Stock < detalleDto.Cantidad)
82.                         throw new Exception($"Stock insuficiente para el producto {producto.Nombre}");
83. 
84.                     var subtotalDetalle = (detalleDto.Cantidad * detalleDto.PrecioUnitario) - detalleDto.Descuento;
85.                     subtotal += subtotalDetalle;
86. 
87.                     var detalle = new DetalleVenta
88.                     {
89.                         ProductoId = detalleDto.ProductoId,
90.                         Cantidad = detalleDto.Cantidad,
91.                         PrecioUnitario = detalleDto.PrecioUnitario,
92.                         Descuento = detalleDto.Descuento,
93.                         Subtotal = subtotalDetalle
94.                     };
95. 
96.                     detalles.Add(detalle);
97. 
98.                     // Actualizar stock
99.                     producto.Stock -= detalleDto.Cantidad;
100.                     producto.FechaActualizacion = DateTime.UtcNow;
101. 
102.                     // Registrar movimiento de inventario
103.                     var movimiento = new MovimientoInventario
104.                     {
105.                         ProductoId = producto.Id,
106.                         TipoMovimiento = "Salida",
107.                         Cantidad = detalleDto.Cantidad,
108.                         StockAnterior = producto.Stock + detalleDto.Cantidad,
109.                         StockNuevo = producto.Stock,
110.                         FechaMovimiento = DateTime.UtcNow,
111.                         Motivo = "Venta",
112.                         Referencia = numeroVenta
113.                     };
114.                     _context.MovimientosInventario.Add(movimiento);
115.                 }
116. 
117.                 var total = subtotal - dto.Descuento + dto.Impuesto;
118. 
119.                 var venta = new Venta
120.                 {
121.                     NumeroVenta = numeroVenta,
122.                     FechaVenta = DateTime.Now,
123.                     ClienteId = dto.ClienteId,
124.                     UsuarioId = dto.UsuarioId,
125.                     Subtotal = subtotal,
126.                     Descuento = dto.Descuento,
127.                     Impuesto = dto.Impuesto,
128.                     Total = total,
129.                     MetodoPago = dto.MetodoPago,
130.                     EstadoVenta = "Completada",
131.                     Notas = dto.Notas,
132.                     DetallesVenta = detalles,
133.                     UltimaSincronizacion = DateTime.UtcNow,
134.                     Sincronizado = false,
135.                     OrigenVenta = "POS"
136.                 };
137. 
138.                 _context.Ventas.Add(venta);
139.                 await _context.SaveChangesAsync();
140.                 await transaction.CommitAsync();
141. 
142.                 return (await ObtenerPorIdAsync(venta.Id))!;
143.             }
144.             catch
145.             {
146.                 await transaction.RollbackAsync();
147.                 throw;
148.             }
149.         }
```
**Que hace:** Valida productos, descuenta stock, registra movimientos y persiste la venta en una transaccion.

**Lineas 206-225 (numero de venta diario)**
```csharp
206.         private async Task<string> GenerarNumeroVentaAsync()
207.         {
208.             var hoy = DateTime.Today;
209.             var prefijo = $"V{hoy:yyyyMMdd}";
210.             
211.             var ultimaVenta = await _context.Ventas
212.                 .Where(v => v.NumeroVenta.StartsWith(prefijo))
213.                 .OrderByDescending(v => v.NumeroVenta)
214.                 .FirstOrDefaultAsync();
215. 
216.             int siguiente = 1;
217.             if (ultimaVenta != null)
218.             {
219.                 var numero = ultimaVenta.NumeroVenta.Substring(prefijo.Length);
220.                 if (int.TryParse(numero, out int ultimoNumero))
221.                     siguiente = ultimoNumero + 1;
222.             }
223. 
224.             return $"{prefijo}{siguiente:D4}";
225.         }
```
**Que hace:** Genera folio por dia con prefijo y consecutivo.

### Archivo: `Application\Services\SincronizacionService.cs`
**Lineas 24-89 (sincronizacion principal)**
```csharp
24.         public async Task<SincronizacionResponseDto> SincronizarDatosAsync(SincronizacionDto datos)
25.         {
26.             var response = new SincronizacionResponseDto
27.             {
28.                 FechaSincronizacion = DateTime.UtcNow
29.             };
30. 
31.             using var transaction = await _context.Database.BeginTransactionAsync();
32.             try
33.             {
34.                 // Sincronizar productos
35.                 if (datos.Productos != null)
36.                 {
37.                     foreach (var productoDto in datos.Productos)
38.                     {
39.                         await SincronizarProductoAsync(productoDto);
40.                         response.ProductosSincronizados++;
41.                     }
42.                 }
43. 
44.                 // Sincronizar clientes
45.                 if (datos.Clientes != null)
46.                 {
47.                     foreach (var clienteDto in datos.Clientes)
48.                     {
49.                         await SincronizarClienteAsync(clienteDto);
50.                         response.ClientesSincronizados++;
51.                     }
52.                 }
53. 
54.                 // Sincronizar ventas
55.                 if (datos.Ventas != null)
56.                 {
57.                     foreach (var ventaDto in datos.Ventas)
58.                     {
59.                         await SincronizarVentaAsync(ventaDto);
60.                         response.VentasSincronizadas++;
61.                     }
62.                 }
63. 
64.                 // Registrar sincronización
65.                 var registro = new RegistroSincronizacion
66.                 {
67.                     Entidad = "General",
68.                     EntidadId = 0,
69.                     TipoOperacion = "SYNC",
70.                     FechaSincronizacion = DateTime.UtcNow,
71.                     DispositivoOrigen = datos.DispositivoOrigen,
72.                     Exitoso = true
73.                 };
74.                 _context.RegistrosSincronizacion.Add(registro);
75. 
76.                 await _context.SaveChangesAsync();
77.                 await transaction.CommitAsync();
78. 
79.                 response.Exitoso = true;
80.                 response.Mensaje = "Sincronización completada exitosamente";
81.             }
82.             catch (Exception ex)
83.             {
84.                 await transaction.RollbackAsync();
85.                 response.Exitoso = false;
86.                 response.Mensaje = $"Error durante la sincronización: {ex.Message}";
87.             }
88. 
89.             return response;
90.         }
```
**Que hace:** Orquesta la sincronizacion de productos, clientes y ventas dentro de una transaccion.

### Archivo: `Application\Services\SesionUsuarioService.cs`
**Lineas 30-65 (cargar sesion guardada)**
```csharp
30.         public async Task CargarSesionGuardadaAsync()
31.         {
32.             try
33.             {
34.                 var token = await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", TokenStorageKey);
35.                 var usuarioJson = await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", UsuarioStorageKey);
36. 
37.                 if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(usuarioJson))
38.                 {
39.                     LimpiarSesionEnMemoria();
40.                     return;
41.                 }
42. 
43.                 var usuario = JsonSerializer.Deserialize<LoginResponseDto>(usuarioJson);
44.                 if (usuario == null)
45.                 {
46.                     _logger.LogWarning("No se pudo deserializar la sesión de usuario guardada.");
47.                     LimpiarSesionEnMemoria();
48.                     return;
49.                 }
50. 
51.                 usuario.Token = token;
52.                 UsuarioActual = usuario;
53.                 AplicarTokenHttpClient();
54.             }
55.             catch (JSException ex)
56.             {
57.                 _logger.LogWarning(ex, "No se pudo acceder a localStorage para cargar sesión.");
58.                 LimpiarSesionEnMemoria();
59.             }
60.             catch (InvalidOperationException ex)
61.             {
62.                 _logger.LogWarning(ex, "El runtime JS no está listo para cargar sesión.");
63.                 LimpiarSesionEnMemoria();
64.             }
65.         }
```
**Que hace:** Restaura token y usuario desde localStorage para la UI.

### Archivo: `Application\Services\BalanzaService.cs`
**Lineas 9-25 (configuracion de la balanza)**
```csharp
9.     public sealed class BalanzaSettings
10.     {
11.         public bool Enabled { get; set; } = true;
12.         public bool AutoDetect { get; set; } = true;
13.         public string PortName { get; set; } = string.Empty;
14.         public int BaudRate { get; set; } = 115200;
15.         public Parity Parity { get; set; } = Parity.None;
16.         public int DataBits { get; set; } = 8;
17.         public StopBits StopBits { get; set; } = StopBits.One;
18.         public Handshake Handshake { get; set; } = Handshake.None;
19.         public string Command { get; set; } = "P";
20.         public int PollIntervalMs { get; set; } = 1000;
21.         public int ResponseTimeoutMs { get; set; } = 2000;
22.         public int RetryIntervalMs { get; set; } = 5000;
23.         public string WeightUnit { get; set; } = "kg";
24.         public decimal MinimumValidWeight { get; set; } = 0.001m;
25.     }
```
**Que hace:** Define parametros del puerto serial y criterios de lectura.

**Lineas 68-116 (loop principal del servicio)**
```csharp
68.         protected override async Task ExecuteAsync(CancellationToken stoppingToken)
69.         {
70.             if (!_settings.Enabled)
71.             {
72.                 UpdateState(false, "Balanza deshabilitada", null, null, null, null);
73.                 return;
74.             }
75. 
76.             _logger.LogInformation("BalanzaService iniciado");
77. 
78.             while (!stoppingToken.IsCancellationRequested)
79.             {
80.                 try
81.                 {
82.                     if (!Conectada)
83.                     {
84.                         var connected = await TryConnectAsync(stoppingToken);
85.                         if (!connected)
86.                         {
87.                             UpdateState(false, "No se encontró la balanza", null, null, null, null);
88.                         }
89.                     }
90.                     else
91.                     {
92.                         // Solo leer si está conectada
93.                         try
94.                         {
95.                             var lectura = await RequestWeightAsync(stoppingToken);
96.                             // No hacer nada si la lectura es nula, solo mantener conexión
97.                         }
98.                         catch
99.                         {
100.                             await DisconnectPortAsync();
101.                         }
102.                     }
103. 
104.                     await Task.Delay(_settings.PollIntervalMs, stoppingToken);
105.                 }
106.                 catch (OperationCanceledException)
107.                 {
108.                     break;
109.                 }
110.                 catch (Exception ex)
111.                 {
112.                     _logger.LogWarning(ex, "Error en BalanzaService");
113.                     await DisconnectPortAsync();
114.                     UpdateState(false, "Error de comunicación", null, null, null, null);
115.                     await Task.Delay(_settings.RetryIntervalMs, stoppingToken);
116.                 }
117.             }
```
**Que hace:** Mantiene la conexion con la balanza y lee el peso periodicamente.

## API Controllers

### Archivo: `Api\Controllers\AuthController.cs`
**Lineas 21-37 (login)**
```csharp
21.         [HttpPost("login")]
22.         [AllowAnonymous]
23.         public async Task<ActionResult<LoginResponseDto>> Login([FromBody] LoginDto dto)
24.         {
25.             try
26.             {
27.                 var response = await _authService.LoginAsync(dto);
28.                 if (response == null)
29.                     return Unauthorized(new { mensaje = "Credenciales inválidas" });
30. 
31.                 return Ok(response);
32.             }
33.             catch (Exception ex)
34.             {
35.                 _logger.LogError(ex, "Error durante el login");
36.                 return StatusCode(500, "Error interno del servidor");
37.             }
38.         }
```
**Que hace:** Autentica usuarios y devuelve JWT.

### Archivo: `Api\Controllers\ProductosController.cs`
**Lineas 118-131 (crear producto)**
```csharp
118.         [HttpPost]
119.         [Authorize(Roles = "Admin,Supervisor")]
120.         public async Task<ActionResult<ProductoDto>> Crear([FromBody] CrearProductoDto dto)
121.         {
122.             try
123.             {
124.                 var producto = await _productoService.CrearAsync(dto);
125.                 return CreatedAtAction(nameof(ObtenerPorId), new { id = producto.Id }, producto);
126.             }
127.             catch (Exception ex)
128.             {
129.                 _logger.LogError(ex, "Error al crear producto");
130.                 return StatusCode(500, ex.Message);
131.             }
132.         }
```
**Que hace:** Crea productos y exige rol Admin o Supervisor.

### Archivo: `Api\Controllers\VentasController.cs`
**Lineas 120-133 (crear venta)**
```csharp
120.         [HttpPost]
121.         public async Task<ActionResult<VentaDto>> Crear([FromBody] CrearVentaDto dto)
122.         {
123.             try
124.             {
125.                 var venta = await _ventaService.CrearAsync(dto);
126.                 return CreatedAtAction(nameof(ObtenerPorId), new { id = venta.Id }, venta);
127.             }
128.             catch (Exception ex)
129.             {
130.                 _logger.LogError(ex, "Error al crear venta");
131.                 return BadRequest(new { mensaje = ex.Message });
132.             }
133.         }
```
**Que hace:** Persiste la venta llamando al servicio de negocio.

### Archivo: `Api\Controllers\DashboardController.cs`
**Lineas 22-88 (estadisticas del dashboard)**
```csharp
22.         [HttpGet("estadisticas")]
23.         public async Task<ActionResult> ObtenerEstadisticas()
24.         {
25.             try
26.             {
27.                 var hoy = DateTime.Today;
28.                 var inicioMes = new DateTime(hoy.Year, hoy.Month, 1);
29.                 var finMes = inicioMes.AddMonths(1);
30. 
31.                 var estadisticas = new
32.                 {
33.                     ventasHoy = new
34.                     {
35.                         total = await _context.Ventas
36.                             .Where(v => v.FechaVenta >= hoy && v.FechaVenta < hoy.AddDays(1))
37.                             .Select(v => (decimal?)v.Total)
38.                             .SumAsync() ?? 0m,
39.                         cantidad = await _context.Ventas
40.                             .CountAsync(v => v.FechaVenta >= hoy && v.FechaVenta < hoy.AddDays(1))
41.                     },
42.                     ventasMes = new
43.                     {
44.                         total = await _context.Ventas
45.                             .Where(v => v.FechaVenta >= inicioMes && v.FechaVenta < finMes)
46.                             .Select(v => (decimal?)v.Total)
47.                             .SumAsync() ?? 0m,
48.                         cantidad = await _context.Ventas
49.                             .CountAsync(v => v.FechaVenta >= inicioMes && v.FechaVenta < finMes)
50.                     },
51.                     inventario = new
52.                     {
53.                         totalProductos = await _context.Productos.CountAsync(p => p.Activo),
54.                         productosStockBajo = await _context.Productos
55.                             .CountAsync(p => p.Activo && p.Stock <= p.StockMinimo),
56.                         valorInventario = await _context.Productos
57.                             .Where(p => p.Activo)
58.                             .Select(p => (decimal?)(p.Stock * p.PrecioCompra))
59.                             .SumAsync() ?? 0m
60.                     },
61.                     clientes = new
62.                     {
63.                         total = await _context.Clientes.CountAsync(c => c.Activo),
64.                         nuevosEsteMes = await _context.Clientes
65.                             .CountAsync(c => c.FechaCreacion >= inicioMes && c.FechaCreacion < finMes)
66.                     },
67.                     productosTop = await _context.DetallesVenta
68.                         .Include(d => d.Producto)
69.                         .Where(d => d.Venta!.FechaVenta >= inicioMes && d.Venta.FechaVenta < finMes)
70.                         .GroupBy(d => new { d.ProductoId, d.Producto!.Nombre })
71.                         .Select(g => new
72.                         {
73.                             productoId = g.Key.ProductoId,
74.                             nombre = g.Key.Nombre,
75.                             cantidadVendida = g.Sum(d => d.Cantidad),
76.                             totalVentas = g.Sum(d => d.Subtotal)
77.                         })
78.                         .OrderByDescending(p => (double)p.cantidadVendida)
79.                         .Take(5)
80.                         .ToListAsync()
81.                 };
82. 
83.                 return Ok(estadisticas);
84.             }
85.             catch (Exception ex)
86.             {
87.                 _logger.LogError(ex, "Error al obtener estadísticas del dashboard");
88.                 return StatusCode(500, "Error interno del servidor");
89.             }
90.         }
```
**Que hace:** Calcula métricas del dashboard desde la base de datos.

### Archivo: `Api\Controllers\BalanzaController.cs`
**Lineas 19-52 (lectura del peso)**
```csharp
19.         [HttpGet("leer")]
20.         public IActionResult LeerPeso()
21.         {
22.             try
23.             {
24.                 if (!_balanzaService.Conectada || !_balanzaService.PesoActual.HasValue)
25.                 {
26.                     return StatusCode(503, new
27.                     {
28.                         error = "Balanza no disponible",
29.                         exito = false,
30.                         estado = _balanzaService.Estado,
31.                         puerto = _balanzaService.PuertoActual
32.                     });
33.                 }
34. 
35.                 var response = new
36.                 {
37.                     peso = _balanzaService.PesoActual.Value,
38.                     unidad = _balanzaService.UnidadMedida,
39.                     lectura_cruda = _balanzaService.LecturaCruda,
40.                     estado = _balanzaService.Estado,
41.                     puerto = _balanzaService.PuertoActual,
42.                     ultima_lectura = _balanzaService.UltimaLectura,
43.                     exito = true
44.                 };
45. 
46.                 return Ok(response);
47.             }
48.             catch (Exception ex)
49.             {
50.                 _logger.LogError(ex, "Error al leer balanza");
51.                 return StatusCode(500, new { error = ex.Message });
52.             }
53.         }
```
**Que hace:** Expone el peso actual de la balanza via API.

## UI (Blazor) - Logica funcional

### Archivo: `Views\Auth\Login.razor`
**Lineas 55-103 (flujo de login)**
```csharp
55.     protected override async Task OnAfterRenderAsync(bool firstRender)
56.     {
57.         if (!firstRender)
58.         {
59.             return;
60.         }
61. 
62.         await SesionUsuario.CargarSesionGuardadaAsync();
63.         if (SesionUsuario.EstaAutenticado)
64.         {
65.             Navigation.NavigateTo("/dashboard");
66.             return;
67.         }
68. 
69.         verificandoSesion = false;
70.         await InvokeAsync(StateHasChanged);
71.     }
72. 
73.     private async Task IniciarSesion()
74.     {
75.         mensajeError = null;
76. 
77.         if (string.IsNullOrWhiteSpace(login.NombreUsuario) || string.IsNullOrWhiteSpace(login.Password))
78.         {
79.             mensajeError = "Debes capturar usuario y contraseña.";
80.             return;
81.         }
82. 
83.         autenticando = true;
84.         try
85.         {
86.             var (exitoso, mensaje) = await SesionUsuario.IniciarSesionAsync(login);
87.             if (!exitoso)
88.             {
89.                 mensajeError = mensaje;
90.                 return;
91.             }
92. 
93.             Navigation.NavigateTo("/dashboard");
94.         }
95.         catch (HttpRequestException ex)
96.         {
97.             mensajeError = $"No se pudo conectar al servidor de autenticación: {ex.Message}";
98.         }
99.         finally
100.         {
101.             autenticando = false;
102.         }
103.     }
```
**Que hace:** Valida campos, llama a la API de auth y redirige al dashboard.

### Archivo: `Views\Ventas\Nuevo.razor`
**Lineas 1377-1395 (suscripcion a eventos de balanza)**
```csharp
1377.     protected override void OnInitialized()
1378.     {
1379.         BalanzaService.EstadoChanged += ManejarCambioBalanza;
1380.     }
1381. 
1382.     protected override async Task OnInitializedAsync()
1383.     {
1384.         await CargarDatos();
1385.     }
1386. 
1387.     private void ManejarCambioBalanza()
1388.     {
1389.         if (mostrarModalPeso && productoPesoSeleccionado != null && modoCapturaPeso == "balanza" && BalanzaService.PesoActual.HasValue)
1390.         {
1391.             pesoCaptura = BalanzaService.PesoActual.Value.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture);
1392.         }
1393. 
1394.         _ = InvokeAsync(StateHasChanged);
1395.     }
```
**Que hace:** Escucha cambios de la balanza y actualiza el peso en la UI.

**Lineas 1470-1499 (agregar al carrito)**
```csharp
1470.     private void AgregarAlCarrito(ProductoDto producto)
1471.     {
1472.         if (producto.Stock <= 0)
1473.         {
1474.             return;
1475.         }
1476. 
1477.         if (producto.EsPorPeso)
1478.         {
1479.             productoPesoSeleccionado = producto;
1480.             pesoCaptura = BalanzaService.PesoActual.HasValue
1481.                 ? BalanzaService.PesoActual.Value.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture)
1482.                 : "1.000";
1483.             modoCapturaPeso = BalanzaService.Conectada ? "balanza" : "manual";
1484.             mostrarModalPeso = true;
1485.             return;
1486.         }
1487. 
1488.         var item = carrito.FirstOrDefault(x => x.Producto.Id == producto.Id);
1489.         if (item != null)
1490.         {
1491.             if (item.Cantidad < producto.Stock)
1492.             {
1493.                 item.Cantidad += 1m;
1494.             }
1495.         }
1496.         else
1497.         {
1498.             carrito.Add(new ItemCarrito { Producto = producto, Cantidad = 1m });
1499.         }
1500.     }
```
**Que hace:** Agrega productos al carrito y abre modal si es por peso.

**Lineas 1522-1561 (confirmar peso)**
```csharp
1522.     private async Task ConfirmarPeso()
1523.     {
1524.         if (productoPesoSeleccionado == null)
1525.         {
1526.             return;
1527.         }
1528. 
1529.         if (!decimal.TryParse(pesoCaptura, System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.CurrentCulture, out var peso)
1530.             && !decimal.TryParse(pesoCaptura, System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out peso))
1531.         {
1532.             await JS.InvokeVoidAsync("alert", "Ingrese un peso válido");
1533.             return;
1534.         }
1535. 
1536.         if (peso <= 0)
1537.         {
1538.             await JS.InvokeVoidAsync("alert", "El peso debe ser mayor a cero");
1539.             return;
1540.         }
1541. 
1542.         if (peso > productoPesoSeleccionado.Stock)
1543.         {
1544.             await JS.InvokeVoidAsync("alert", "Peso insuficiente en inventario");
1545.             return;
1546.         }
1547. 
1548.         var item = carrito.FirstOrDefault(x => x.Producto.Id == productoPesoSeleccionado.Id);
1549.         if (item != null)
1550.         {
1551.             item.Cantidad += peso;
1552.         }
1553.         else
1554.         {
1555.             carrito.Add(new ItemCarrito { Producto = productoPesoSeleccionado, Cantidad = peso });
1556.         }
1557. 
1558.         CerrarModalPeso();
1559.         ActualizarCarrito();
1560.         await Task.CompletedTask;
1561.     }
```
**Que hace:** Valida el peso y agrega el item al carrito.

**Lineas 1629-1678 (procesar venta)**
```csharp
1629.     private async Task ProcesarVenta()
1630.     {
1631.         if (carrito.Count == 0)
1632.         {
1633.             await JS.InvokeVoidAsync("alert", "Agregue productos al carrito");
1634.             return;
1635.         }
1636. 
1637.         procesando = true;
1638.         try
1639.         {
1640.             var ventaDto = new CrearVentaDto
1641.             {
1642.                 ClienteId = clienteSeleccionado,
1643.                 UsuarioId = 1,
1644.                 MetodoPago = metodoPago,
1645.                 Descuento = descuento,
1646.                 Impuesto = iva,
1647.                 Notas = notas,
1648.                 Detalles = carrito.Select(x => new CrearDetalleVentaDto
1649.                 {
1650.                     ProductoId = x.Producto.Id,
1651.                     Cantidad = x.Cantidad,
1652.                     PrecioUnitario = x.Producto.PrecioVenta,
1653.                     Descuento = 0
1654.                 }).ToList()
1655.             };
1656. 
1657.             await VentaService.CrearAsync(ventaDto);
1658.             await JS.InvokeVoidAsync("alert", $"✅ Venta procesada exitosamente.\nTotal: ${total:N0}");
1659.             
1660.             carrito.Clear();
1661.             descuento = 0;
1662.             notas = "";
1663.             aplicarImpuesto = false;
1664.             porcentajeImpuesto = 0m;
1665.             
1666.             Navigation.NavigateTo("/ventas");
1667.         }
1668.         catch (HttpRequestException ex)
1669.         {
1670.             await JS.InvokeVoidAsync("alert", $"❌ Error de conexión: {ex.Message}");
1671.         }
1672.         catch (Exception ex)
1673.         {
1674.             Console.WriteLine($"Error en ProcesarVenta: {ex}");
1675.             await JS.InvokeVoidAsync("alert", $"❌ Error al guardar: {ex.InnerException?.Message ?? ex.Message}");
1676.         }
1677.         finally
1678.         {
1679.             procesando = false;
1680.         }
1681.     }
```
**Que hace:** Construye el DTO, llama al servicio de ventas y limpia el carrito.
