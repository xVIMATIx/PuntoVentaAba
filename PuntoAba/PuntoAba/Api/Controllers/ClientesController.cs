using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PuntoAba.Data;
using PuntoAba.DTOs;

namespace PuntoAba.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ClientesController : ControllerBase
    {
        private readonly PuntoAbaDbContext _context;
        private readonly ILogger<ClientesController> _logger;

        public ClientesController(PuntoAbaDbContext context, ILogger<ClientesController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<ClienteDto>>> ObtenerTodos()
        {
            try
            {
                var clientes = await _context.Clientes
                    .Where(c => c.Activo)
                    .Select(c => new ClienteDto
                    {
                        Id = c.Id,
                        Nombre = c.Nombre,
                        Apellido = c.Apellido,
                        Telefono = c.Telefono,
                        Email = c.Email,
                        Direccion = c.Direccion,
                        DocumentoIdentidad = c.DocumentoIdentidad,
                        Activo = c.Activo
                    })
                    .ToListAsync();

                return Ok(clientes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener clientes");
                return StatusCode(500, "Error interno del servidor");
            }
        }

        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<ActionResult<ClienteDto>> ObtenerPorId(int id)
        {
            try
            {
                var cliente = await _context.Clientes.FindAsync(id);
                if (cliente == null)
                    return NotFound($"Cliente con ID {id} no encontrado");

                var dto = new ClienteDto
                {
                    Id = cliente.Id,
                    Nombre = cliente.Nombre,
                    Apellido = cliente.Apellido,
                    Telefono = cliente.Telefono,
                    Email = cliente.Email,
                    Direccion = cliente.Direccion,
                    DocumentoIdentidad = cliente.DocumentoIdentidad,
                    Activo = cliente.Activo
                };

                return Ok(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener cliente {Id}", id);
                return StatusCode(500, "Error interno del servidor");
            }
        }

        [HttpGet("buscar/{termino}")]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<ClienteDto>>> Buscar(string termino)
        {
            try
            {
                var clientes = await _context.Clientes
                    .Where(c => c.Activo && (
                        c.Nombre.Contains(termino) ||
                        (c.Apellido != null && c.Apellido.Contains(termino)) ||
                        (c.DocumentoIdentidad != null && c.DocumentoIdentidad.Contains(termino)) ||
                        (c.Telefono != null && c.Telefono.Contains(termino))
                    ))
                    .Select(c => new ClienteDto
                    {
                        Id = c.Id,
                        Nombre = c.Nombre,
                        Apellido = c.Apellido,
                        Telefono = c.Telefono,
                        Email = c.Email,
                        Direccion = c.Direccion,
                        DocumentoIdentidad = c.DocumentoIdentidad,
                        Activo = c.Activo
                    })
                    .ToListAsync();

                return Ok(clientes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al buscar clientes");
                return StatusCode(500, "Error interno del servidor");
            }
        }

        [HttpPost]
        public async Task<ActionResult<ClienteDto>> Crear([FromBody] CrearClienteDto dto)
        {
            try
            {
                var cliente = new Models.Cliente
                {
                    Nombre = dto.Nombre,
                    Apellido = dto.Apellido,
                    Telefono = dto.Telefono,
                    Email = dto.Email,
                    Direccion = dto.Direccion,
                    DocumentoIdentidad = dto.DocumentoIdentidad,
                    FechaCreacion = DateTime.UtcNow
                };

                _context.Clientes.Add(cliente);
                await _context.SaveChangesAsync();

                var clienteDto = new ClienteDto
                {
                    Id = cliente.Id,
                    Nombre = cliente.Nombre,
                    Apellido = cliente.Apellido,
                    Telefono = cliente.Telefono,
                    Email = cliente.Email,
                    Direccion = cliente.Direccion,
                    DocumentoIdentidad = cliente.DocumentoIdentidad,
                    Activo = cliente.Activo
                };

                return CreatedAtAction(nameof(ObtenerPorId), new { id = cliente.Id }, clienteDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear cliente");
                return StatusCode(500, "Error interno del servidor");
            }
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<ClienteDto>> Actualizar(int id, [FromBody] ActualizarClienteDto dto)
        {
            try
            {
                var cliente = await _context.Clientes.FindAsync(id);
                if (cliente == null)
                    return NotFound($"Cliente con ID {id} no encontrado");

                if (dto.Nombre != null) cliente.Nombre = dto.Nombre;
                if (dto.Apellido != null) cliente.Apellido = dto.Apellido;
                if (dto.Telefono != null) cliente.Telefono = dto.Telefono;
                if (dto.Email != null) cliente.Email = dto.Email;
                if (dto.Direccion != null) cliente.Direccion = dto.Direccion;
                if (dto.DocumentoIdentidad != null) cliente.DocumentoIdentidad = dto.DocumentoIdentidad;
                if (dto.Activo.HasValue) cliente.Activo = dto.Activo.Value;

                cliente.FechaActualizacion = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                var clienteDto = new ClienteDto
                {
                    Id = cliente.Id,
                    Nombre = cliente.Nombre,
                    Apellido = cliente.Apellido,
                    Telefono = cliente.Telefono,
                    Email = cliente.Email,
                    Direccion = cliente.Direccion,
                    DocumentoIdentidad = cliente.DocumentoIdentidad,
                    Activo = cliente.Activo
                };

                return Ok(clienteDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar cliente {Id}", id);
                return StatusCode(500, "Error interno del servidor");
            }
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> Eliminar(int id)
        {
            try
            {
                var cliente = await _context.Clientes.FindAsync(id);
                if (cliente == null)
                    return NotFound($"Cliente con ID {id} no encontrado");

                cliente.Activo = false;
                cliente.FechaActualizacion = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                return Ok(new { mensaje = "Cliente eliminado correctamente" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar cliente {Id}", id);
                return StatusCode(500, "Error interno del servidor");
            }
        }
    }
}
