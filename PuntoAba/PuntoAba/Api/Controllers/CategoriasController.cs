using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PuntoAba.Data;
using PuntoAba.DTOs;
using PuntoAba.Models;

namespace PuntoAba.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class CategoriasController : ControllerBase
    {
        private readonly PuntoAbaDbContext _context;
        private readonly ILogger<CategoriasController> _logger;

        public CategoriasController(PuntoAbaDbContext context, ILogger<CategoriasController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<CategoriaDto>>> ObtenerTodas()
        {
            try
            {
                var categorias = await _context.Categorias
                    .Where(c => c.Activo)
                    .Select(c => new CategoriaDto
                    {
                        Id = c.Id,
                        Nombre = c.Nombre,
                        Descripcion = c.Descripcion,
                        Activo = c.Activo,
                        CantidadProductos = c.Productos.Count(p => p.Activo)
                    })
                    .ToListAsync();

                return Ok(categorias);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener categorías");
                return StatusCode(500, "Error interno del servidor");
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<CategoriaDto>> ObtenerPorId(int id)
        {
            try
            {
                var categoria = await _context.Categorias
                    .Include(c => c.Productos)
                    .FirstOrDefaultAsync(c => c.Id == id);

                if (categoria == null)
                    return NotFound($"Categoría con ID {id} no encontrada");

                var dto = new CategoriaDto
                {
                    Id = categoria.Id,
                    Nombre = categoria.Nombre,
                    Descripcion = categoria.Descripcion,
                    Activo = categoria.Activo,
                    CantidadProductos = categoria.Productos.Count(p => p.Activo)
                };

                return Ok(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener categoría {Id}", id);
                return StatusCode(500, "Error interno del servidor");
            }
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Supervisor")]
        public async Task<ActionResult<CategoriaDto>> Crear([FromBody] CrearCategoriaDto dto)
        {
            try
            {
                var categoria = new Categoria
                {
                    Nombre = dto.Nombre,
                    Descripcion = dto.Descripcion,
                    FechaCreacion = DateTime.UtcNow
                };

                _context.Categorias.Add(categoria);
                await _context.SaveChangesAsync();

                var categoriaDto = new CategoriaDto
                {
                    Id = categoria.Id,
                    Nombre = categoria.Nombre,
                    Descripcion = categoria.Descripcion,
                    Activo = categoria.Activo,
                    CantidadProductos = 0
                };

                return CreatedAtAction(nameof(ObtenerPorId), new { id = categoria.Id }, categoriaDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear categoría");
                return StatusCode(500, "Error interno del servidor");
            }
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin,Supervisor")]
        public async Task<ActionResult<CategoriaDto>> Actualizar(int id, [FromBody] ActualizarCategoriaDto dto)
        {
            try
            {
                var categoria = await _context.Categorias
                    .Include(c => c.Productos)
                    .FirstOrDefaultAsync(c => c.Id == id);

                if (categoria == null)
                    return NotFound($"Categoría con ID {id} no encontrada");

                if (dto.Nombre != null) categoria.Nombre = dto.Nombre;
                if (dto.Descripcion != null) categoria.Descripcion = dto.Descripcion;
                if (dto.Activo.HasValue) categoria.Activo = dto.Activo.Value;

                categoria.FechaActualizacion = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                var categoriaDto = new CategoriaDto
                {
                    Id = categoria.Id,
                    Nombre = categoria.Nombre,
                    Descripcion = categoria.Descripcion,
                    Activo = categoria.Activo,
                    CantidadProductos = categoria.Productos.Count(p => p.Activo)
                };

                return Ok(categoriaDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar categoría {Id}", id);
                return StatusCode(500, "Error interno del servidor");
            }
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> Eliminar(int id)
        {
            try
            {
                var categoria = await _context.Categorias
                    .Include(c => c.Productos)
                    .FirstOrDefaultAsync(c => c.Id == id);

                if (categoria == null)
                    return NotFound($"Categoría con ID {id} no encontrada");

                if (categoria.Productos.Any(p => p.Activo))
                    return BadRequest(new { mensaje = "No se puede eliminar una categoría con productos activos" });

                categoria.Activo = false;
                categoria.FechaActualizacion = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                return Ok(new { mensaje = "Categoría eliminada correctamente" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar categoría {Id}", id);
                return StatusCode(500, "Error interno del servidor");
            }
        }
    }
}
