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
            var productos = await _productoService.ObtenerTodosAsync();
            return Ok(productos);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ProductoDto>> ObtenerPorId(int id)
        {
            var producto = await _productoService.ObtenerPorIdAsync(id);
            if (producto == null)
                return NotFound($"Producto con ID {id} no encontrado");

            return Ok(producto);
        }

        [HttpGet("codigo/{codigo}")]
        public async Task<ActionResult<ProductoDto>> ObtenerPorCodigo(string codigo)
        {
            var producto = await _productoService.ObtenerPorCodigoAsync(codigo);
            if (producto == null)
                return NotFound($"Producto con código {codigo} no encontrado");

            return Ok(producto);
        }

        [HttpGet("categoria/{categoriaId}")]
        public async Task<ActionResult<IEnumerable<ProductoDto>>> ObtenerPorCategoria(int categoriaId)
        {
            var productos = await _productoService.ObtenerPorCategoriaAsync(categoriaId);
            return Ok(productos);
        }

        [HttpGet("buscar/{termino}")]
        public async Task<ActionResult<IEnumerable<ProductoDto>>> Buscar(string termino)
        {
            var productos = await _productoService.BuscarAsync(termino);
            return Ok(productos);
        }

        [HttpGet("stock-bajo")]
        public async Task<ActionResult<IEnumerable<ProductoDto>>> ObtenerStockBajo()
        {
            var productos = await _productoService.ObtenerStockBajoAsync();
            return Ok(productos);
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Supervisor")]
        public async Task<ActionResult<ProductoDto>> Crear([FromBody] CrearProductoDto dto)
        {
            var producto = await _productoService.CrearAsync(dto);
            return CreatedAtAction(nameof(ObtenerPorId), new { id = producto.Id }, producto);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin,Supervisor")]
        public async Task<ActionResult<ProductoDto>> Actualizar(int id, [FromBody] ActualizarProductoDto dto)
        {
            var producto = await _productoService.ActualizarAsync(id, dto);
            if (producto == null)
                return NotFound($"Producto con ID {id} no encontrado");

            return Ok(producto);
        }

        [HttpPatch("{id}/stock")]
        [Authorize(Roles = "Admin,Supervisor")]
        public async Task<ActionResult> ActualizarStock(int id, [FromQuery] decimal cantidad)
        {
            return Conflict(new ProblemDetails
            {
                Status = 409, Title = "Usa una operación identificada de inventario",
                Detail = "Registra entradas, ajustes o conteos en /api/inventario para conservar motivo y responsable."
            });
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> Eliminar(int id)
        {
            var resultado = await _productoService.EliminarAsync(id);
            if (!resultado)
                return NotFound($"Producto con ID {id} no encontrado");

            return Ok(new { mensaje = "Producto eliminado correctamente" });
        }
    }
}
