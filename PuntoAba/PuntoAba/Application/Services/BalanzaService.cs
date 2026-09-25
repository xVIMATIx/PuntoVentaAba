using Microsoft.Extensions.Options;
using System.Globalization;
using System.IO.Ports;
using System.Text;
using System.Text.RegularExpressions;

namespace PuntoAba.Services
{
    public sealed class BalanzaSettings
    {
        public bool Enabled { get; set; } = true;
        public bool AutoDetect { get; set; } = true;
        public string PortName { get; set; } = string.Empty;
        public int BaudRate { get; set; } = 115200;
        public Parity Parity { get; set; } = Parity.None;
        public int DataBits { get; set; } = 8;
        public StopBits StopBits { get; set; } = StopBits.One;
        public Handshake Handshake { get; set; } = Handshake.None;
        public string Command { get; set; } = "P";
        public int PollIntervalMs { get; set; } = 1000;
        public int ResponseTimeoutMs { get; set; } = 2000;
        public int RetryIntervalMs { get; set; } = 5000;
        public string WeightUnit { get; set; } = "kg";
        public decimal MinimumValidWeight { get; set; } = 0.001m;
    }

    public interface IBalanzaService
    {
        bool Conectada { get; }
        string Estado { get; }
        string? PuertoActual { get; }
        decimal? PesoActual { get; }
        string? LecturaCruda { get; }
        DateTime? UltimaLectura { get; }
        string UnidadMedida { get; }
        event Action? EstadoChanged;
    }

    public sealed class BalanzaService : BackgroundService, IBalanzaService
    {
        private readonly BalanzaSettings _settings;
        private readonly ILogger<BalanzaService> _logger;
        private readonly object _sync = new();
        private SerialPort? _serialPort;
        private bool _conectada;
        private string _estado = "Desconectada";
        private string? _puertoActual;
        private decimal? _pesoActual;
        private string? _lecturaCruda;
        private DateTime? _ultimaLectura;

        public event Action? EstadoChanged;

        public BalanzaService(IOptions<BalanzaSettings> options, ILogger<BalanzaService> logger)
        {
            _settings = options.Value;
            _logger = logger;
        }

        public bool Conectada { get { lock (_sync) { return _conectada; } } }
        public string Estado { get { lock (_sync) { return _estado; } } }
        public string? PuertoActual { get { lock (_sync) { return _puertoActual; } } }
        public decimal? PesoActual { get { lock (_sync) { return _pesoActual; } } }
        public string? LecturaCruda { get { lock (_sync) { return _lecturaCruda; } } }
        public DateTime? UltimaLectura { get { lock (_sync) { return _ultimaLectura; } } }
        public string UnidadMedida => _settings.WeightUnit;

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (!_settings.Enabled)
            {
                UpdateState(false, "Balanza deshabilitada", null, null, null, null);
                return;
            }

            _logger.LogInformation("BalanzaService iniciado");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    if (!Conectada)
                    {
                        var connected = await TryConnectAsync(stoppingToken);
                        if (!connected)
                        {
                            UpdateState(false, "No se encontró la balanza", null, null, null, null);
                        }
                    }
                    else
                    {
                        // Solo leer si está conectada
                        try
                        {
                            var lectura = await RequestWeightAsync(stoppingToken);
                            // No hacer nada si la lectura es nula, solo mantener conexión
                        }
                        catch
                        {
                            await DisconnectPortAsync();
                        }
                    }

                    await Task.Delay(_settings.PollIntervalMs, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error en BalanzaService");
                    await DisconnectPortAsync();
                    UpdateState(false, "Error de comunicación", null, null, null, null);
                    await Task.Delay(_settings.RetryIntervalMs, stoppingToken);
                }
            }

