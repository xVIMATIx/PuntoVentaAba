using PuntoAba.DTOs;

namespace PuntoAba.Services;

public interface IInventarioService
{
    Task<EntradaMercanciaDto> RegistrarEntradaAsync(CrearEntradaMercanciaDto dto, int? usuarioId = null);
    Task<List<EntradaMercanciaDto>> ObtenerEntradasAsync();
    Task<PaginaMovimientosDto> ObtenerMovimientosAsync(FiltroMovimientosDto filtro);
    Task<List<ProductoResurtidoDto>> ObtenerResurtidoAsync();
}

public class BorradorEntradaInventario
{
    public List<LineaEntradaMercanciaDto> Detalles { get; set; } = new();
}
