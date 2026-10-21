using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using PuntoAba.Data;
using PuntoAba.DTOs;

namespace PuntoAba.Services;
public interface IAccesoOperacion { Task<UsuarioDto> ExigirAsync(params string[] roles); }
public class IdentidadToken(DbContextOptions<PuntoAbaDbContext> opciones, IConfiguration configuracion)
{
    public async Task<UsuarioDto> ValidarAsync(string token)
    {
        ClaimsPrincipal principal;
        try
        {
            principal = new JwtSecurityTokenHandler().ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuer = true, ValidateAudience = true, ValidateLifetime = true, ValidateIssuerSigningKey = true,
                ValidIssuer = configuracion["Jwt:Issuer"] ?? "PuntoAba", ValidAudience = configuracion["Jwt:Audience"] ?? "PuntoAbaClients",
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuracion["Jwt:Key"] ?? throw new InvalidOperationException("Configura Jwt__Key mediante una variable de entorno."))),
                ValidAlgorithms = new[] { SecurityAlgorithms.HmacSha256 }, ClockSkew = TimeSpan.FromSeconds(30)
            }, out _);
        }
        catch (Exception ex) when (ex is SecurityTokenException or ArgumentException)
        { throw new UnauthorizedAccessException("Tu sesión venció o no es válida. Inicia sesión nuevamente."); }
        return await ObtenerAsync(principal);
    }
    public async Task<UsuarioDto> ObtenerAsync(ClaimsPrincipal principal)
    {
        if (!int.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var id)) throw new UnauthorizedAccessException("Inicia sesión para continuar.");
        await using var db = new PuntoAbaDbContext(opciones);
        var u = await db.Usuarios.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id && u.Activo) ?? throw new UnauthorizedAccessException("El usuario ya no está activo.");
        return new UsuarioDto { Id = u.Id, NombreUsuario = u.NombreUsuario, NombreCompleto = u.NombreCompleto, Email = u.Email, Rol = u.Rol, Activo = true };
    }
}
public class AccesoOperacion(IHttpContextAccessor http, SesionUsuarioService sesion, IdentidadToken identidad) : IAccesoOperacion
{
    public async Task<UsuarioDto> ExigirAsync(params string[] roles)
    {
        var u = http.HttpContext?.User.Identity?.IsAuthenticated == true ? await identidad.ObtenerAsync(http.HttpContext.User)
            : !string.IsNullOrWhiteSpace(sesion.Token) ? await identidad.ValidarAsync(sesion.Token) : throw new UnauthorizedAccessException("Inicia sesión para continuar.");
        if (roles.Length > 0 && !roles.Contains(u.Rol)) throw new UnauthorizedAccessException("Tu usuario no tiene permiso para esta operación.");
        return u;
    }
}