            await DisconnectPortAsync();
            _logger.LogInformation("BalanzaService detenido");
        }

        private async Task<bool> TryConnectAsync(CancellationToken stoppingToken)
        {
            await DisconnectPortAsync();

            var portsToTry = new List<string>();
            if (!string.IsNullOrWhiteSpace(_settings.PortName))
            {
                portsToTry.Add(_settings.PortName.Trim());
            }
            else if (_settings.AutoDetect)
            {
                portsToTry.AddRange(SerialPort.GetPortNames().OrderBy(p => p, StringComparer.OrdinalIgnoreCase));
            }

            foreach (var portName in portsToTry)
            {
                if (stoppingToken.IsCancellationRequested)
                {
                    return false;
                }

                if (await TryOpenAndProbePortAsync(portName, stoppingToken))
                {
                    return true;
                }
            }

            return false;
        }

        private async Task<bool> TryOpenAndProbePortAsync(string portName, CancellationToken stoppingToken)
        {
            SerialPort? port = null;

            try
            {
                port = CreatePort(portName);
                port.Open();
                
                // Esperar a que el puerto esté listo
                await Task.Delay(100, stoppingToken);
                
                port.DiscardInBuffer();
                port.DiscardOutBuffer();

                var probe = await RequestWeightAsync(port, stoppingToken);
                if (probe.HasValue)
                {
                    lock (_sync)
                    {
                        _serialPort = port;
                        _puertoActual = portName;
                        _conectada = true;
                        _estado = $"Balanza conectada en {portName}";
                    }

                    NotifyStateChanged();
                    port = null;
                    return true;
                }
            }
            catch (UnauthorizedAccessException)
            {
                _logger.LogDebug("Puerto {PortName} está en uso por otro proceso", portName);
            }
            catch (System.IO.IOException ex)
            {
                _logger.LogDebug(ex, "No se pudo conectar al puerto {PortName}", portName);
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "No se pudo conectar con el puerto {PortName}", portName);
            }
            finally
            {
                if (port != null)
                {
                    try 
                    { 
                        if (port.IsOpen) port.Close(); 
                    } 
                    catch { }
                    try 
                    { 
                        port.Dispose(); 
                    } 
                    catch { }
                }
            }

            return false;
        }

        private SerialPort CreatePort(string portName)
        {
            return new SerialPort(portName, _settings.BaudRate, _settings.Parity, _settings.DataBits, _settings.StopBits)
            {
                Handshake = _settings.Handshake,
                ReadTimeout = _settings.ResponseTimeoutMs,
                WriteTimeout = _settings.ResponseTimeoutMs,
                Encoding = Encoding.ASCII,
                NewLine = "\r\n"
            };
        }

        private async Task<decimal?> RequestWeightAsync(CancellationToken stoppingToken)
        {
            SerialPort? port;
            lock (_sync)
            {
                port = _serialPort;
            }

            if (port == null || !port.IsOpen)
            {
                return null;
            }

            return await RequestWeightAsync(port, stoppingToken);
        }

        private async Task<decimal?> RequestWeightAsync(SerialPort port, CancellationToken stoppingToken)
        {
            try
            {
                port.DiscardInBuffer();
                port.DiscardOutBuffer();
                port.Write(_settings.Command);

                var raw = await ReadResponseAsync(port, stoppingToken);
                if (string.IsNullOrWhiteSpace(raw))
                {
                    return null;
                }

                var peso = ParseWeight(raw);
                if (peso.HasValue && peso.Value >= _settings.MinimumValidWeight)
                {
                    lock (_sync)
                    {
                        _pesoActual = peso.Value;
                        _lecturaCruda = raw;
                        _ultimaLectura = DateTime.Now;
                        _conectada = true;
                        _estado = $"Balanza conectada en {_puertoActual}";
                    }

                    NotifyStateChanged();
                    return peso.Value;
                }

                lock (_sync)
                {
                    _lecturaCruda = raw;
                    _pesoActual = null;
                    _conectada = true;
                    _estado = $"Lectura inválida desde {_puertoActual}: {raw}";
                }

                NotifyStateChanged();
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Error al solicitar peso de la balanza");
                return null;
            }
        }

        private async Task<string> ReadResponseAsync(SerialPort port, CancellationToken stoppingToken)
        {
            // Esperar a que la balanza responda (como hace ChatGPT)
            await Task.Delay(300, stoppingToken);
            
            try
            {
                return port.ReadExisting();
            }
            catch
            {
                return string.Empty;
            }
        }

        private static decimal? ParseWeight(string raw)
        {
            // Acepta formatos con punto o coma decimal, e incluso enteros.
            var match = Regex.Match(raw, @"-?\d+(?:[.,]\d+)?");
            if (!match.Success)
            {
                return null;
            }

            var normalized = match.Value.Replace(',', '.');
            if (decimal.TryParse(normalized, NumberStyles.Any, CultureInfo.InvariantCulture, out var value))
            {
                return value;
            }

            return null;
        }

        private async Task DisconnectPortAsync()
        {
            SerialPort? port;
            lock (_sync)
            {
                port = _serialPort;
                _serialPort = null;
                _conectada = false;
                _puertoActual = null;
                _pesoActual = null;
                _lecturaCruda = null;
                _ultimaLectura = null;
                _estado = "Desconectada";
            }

            if (port == null)
            {
                return;
            }

            try
            {
                if (port.IsOpen)
                {
                    port.Close();
                }
            }
            catch
            {
            }
            finally
            {
                port.Dispose();
            }

            NotifyStateChanged();
        }

        private void UpdateState(bool conectada, string estado, string? puerto, decimal? peso, string? lectura, DateTime? ultimaLectura)
        {
            lock (_sync)
            {
                _conectada = conectada;
                _estado = estado;
                _puertoActual = puerto;
                _pesoActual = peso;
                _lecturaCruda = lectura;
                _ultimaLectura = ultimaLectura;
            }

            NotifyStateChanged();
        }

        private void NotifyStateChanged()
        {
            try
            {
                EstadoChanged?.Invoke();
            }
            catch
            {
            }
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            await DisconnectPortAsync();
            await base.StopAsync(cancellationToken);
        }
    }
}
