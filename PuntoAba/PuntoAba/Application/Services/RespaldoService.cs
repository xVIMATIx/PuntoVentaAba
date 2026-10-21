using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Data.Sqlite;

namespace PuntoAba.Services;
public record ArchivoRespaldo(string Nombre, DateTime Fecha, long Bytes);
public record VerificacionRespaldo(int Productos, int Ventas, int Movimientos, int Imagenes);
public class MotorRespaldos(IConfiguration configuracion, IWebHostEnvironment entorno)
{
    private readonly SemaphoreSlim mutex = new(1);
    public string Carpeta => Path.GetFullPath(Path.Combine(entorno.ContentRootPath, configuracion["Respaldos:Carpeta"] ?? "Respaldos"));
    public bool Automaticos => configuracion.GetValue("Respaldos:Automaticos", true);
    public List<ArchivoRespaldo> Listar() => Directory.Exists(Carpeta) ? new DirectoryInfo(Carpeta).EnumerateFiles("ABA-*.zip").OrderByDescending(f => f.LastWriteTimeUtc).Select(f => new ArchivoRespaldo(f.Name, f.LastWriteTimeUtc, f.Length)).ToList() : new();
    public async Task<string> CrearAsync()
    {
        await mutex.WaitAsync();
        var temporal = Path.Combine(Carpeta, ".tmp-" + Guid.NewGuid().ToString("N"));
        try
        {
            Directory.CreateDirectory(temporal);
            var cs = new SqliteConnectionStringBuilder(configuracion.GetConnectionString("DefaultConnection") ?? "Data Source=PuntoAba.db");
            if (cs.DataSource == ":memory:") throw new InvalidOperationException("La base en memoria no admite respaldos de archivos.");
            cs.DataSource = Path.GetFullPath(cs.DataSource, entorno.ContentRootPath); cs.Mode = SqliteOpenMode.ReadOnly; cs.Pooling = false;
            await using (var origen = new SqliteConnection(cs.ToString()))
            await using (var destino = new SqliteConnection($"Data Source={Path.Combine(temporal, "PuntoAba.db")};Pooling=False"))
            { await origen.OpenAsync(); await destino.OpenAsync(); origen.BackupDatabase(destino); }
            var imagenes = Path.Combine(entorno.WebRootPath ?? Path.Combine(entorno.ContentRootPath, "wwwroot"), "images", "productos");
            var hashes = new Dictionary<string, string>();
            hashes["PuntoAba.db"] = Convert.ToHexString(SHA256.HashData(await File.ReadAllBytesAsync(Path.Combine(temporal, "PuntoAba.db"))));
            if (Directory.Exists(imagenes)) foreach (var file in Directory.EnumerateFiles(imagenes, "*", SearchOption.TopDirectoryOnly))
            {
                var nombre = "imagenes/" + Path.GetFileName(file); Directory.CreateDirectory(Path.Combine(temporal, "imagenes"));
                File.Copy(file, Path.Combine(temporal, nombre)); hashes[nombre] = Convert.ToHexString(SHA256.HashData(await File.ReadAllBytesAsync(Path.Combine(temporal, nombre))));
            }
            await File.WriteAllTextAsync(Path.Combine(temporal, "manifest.json"), JsonSerializer.Serialize(hashes));
            var nombreZip = $"ABA-{DateTime.Now:yyyyMMdd-HHmmss}-{Guid.NewGuid().ToString("N")[..8]}.zip";
            ZipFile.CreateFromDirectory(temporal, Path.Combine(Carpeta, nombreZip)); return nombreZip;
        }
        finally { if (Directory.Exists(temporal)) Directory.Delete(temporal, true); mutex.Release(); }
    }
    public async Task<VerificacionRespaldo> VerificarAsync(string nombre)
    {
        if (nombre != Path.GetFileName(nombre) || !Listar().Any(f => f.Nombre == nombre)) throw new ArgumentException("Selecciona un respaldo de la lista.");
        var temporal = Path.Combine(Carpeta, ".verificar-" + Guid.NewGuid().ToString("N"));
        try
        {
            Directory.CreateDirectory(temporal); ZipFile.ExtractToDirectory(Path.Combine(Carpeta, nombre), temporal);
            var hashes = JsonSerializer.Deserialize<Dictionary<string, string>>(await File.ReadAllTextAsync(Path.Combine(temporal, "manifest.json"))) ?? throw new InvalidOperationException("El respaldo no contiene un índice válido.");
            foreach (var h in hashes)
            {
                var path = Path.GetFullPath(Path.Combine(temporal, h.Key));
                if (!path.StartsWith(temporal + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) || !File.Exists(path) || Convert.ToHexString(SHA256.HashData(await File.ReadAllBytesAsync(path))) != h.Value)
                    throw new InvalidOperationException("El respaldo está incompleto o sus archivos cambiaron.");
            }
            await using var db = new SqliteConnection($"Data Source={Path.Combine(temporal, "PuntoAba.db")};Mode=ReadOnly;Pooling=False"); await db.OpenAsync();
            using var check = db.CreateCommand(); check.CommandText = "PRAGMA integrity_check";
            if (Convert.ToString(await check.ExecuteScalarAsync()) != "ok") throw new InvalidOperationException("La base recuperada no pasó la comprobación de integridad.");
            async Task<int> Contar(string tabla) { using var cmd = db.CreateCommand(); cmd.CommandText = "SELECT COUNT(*) FROM " + tabla; return Convert.ToInt32(await cmd.ExecuteScalarAsync()); }
            return new(await Contar("Productos"), await Contar("Ventas"), await Contar("MovimientosInventario"), hashes.Keys.Count(k => k.StartsWith("imagenes/")));
        }
        finally { if (Directory.Exists(temporal)) Directory.Delete(temporal, true); }
    }
}
public class RespaldoService(MotorRespaldos motor, IAccesoOperacion acceso)
{
    public string Carpeta => motor.Carpeta;
    public bool Automaticos => motor.Automaticos;
    public async Task<List<ArchivoRespaldo>> ListarAsync() { await acceso.ExigirAsync("Admin"); return motor.Listar(); }
    public async Task<string> CrearAsync() { await acceso.ExigirAsync("Admin"); return await motor.CrearAsync(); }
    public async Task<VerificacionRespaldo> VerificarAsync(string nombre) { await acceso.ExigirAsync("Admin"); return await motor.VerificarAsync(nombre); }
}
public class RespaldoPeriodico(MotorRespaldos motor, ILogger<RespaldoPeriodico> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!motor.Automaticos) return;
        // El arranque termina de aplicar las migraciones antes del primer respaldo.
        await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);
        using var reloj = new PeriodicTimer(TimeSpan.FromMinutes(30));
        do
        {
            try { if (motor.Listar().FirstOrDefault()?.Fecha < DateTime.UtcNow.AddDays(-1) || motor.Listar().Count == 0) await motor.CrearAsync(); }
            catch (Exception ex) { logger.LogError(ex, "No se pudo crear el respaldo automático"); }
        } while (await reloj.WaitForNextTickAsync(stoppingToken));
    }
}
