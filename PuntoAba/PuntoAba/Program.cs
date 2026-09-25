using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using PuntoAba.Data;
using PuntoAba.Services;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Configuración de base de datos
builder.Services.AddDbContext<PuntoAbaDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    
    // Si no hay connection string configurada, usar SQLite para desarrollo
    if (string.IsNullOrEmpty(connectionString))
    {
        connectionString = "Data Source=PuntoAba.db";
    }
    
    options.UseSqlite(connectionString);
});

// Configuración de autenticación JWT
var jwtKey = builder.Configuration["Jwt:Key"] ?? "PuntoAba_SecretKey_MuySegura_2026_Change_This_In_Production";
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "PuntoAba";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "PuntoAbaClients";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
    };
});

builder.Services.AddAuthorization();

// Configuración de CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Registro de servicios
builder.Services.AddScoped<IProductoService, ProductoService>();
builder.Services.AddScoped<IVentaService, VentaService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ISincronizacionService, SincronizacionService>();
builder.Services.Configure<BalanzaSettings>(builder.Configuration.GetSection("Balanza"));
builder.Services.AddSingleton<BalanzaService>();
builder.Services.AddSingleton<IBalanzaService>(sp => sp.GetRequiredService<BalanzaService>());
builder.Services.AddHostedService(sp => sp.GetRequiredService<BalanzaService>());

// Configuración de Blazor Server
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

// Registrar HttpClient
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.Configuration["ApiBaseUrl"] ?? "http://localhost:5000") });
builder.Services.AddScoped<SesionUsuarioService>();

// Configuración de controladores
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
    });

builder.Services.AddEndpointsApiExplorer();

// Configuración de Swagger con autenticación JWT
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "PuntoAba API",
        Version = "v1",
        Description = "API para sistema de punto de venta de tienda de abarrotes",
        Contact = new OpenApiContact
        {
            Name = "PuntoAba",
            Email = "contacto@puntoaba.com"
        }
    });

    // Configurar autenticación JWT en Swagger
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Autenticación JWT. Ingrese 'Bearer' seguido de un espacio y el token JWT.\r\n\r\nEjemplo: 'Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...'",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// Aplicar migraciones automáticamente en desarrollo
if (app.Environment.IsDevelopment())
{
    using (var scope = app.Services.CreateScope())
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<PuntoAbaDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        try
        {
            dbContext.Database.Migrate();
            AsegurarUsuarioAdminDesarrollo(dbContext, logger);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "No se pudieron aplicar las migraciones automáticamente. Ejecute 'dotnet ef database update' manualmente.");
        }
    }
}

// Configuración del pipeline HTTP
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "PuntoAba API v1");
        c.RoutePrefix = "api"; // Swagger en /api
    });
}

// app.UseHttpsRedirection();  // Deshabilitado para desarrollo

app.UseStaticFiles();

app.UseCors("AllowAll");

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapRazorPages();
app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

// Endpoint informativo de la API
app.MapGet("/api/info", () => Results.Ok(new {
    mensaje = "PuntoAba API - Balanza Operativa",
    endpoints = new {
        balanza = "/api/balanza/leer",
        clientes = "/api/clientes",
        productos = "/api/productos",
        swagger = "/api"
    }
}));

// Endpoint de salud
app.MapGet("/health", () => Results.Ok(new
{
    status = "Healthy",
    timestamp = DateTime.UtcNow,
    version = "1.0.0"
}))
.WithName("HealthCheck")
.WithTags("Health");

app.Run();

static void AsegurarUsuarioAdminDesarrollo(PuntoAbaDbContext dbContext, ILogger logger)
{
    const string usuarioAdmin = "admin";
    const string emailAdmin = "admin@puntoaba.com";
    const string passwordAdmin = "Admin123";
    const string hashPlaceholder = "$2a$11$9vJZ5qZ5qZ5qZ5qZ5qZ5qOxK3kZ4kZ4kZ4kZ4kZ4kZ4kZ4kZ4kZ4m";

    var admin = dbContext.Usuarios.FirstOrDefault(u => u.NombreUsuario == usuarioAdmin);
    if (admin == null)
    {
        dbContext.Usuarios.Add(new PuntoAba.Models.Usuario
        {
            NombreUsuario = usuarioAdmin,
            NombreCompleto = "Administrador del Sistema",
            Email = emailAdmin,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(passwordAdmin),
            Rol = "Admin",
            Activo = true,
            FechaCreacion = DateTime.UtcNow
        });
        dbContext.SaveChanges();
        logger.LogInformation("Se creó usuario admin de desarrollo.");
        return;
    }

    if (admin.PasswordHash == hashPlaceholder)
    {
        admin.PasswordHash = BCrypt.Net.BCrypt.HashPassword(passwordAdmin);
        admin.Activo = true;
        admin.Rol = "Admin";
        admin.FechaActualizacion = DateTime.UtcNow;
        dbContext.SaveChanges();
        logger.LogInformation("Se actualizó hash placeholder del usuario admin de desarrollo.");
    }
}
