using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Hosting;
using PuntoAba.Data;
using PuntoAba.DTOs;
using PuntoAba.Models;
using PuntoAba.Services;
using PuntoAba.Presentation.Ventas;

static class ModularChecks
{
    private static void Verificar(bool condicion, string texto) { if (!condicion) throw new Exception(texto); Console.WriteLine("OK: " + texto); }
    private static async Task Rechazar<T>(Func<Task> f) where T:Exception
    {
        try { await f(); } catch(T) { return; }
        throw new Exception("Se esperaba " + typeof(T).Name);
    }
    public static async Task RunAsync(DbContextOptions<PuntoAbaDbContext> opciones, IWebHostEnvironment entorno)
    {
        var actor = new Permiso();
        var clientes = new ClienteService(opciones, actor);
        var categorias = new CategoriaService(opciones, actor);
        await Rechazar<ArgumentException>(() => clientes.CrearAsync(new() { Nombre = "   " }));
        await Rechazar<System.ComponentModel.DataAnnotations.ValidationException>(() => clientes.CrearAsync(new() { Nombre = "Cliente", Email = "invalido" }));
        var c = await clientes.CrearAsync(new() { Nombre = "Ana", Apellido = "Prueba", Telefono = "555" });
        var copia = await clientes.ObtenerPorIdAsync(c.Id); copia!.Nombre = "Sin guardar";
        Verificar((await clientes.ObtenerPorIdAsync(c.Id))!.Nombre == "Ana", "Editar o cancelar un DTO no altera datos guardados");
        await clientes.ActualizarAsync(c.Id, new() { Nombre = "Ana editada", Telefono = "" });
        Verificar((await clientes.ObtenerTodosAsync("editada")).Any(x => x.Id == c.Id), "Clientes reutiliza creación, búsqueda y actualización fuera de la pantalla");
        var cat = await categorias.CrearAsync(new() { Nombre = "Temporal de prueba" });
        await categorias.ActualizarAsync(cat.Id, new() { Descripcion = "Revisada" });
        await Rechazar<ArgumentException>(() => categorias.EliminarAsync(1));
        await Rechazar<ArgumentException>(() => categorias.ActualizarAsync(1, new() { Activo = false }));
        Verificar((await categorias.ObtenerPorIdAsync(1))!.Activo, "La API y las pantallas no pueden desactivar categorías con productos activos");
        actor.Rol = "Cajero";
        await clientes.ObtenerTodosAsync(); await categorias.ObtenerTodasAsync();
        await Rechazar<UnauthorizedAccessException>(() => clientes.CrearAsync(new() { Nombre = "No autorizado" }));
        await Rechazar<UnauthorizedAccessException>(() => categorias.CrearAsync(new() { Nombre = "No autorizado" }));
        actor.Rol = "Supervisor";
        await Rechazar<UnauthorizedAccessException>(() => clientes.EliminarAsync(c.Id));
        await Rechazar<UnauthorizedAccessException>(() => categorias.EliminarAsync(cat.Id));
        actor.Rol = "Admin";
        await clientes.EliminarAsync(c.Id); await categorias.EliminarAsync(cat.Id);
        Verificar(!(await clientes.ObtenerTodosAsync()).Any(x => x.Id == c.Id) && !(await categorias.ObtenerTodasAsync()).Any(x => x.Id == cat.Id), "Bajas lógicas y permisos de catálogos se aplican desde servicios");
        var estado = new CarritoVenta();
        var piezas = new ProductoDto { Id = 1, Stock = 2, PrecioVenta = 5 };
        var peso = new ProductoDto { Id = 2, Stock = 1, EsPorPeso = true, PrecioVenta = 10 };
        Verificar(estado.Agregar(piezas, 1) && !estado.Agregar(piezas, .5m) && estado.Agregar(piezas, 1) && estado.Agregar(piezas, 1), "Carrito valida piezas enteras y permite superar existencias");
        Verificar(estado.Agregar(peso, .125m) && !estado.CambiarCantidad(2, .1234m) && !estado.CambiarCantidad(1, -1) && estado.Subtotal == 16.25m, "Carrito valida peso y calcula subtotal sin guardar movimientos");
        Verificar(new CarritoVenta().Items.Count == 0, "Cada venta conserva su propio estado");
        var dashboard = await new DashboardService(opciones, actor).ObtenerEstadisticasAsync();
        Verificar(dashboard.VentasHoy.Cantidad == 1 && dashboard.Inventario.TotalProductos == 3, "Dashboard comparte un contrato tipado entre API y Blazor");
        await using var invalida = new MemoryStream(System.Text.Encoding.UTF8.GetBytes("<script>prueba</script>"));
        var imagenes = new ImagenProductoService(entorno, actor);
        await Rechazar<ArgumentException>(() => imagenes.GuardarAsync(invalida, "image/png"));
        await using var png = new MemoryStream(Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVQIHWP4z8DwHwAFgAI/ScLbtAAAAABJRU5ErkJggg=="));
        var url = await imagenes.GuardarAsync(png, "image/png");
        Verificar(url.EndsWith(".png") && File.Exists(Path.Combine(entorno.WebRootPath, url.TrimStart('/'))), "Las imágenes se guardan desde un servicio con validación de formato");
    }
    public static async Task PrepararApiAsync(string directorio)
    {
        Directory.CreateDirectory(directorio);
        var archivo = Path.Combine(Path.GetFullPath(directorio), "api.db");
        if (File.Exists(archivo)) throw new Exception("La carpeta de pruebas ya contiene una base.");
        var opciones = new DbContextOptionsBuilder<PuntoAbaDbContext>().UseSqlite("Data Source=" + archivo).Options;
        await using var db = new PuntoAbaDbContext(opciones); await db.Database.MigrateAsync();
        var admin = await db.Usuarios.SingleAsync(u => u.Id == 1); admin.PasswordHash = BCrypt.Net.BCrypt.HashPassword("Pruebas123"); admin.Rol = "Admin"; admin.Activo = true;
        db.Usuarios.Add(new() { Id = 2, NombreUsuario = "cajero-api", NombreCompleto = "Prueba API", Email = "api@example.test", PasswordHash = BCrypt.Net.BCrypt.HashPassword("Pruebas123"), Rol = "Cajero" });
        db.Productos.AddRange(new Producto { Id = 101, Codigo = "00111", Nombre = "Piezas API", CategoriaId = 1, Stock = 10, StockMinimo = 12, StockObjetivo = 24, PrecioCompra = 2, PrecioVenta = 5 },
            new Producto { Id = 102, Codigo = "00222", Nombre = "Peso API", CategoriaId = 1, Stock = 5, StockMinimo = 6, EsPorPeso = true, UnidadMedida = "Kg", PrecioCompra = 3, PrecioVenta = 7 });
        await db.SaveChangesAsync(); Console.WriteLine("Fixture API creada: " + archivo);
    }
    private class Permiso : IAccesoOperacion
    {
        public string Rol = "Admin";
        public Task<UsuarioDto> ExigirAsync(params string[] roles)
        {
            if (!roles.Contains(Rol)) throw new UnauthorizedAccessException("Sin permiso");
            return Task.FromResult(new UsuarioDto { Id = 1, Rol = Rol, Activo = true });
        }
    }
}
