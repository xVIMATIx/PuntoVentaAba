using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PuntoAba.DTOs;
using PuntoAba.Services;

namespace PuntoAba.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ProductosController : ControllerBase
    {
        private readonly IProductoService _productoService;
        private readonly ILogger<ProductosController> _logger;

        public ProductosController(IProductoService productoService, ILogger<ProductosController> logger)
        {
            _productoService = productoService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<ProductoDto>>> ObtenerTodos()
        {
            try
            {
                var productos = await _productoService.ObtenerTodosAsync();
                return Ok(productos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener productos");
                return StatusCode(500, "Error interno del servidor");
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ProductoDto>> ObtenerPorId(int id)
        {
            try
            {
                var producto = await _productoService.ObtenerPorIdAsync(id);
                if (producto == null)
                    return NotFound($"Producto con ID {id} no encontrado");

                return Ok(producto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener producto {Id}", id);
                return StatusCode(500, "Error interno del servidor");
            }
        }

        [HttpGet("codigo/{codigo}")]
        public async Task<ActionResult<ProductoDto>> ObtenerPorCodigo(string codigo)
        {
            try
            {
                var producto = await _productoService.ObtenerPorCodigoAsync(codigo);
                if (producto == null)
                    return NotFound($"Producto con código {codigo} no encontrado");

                return Ok(producto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener producto por código {Codigo}", codigo);
                return StatusCode(500, "Error interno del servidor");
            }
        }

        [HttpGet("categoria/{categoriaId}")]
        public async Task<ActionResult<IEnumerable<ProductoDto>>> ObtenerPorCategoria(int categoriaId)
        {
            try
            {
                var productos = await _productoService.ObtenerPorCategoriaAsync(categoriaId);
                return Ok(productos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener productos por categoría {CategoriaId}", categoriaId);
                return StatusCode(500, "Error interno del servidor");
            }
        }

        [HttpGet("buscar/{termino}")]
        public async Task<ActionResult<IEnumerable<ProductoDto>>> Buscar(string termino)
        {
            try
            {
                var productos = await _productoService.BuscarAsync(termino);
                return Ok(productos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al buscar productos con término {Termino}", termino);
                return StatusCode(500, "Error interno del servidor");
            }
        }

        [HttpGet("stock-bajo")]
        public async Task<ActionResult<IEnumerable<ProductoDto>>> ObtenerStockBajo()
        {
            try
            {
                var productos = await _productoService.ObtenerStockBajoAsync();
                return Ok(productos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener productos con stock bajo");
                return StatusCode(500, "Error interno del servidor");
            }
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Supervisor")]
        public async Task<ActionResult<ProductoDto>> Crear([FromBody] CrearProductoDto dto)
        {
            try
            {
                var producto = await _productoService.CrearAsync(dto);
                return CreatedAtAction(nameof(ObtenerPorId), new { id = producto.Id }, producto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear producto");
                return StatusCode(500, ex.Message);
            }
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin,Supervisor")]
        public async Task<ActionResult<ProductoDto>> Actualizar(int id, [FromBody] ActualizarProductoDto dto)
        {
            try
            {
                var producto = await _productoService.ActualizarAsync(id, dto);
                if (producto == null)
                    return NotFound($"Producto con ID {id} no encontrado");

                return Ok(producto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar producto {Id}", id);
                return StatusCode(500, ex.Message);
            }
        }

        [HttpPatch("{id}/stock")]
        [Authorize(Roles = "Admin,Supervisor")]
        public async Task<ActionResult> ActualizarStock(int id, [FromQuery] decimal cantidad)
        {
            try
            {
                var resultado = await _productoService.ActualizarStockAsync(id, cantidad);
                if (!resultado)
                    return NotFound($"Producto con ID {id} no encontrado");

                return Ok(new { mensaje = "Stock actualizado correctamente" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar stock del producto {Id}", id);
                return StatusCode(500, ex.Message);
            }
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> Eliminar(int id)
        {
            try
            {
                var resultado = await _productoService.EliminarAsync(id);
                if (!resultado)
                    return NotFound($"Producto con ID {id} no encontrado");

                return Ok(new { mensaje = "Producto eliminado correctamente" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar producto {Id}", id);
                return StatusCode(500, ex.Message);
            }
        }
    }
}
