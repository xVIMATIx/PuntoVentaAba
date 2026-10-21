using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using PuntoAba.Data;
using PuntoAba.DTOs;
using PuntoAba.Models;
namespace PuntoAba.Services;

public class ClienteService(DbContextOptions<PuntoAbaDbContext> opciones, IAccesoOperacion acceso) : IClienteService
{
    private static ClienteDto Mapear(Cliente c) => new()
    {
        Id = c.Id, Nombre = c.Nombre, Apellido = c.Apellido, Telefono = c.Telefono,
        Email = c.Email, Direccion = c.Direccion, DocumentoIdentidad = c.DocumentoIdentidad, Activo = c.Activo
    };
    public async Task<List<ClienteDto>> ObtenerTodosAsync(string? termino = null)
    {
        await acceso.ExigirAsync("Admin", "Supervisor", "Cajero");
        await using var db = new PuntoAbaDbContext(opciones);
        var clientes = await db.Clientes.AsNoTracking().Where(c => c.Activo).OrderBy(c => c.Nombre).ThenBy(c => c.Id).ToListAsync();
        if (!string.IsNullOrWhiteSpace(termino))
            clientes = clientes.Where(c => new[] { c.Nombre, c.Apellido, c.DocumentoIdentidad, c.Telefono }
                .Any(t => t?.Contains(termino.Trim(), StringComparison.OrdinalIgnoreCase) == true)).ToList();
        return clientes.Select(Mapear).ToList();
    }
    public async Task<ClienteDto?> ObtenerPorIdAsync(int id)
    {
        await acceso.ExigirAsync("Admin", "Supervisor", "Cajero");
        await using var db = new PuntoAbaDbContext(opciones);
        var c = await db.Clientes.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id);
        return c == null ? null : Mapear(c);
    }
    public async Task<ClienteDto> CrearAsync(CrearClienteDto dto)
    {
        await acceso.ExigirAsync("Admin", "Supervisor");
        await using var db = new PuntoAbaDbContext(opciones);
        var c = new Cliente { Nombre = dto.Nombre.Trim(), Apellido = dto.Apellido?.Trim(), Telefono = dto.Telefono?.Trim(),
            Email = string.IsNullOrWhiteSpace(dto.Email) ? null : dto.Email.Trim(), Direccion = dto.Direccion?.Trim(),
            DocumentoIdentidad = dto.DocumentoIdentidad?.Trim() };
        Validar(c); db.Clientes.Add(c); await db.SaveChangesAsync(); return Mapear(c);
    }
    public async Task<ClienteDto?> ActualizarAsync(int id, ActualizarClienteDto dto)
    {
        await acceso.ExigirAsync("Admin", "Supervisor");
        await using var db = new PuntoAbaDbContext(opciones);
        var c = await db.Clientes.FindAsync(id); if (c == null) return null;
        if (dto.Nombre != null) c.Nombre = dto.Nombre.Trim();
        if (dto.Apellido != null) c.Apellido = dto.Apellido.Trim();
        if (dto.Telefono != null) c.Telefono = dto.Telefono.Trim();
        if (dto.Email != null) c.Email = string.IsNullOrWhiteSpace(dto.Email) ? null : dto.Email.Trim();
        if (dto.Direccion != null) c.Direccion = dto.Direccion.Trim();
        if (dto.DocumentoIdentidad != null) c.DocumentoIdentidad = dto.DocumentoIdentidad.Trim();
        if (dto.Activo.HasValue)
        {
            if (!dto.Activo.Value) await acceso.ExigirAsync("Admin");
            c.Activo = dto.Activo.Value;
        }
        Validar(c); c.FechaActualizacion = DateTime.UtcNow; await db.SaveChangesAsync(); return Mapear(c);
    }
    public async Task<bool> EliminarAsync(int id)
    {
        await acceso.ExigirAsync("Admin");
        await using var db = new PuntoAbaDbContext(opciones);
        var c = await db.Clientes.FindAsync(id); if (c == null) return false;
        c.Activo = false; c.FechaActualizacion = DateTime.UtcNow; await db.SaveChangesAsync(); return true;
    }
    private static void Validar(Cliente c)
    {
        if (string.IsNullOrWhiteSpace(c.Nombre)) throw new ArgumentException("Escribe el nombre del cliente.");
        Validator.ValidateObject(c, new ValidationContext(c), true);
    }
}
