using System.Globalization;
using PuntoAba.DTOs;
namespace PuntoAba.Presentation.Ventas;

public static class OrdenCatalogo
{
    private static readonly StringComparer Espanol = StringComparer.Create(CultureInfo.GetCultureInfo("es-MX"), true);
    public static List<ProductoDto> Aplicar(IEnumerable<ProductoDto> productos, string filtro, int categoria,
        IReadOnlyDictionary<int, int> frecuencia, IReadOnlyList<int> favoritos)
    {
        var q = productos.Where(p => p.Activo);
        if (categoria == -1) q = q.Where(p => favoritos.Contains(p.Id));
        else if (categoria > 0) q = q.Where(p => p.CategoriaId == categoria);
        if (!string.IsNullOrWhiteSpace(filtro)) q = q.Where(p => p.Nombre.Contains(filtro.Trim(), StringComparison.CurrentCultureIgnoreCase) || p.Codigo.Contains(filtro.Trim(), StringComparison.OrdinalIgnoreCase));
        return q.OrderBy(p => p.Stock <= 0)
            .ThenByDescending(p => categoria == 0 && frecuencia.TryGetValue(p.Id, out var n) ? n : 0)
            .ThenBy(p => categoria == -1 ? favoritos.ToList().IndexOf(p.Id) : 0)
            .ThenBy(p => p.Nombre, Espanol).ThenBy(p => p.Id).ToList();
    }
}

// Estabilidad estimada por tres lecturas consecutivas; no sustituye la certificacion de la balanza.
public sealed class PesoVenta
{
    private readonly Queue<(decimal Peso, DateTime Fecha)> muestras = new();
    public void Registrar(decimal? peso, DateTime? fecha, bool conectada, string? trama)
    {
        if (!conectada || peso is null or <= 0 || fecha == null || (trama?.Contains("US", StringComparison.OrdinalIgnoreCase) ?? false)) { muestras.Clear(); return; }
        if (muestras.TryPeek(out var primera) && fecha <= muestras.Last().Fecha) return;
        muestras.Enqueue((peso.Value, fecha.Value));
        while (muestras.Count > 3) muestras.Dequeue();
    }
    public decimal? Obtener(bool conectada, DateTime ahora)
    {
        if (!conectada || muestras.Count < 3) return null;
        var ultima = muestras.Last();
        if (ahora < ultima.Fecha || ahora - ultima.Fecha > TimeSpan.FromSeconds(4) || ultima.Fecha - muestras.First().Fecha > TimeSpan.FromSeconds(8)) return null;
        return muestras.Max(x => x.Peso) - muestras.Min(x => x.Peso) <= .002m ? decimal.Round(ultima.Peso, 3) : null;
    }
}
