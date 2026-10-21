using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using PuntoAba.Data;
using PuntoAba.DTOs;
using PuntoAba.Models;
namespace PuntoAba.Services;

public class CategoriaService(DbContextOptions<PuntoAbaDbContext> opciones, IAccesoOperacion acceso) : ICategoriaService
{
    private static CategoriaDto Mapear(Categoria c) => new()
    {
        Id = c.Id, Nombre = c.Nombre, Descripcion = c.Descripcion, Activo = c.Activo,
        CantidadProductos = c.Productos.Count(p => p.Activo)
    };
    public async Task<List<CategoriaDto>> ObtenerTodasAsync()
    {
        await acceso.ExigirAsync("Admin", "Supervisor", "Cajero");
        await using var db = new PuntoAbaDbContext(opciones);
        return (await db.Categorias.AsNoTracking().Include(c => c.Productos).Where(c => c.Activo).OrderBy(c => c.Nombre).ToListAsync()).Select(Mapear).ToList();
    }
    public async Task<CategoriaDto?> ObtenerPorIdAsync(int id)
    {
        await acceso.ExigirAsync("Admin", "Supervisor", "Cajero");
        await using var db = new PuntoAbaDbContext(opciones);
        var c = await db.Categorias.AsNoTracking().Include(c => c.Productos).FirstOrDefaultAsync(c => c.Id == id);
        return c == null ? null : Mapear(c);
    }
    public async Task<CategoriaDto> CrearAsync(CrearCategoriaDto dto)
    {
        await acceso.ExigirAsync("Admin", "Supervisor");
        await using var db = new PuntoAbaDbContext(opciones);
        var c = new Categoria { Nombre = dto.Nombre.Trim(), Descripcion = dto.Descripcion?.Trim() };
        Validar(c); db.Categorias.Add(c); await db.SaveChangesAsync(); return Mapear(c);
    }
    public async Task<CategoriaDto?> ActualizarAsync(int id, ActualizarCategoriaDto dto)
    {
        await acceso.ExigirAsync("Admin", "Supervisor");
        await using var db = new PuntoAbaDbContext(opciones);
        var c = await db.Categorias.Include(c => c.Productos).FirstOrDefaultAsync(c => c.Id == id); if (c == null) return null;
        if (dto.Nombre != null) c.Nombre = dto.Nombre.Trim();
        if (dto.Descripcion != null) c.Descripcion = dto.Descripcion.Trim();
        if (dto.Activo.HasValue)
        {
            if (!dto.Activo.Value) { await acceso.ExigirAsync("Admin"); ExigirSinProductos(c); }
            c.Activo = dto.Activo.Value;
        }
        Validar(c); c.FechaActualizacion = DateTime.UtcNow; await db.SaveChangesAsync(); return Mapear(c);
    }
    public async Task<bool> EliminarAsync(int id)
    {
        await acceso.ExigirAsync("Admin");
        await using var db = new PuntoAbaDbContext(opciones);
        var c = await db.Categorias.Include(c => c.Productos).FirstOrDefaultAsync(c => c.Id == id); if (c == null) return false;
        ExigirSinProductos(c); c.Activo = false; c.FechaActualizacion = DateTime.UtcNow; await db.SaveChangesAsync(); return true;
    }
    private static void ExigirSinProductos(Categoria c)
    {
        if (c.Productos.Any(p => p.Activo)) throw new ArgumentException("No se puede eliminar una categoría con productos activos.");
    }
    private static void Validar(Categoria c)
    {
        if (string.IsNullOrWhiteSpace(c.Nombre)) throw new ArgumentException("Escribe el nombre de la categoría.");
        Validator.ValidateObject(c, new ValidationContext(c), true);
    }
}
