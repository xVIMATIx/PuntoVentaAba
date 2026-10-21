namespace PuntoAba.Presentation.Navegacion;

public record AccesoPantalla(string Titulo, string Ruta, string Icono, string Grupo);
public static class MenuAplicacion
{
    public static readonly IReadOnlyList<AccesoPantalla> Pantallas = new AccesoPantalla[]
    {
        new("Nueva venta", "ventas/nuevo", "venta", "Ventas"),
        new("Inventario", "inventario", "caja", "Inventario"),
        new("Productos", "productos", "caja", "Inventario"),
        new("Categorías", "categorias", "lista", "Inventario"),
        new("Entradas de mercancía", "inventario/entradas", "entrada", "Inventario"),
        new("Resurtido", "inventario/resurtido", "resurtido", "Inventario"),
        new("Correcciones de inventario", "inventario/correcciones", "ajustes", "Inventario"),
        new("Mermas y devoluciones", "inventario/mermas", "devolucion", "Inventario"),
        new("Ventas realizadas", "ventas", "historial", "Historial"),
        new("Movimientos y correcciones", "inventario/movimientos", "historial", "Historial"),
        new("Reportes de ventas", "reportes", "grafica", "Historial"),
        new("Reportes de inventario", "inventario/reportes", "grafica", "Historial"),
        new("Clientes", "clientes", "clientes", "Clientes"),
        new("Usuarios", "usuarios", "clientes", "Administración"),
        new("Respaldos", "inventario/respaldos", "respaldo", "Administración"),
        new("Sincronización", "sincronizacion", "sincronizar", "Administración")
    };
    public static bool EsInventario(string ruta)
    {
        ruta = ruta.Split('?')[0].Trim('/').ToLowerInvariant();
        return EsCorrecciones(ruta) || Pantallas.Any(p => p.Grupo == "Inventario" && p.Ruta == ruta);
    }
    public static string Grupo(string ruta)
    {
        ruta = ruta.Split('?')[0].Trim('/').ToLowerInvariant();
        if (EsCorrecciones(ruta)) return "Inventario";
        return Pantallas.FirstOrDefault(p => p.Ruta == ruta)?.Grupo ?? "";
    }
    public static bool EsHistorial(string ruta) => Grupo(ruta) == "Historial";
    public static bool EsCorrecciones(string ruta) => ruta.Split('?')[0].Trim('/').ToLowerInvariant() is "inventario/correcciones" or "inventario/conteos" or "inventario/ajustes";
    public static bool PuedeAbrir(string ruta, string? rol)
    {
        ruta = ruta.Split('?')[0].Trim('/').ToLowerInvariant();
        if (rol is not ("Admin" or "Supervisor" or "Cajero")) return false;
        if (ruta is "usuarios" or "inventario/respaldos") return rol == "Admin";
        if (ruta is "dashboard" or "inicio" or "inventario" or "inventario/conteos" or "inventario/correcciones" || ruta.StartsWith("ventas")) return true;
        return rol is "Admin" or "Supervisor";
    }
    public static string Titulo(string ruta)
    {
        ruta = ruta.Split('?')[0].Trim('/').ToLowerInvariant();
        if (ruta is "dashboard" or "inicio") return "Inicio";
        if (EsCorrecciones(ruta)) return "Correcciones de inventario";
        if (ruta == "usuarios") return "Usuarios";
        if (ruta == "inventario/respaldos") return "Respaldos";
        if (ruta == "sincronizacion") return "Sincronización";
        return Pantallas.FirstOrDefault(p => p.Ruta == ruta)?.Titulo ?? "Abarrotes Agustín";
    }
}
