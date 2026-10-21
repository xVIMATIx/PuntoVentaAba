using Microsoft.EntityFrameworkCore;
using PuntoAba.Data;
using PuntoAba.DTOs;
using PuntoAba.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace PuntoAba.Services
{
    public class AuthService : IAuthService
    {
        private readonly PuntoAbaDbContext _context;
        private readonly IConfiguration _configuration;

        public AuthService(PuntoAbaDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        public async Task<LoginResponseDto?> LoginAsync(LoginDto dto)
        {
            var usuario = await _context.Usuarios
                .FirstOrDefaultAsync(u => u.NombreUsuario == dto.NombreUsuario && u.Activo);

            if (usuario == null || !BCrypt.Net.BCrypt.Verify(dto.Password, usuario.PasswordHash))
                return null;

            // Actualizar último acceso
            usuario.UltimoAcceso = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            var token = GenerarToken(usuario);

            return new LoginResponseDto
            {
                Id = usuario.Id,
                NombreUsuario = usuario.NombreUsuario,
                NombreCompleto = usuario.NombreCompleto,
                Email = usuario.Email,
                Rol = usuario.Rol,
                Token = token
            };
        }

        public async Task<UsuarioDto> CrearUsuarioAsync(CrearUsuarioDto dto)
        {
            // Verificar que no exista el usuario
            if (await _context.Usuarios.AnyAsync(u => u.NombreUsuario == dto.NombreUsuario))
                throw new Exception("El nombre de usuario ya existe");

            if (await _context.Usuarios.AnyAsync(u => u.Email == dto.Email))
                throw new Exception("El email ya está registrado");

            var usuario = new Usuario
            {
                NombreUsuario = dto.NombreUsuario,
                NombreCompleto = dto.NombreCompleto,
                Email = dto.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                Rol = dto.Rol,
                Telefono = dto.Telefono,
                Activo = true,
                FechaCreacion = DateTime.UtcNow
            };

            _context.Usuarios.Add(usuario);
            await _context.SaveChangesAsync();

            return MapToDto(usuario);
        }

        public async Task<bool> CambiarPasswordAsync(int usuarioId, CambiarPasswordDto dto)
        {
            var usuario = await _context.Usuarios.FindAsync(usuarioId);
            if (usuario == null)
                return false;

            if (!BCrypt.Net.BCrypt.Verify(dto.PasswordActual, usuario.PasswordHash))
                return false;

            usuario.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.PasswordNueva);
            usuario.FechaActualizacion = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<UsuarioDto?> ObtenerUsuarioPorIdAsync(int id)
        {
            var usuario = await _context.Usuarios.FindAsync(id);
            return usuario != null ? MapToDto(usuario) : null;
        }

        public async Task<IEnumerable<UsuarioDto>> ObtenerTodosUsuariosAsync()
        {
            var usuarios = await _context.Usuarios
                .Where(u => u.Activo)
                .ToListAsync();

            return usuarios.Select(MapToDto);
        }

        public async Task<UsuarioDto?> ActualizarUsuarioAsync(int id, ActualizarUsuarioDto dto)
        {
            var usuario = await _context.Usuarios.FindAsync(id);
            if (usuario == null)
                return null;

            if (dto.NombreCompleto != null) usuario.NombreCompleto = dto.NombreCompleto;
            if (dto.Email != null) usuario.Email = dto.Email;
            if (dto.Telefono != null) usuario.Telefono = dto.Telefono;
            if (dto.Rol != null) usuario.Rol = dto.Rol;
            if (dto.Activo.HasValue) usuario.Activo = dto.Activo.Value;

            usuario.FechaActualizacion = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return MapToDto(usuario);
        }

        public async Task<bool> DesactivarUsuarioAsync(int id)
        {
            var usuario = await _context.Usuarios.FindAsync(id);
            if (usuario == null)
                return false;

            usuario.Activo = false;
            usuario.FechaActualizacion = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return true;
        }

        private string GenerarToken(Usuario usuario)
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
                new Claim(ClaimTypes.Name, usuario.NombreUsuario),
                new Claim(ClaimTypes.Email, usuario.Email),
                new Claim(ClaimTypes.Role, usuario.Rol)
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
                _configuration["Jwt:Key"] ?? throw new InvalidOperationException("Configura Jwt__Key mediante una variable de entorno.")));

            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"] ?? "PuntoAba",
                audience: _configuration["Jwt:Audience"] ?? "PuntoAbaClients",
                claims: claims,
                expires: DateTime.UtcNow.AddDays(7),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private UsuarioDto MapToDto(Usuario usuario)
        {
            return new UsuarioDto
            {
                Id = usuario.Id,
                NombreUsuario = usuario.NombreUsuario,
                NombreCompleto = usuario.NombreCompleto,
                Email = usuario.Email,
                Rol = usuario.Rol,
                Telefono = usuario.Telefono,
                Activo = usuario.Activo,
                UltimoAcceso = usuario.UltimoAcceso
            };
        }
    }
}
