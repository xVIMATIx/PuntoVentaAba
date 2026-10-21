using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.JSInterop;
using PuntoAba.DTOs;

namespace PuntoAba.Services
{
    public class SesionUsuarioService
    {
        private const string TokenStorageKey = "puntoaba.auth.token";
        private const string UsuarioStorageKey = "puntoaba.auth.usuario";

        private readonly HttpClient _httpClient;
        private readonly IJSRuntime _jsRuntime;
        private readonly ILogger<SesionUsuarioService> _logger;
        private readonly IdentidadToken _identidad;

        public SesionUsuarioService(HttpClient httpClient, IJSRuntime jsRuntime, ILogger<SesionUsuarioService> logger, IdentidadToken identidad)
        {
            _httpClient = httpClient;
            _jsRuntime = jsRuntime;
            _logger = logger;
            _identidad = identidad;
        }

        public LoginResponseDto? UsuarioActual { get; private set; }
        public string? Token => UsuarioActual?.Token;
        public bool EstaAutenticado => !string.IsNullOrWhiteSpace(Token);

        public async Task CargarSesionGuardadaAsync()
        {
            try
            {
                var token = await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", TokenStorageKey);
                var usuarioJson = await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", UsuarioStorageKey);

                if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(usuarioJson))
                {
                    LimpiarSesionEnMemoria();
                    return;
                }

                var validado = await _identidad.ValidarAsync(token);
                var usuario = new LoginResponseDto { Id = validado.Id, NombreUsuario = validado.NombreUsuario, NombreCompleto = validado.NombreCompleto, Email = validado.Email, Rol = validado.Rol };
                if (usuario == null)
                {
                    _logger.LogWarning("No se pudo deserializar la sesión de usuario guardada.");
                    LimpiarSesionEnMemoria();
                    return;
                }

                usuario.Token = token;
                UsuarioActual = usuario;
                AplicarTokenHttpClient();
            }
            catch (UnauthorizedAccessException) { LimpiarSesionEnMemoria(); }
            catch (JSException ex)
            {
                _logger.LogWarning(ex, "No se pudo acceder a localStorage para cargar sesión.");
                LimpiarSesionEnMemoria();
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "El runtime JS no está listo para cargar sesión.");
                LimpiarSesionEnMemoria();
            }
        }

        public async Task<(bool Exitoso, string Mensaje)> IniciarSesionAsync(LoginDto credenciales)
        {
            var response = await _httpClient.PostAsJsonAsync("api/auth/login", credenciales);

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                return (false, "Credenciales inválidas.");
            }

            if (!response.IsSuccessStatusCode)
            {
                return (false, $"No se pudo iniciar sesión ({(int)response.StatusCode}).");
            }

            var usuario = await response.Content.ReadFromJsonAsync<LoginResponseDto>();
            if (usuario == null || string.IsNullOrWhiteSpace(usuario.Token))
            {
                return (false, "La respuesta de autenticación no contiene token.");
            }

            UsuarioActual = usuario;
            AplicarTokenHttpClient();
            await GuardarSesionAsync();

            return (true, "Sesión iniciada correctamente.");
        }

        public async Task CerrarSesionAsync()
        {
            LimpiarSesionEnMemoria();

            try
            {
                await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", TokenStorageKey);
                await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", UsuarioStorageKey);
            }
            catch (JSException ex)
            {
                _logger.LogWarning(ex, "No se pudo limpiar localStorage al cerrar sesión.");
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "El runtime JS no está listo para limpiar sesión.");
            }
        }

        private async Task GuardarSesionAsync()
        {
            if (UsuarioActual == null || string.IsNullOrWhiteSpace(UsuarioActual.Token))
            {
                _logger.LogWarning("Se intentó guardar sesión sin datos de token.");
                return;
            }

            var usuarioSinToken = new LoginResponseDto
            {
                Id = UsuarioActual.Id,
                NombreUsuario = UsuarioActual.NombreUsuario,
                NombreCompleto = UsuarioActual.NombreCompleto,
                Email = UsuarioActual.Email,
                Rol = UsuarioActual.Rol,
                Token = string.Empty
            };

            try
            {
                await _jsRuntime.InvokeVoidAsync("localStorage.setItem", TokenStorageKey, UsuarioActual.Token);
                await _jsRuntime.InvokeVoidAsync("localStorage.setItem", UsuarioStorageKey, JsonSerializer.Serialize(usuarioSinToken));
            }
            catch (JSException ex)
            {
                _logger.LogWarning(ex, "No se pudo guardar sesión en localStorage.");
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "El runtime JS no está listo para guardar sesión.");
            }
        }

        private void AplicarTokenHttpClient()
        {
            if (string.IsNullOrWhiteSpace(Token))
            {
                _httpClient.DefaultRequestHeaders.Authorization = null;
                return;
            }

            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Token);
        }

        private void LimpiarSesionEnMemoria()
        {
            UsuarioActual = null;
            _httpClient.DefaultRequestHeaders.Authorization = null;
        }
    }
}
