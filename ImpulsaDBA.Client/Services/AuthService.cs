using System.Net.Http.Json;
using ImpulsaDBA.Shared.DTOs;
using ImpulsaDBA.Shared.Requests;
using ImpulsaDBA.Shared.Responses;

namespace ImpulsaDBA.Client.Services
{
    /// <summary>
    /// Servicio de autenticación que valida credenciales a través de la API.
    /// </summary>
    public class AuthService
    {
        private readonly HttpClient _httpClient;

        public AuthService(HttpClient httpClient)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        }

        public async Task<UsuarioDto?> ValidarCredencialesUsuario(string? correo, string? cedula, string? numeroCelular)
        {
            try
            {
                var request = new
                {
                    Correo = correo,
                    Cedula = cedula,
                    NumeroCelular = numeroCelular
                };

                var response = await _httpClient.PostAsJsonAsync("api/auth/validar-usuario", request);
                
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<UsuarioDto>();
                }
                
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al validar credenciales de usuario: {ex.Message}");
                return null;
            }
        }

        public async Task<UsuarioDto?> ValidarCredencialesPassword(string password, int? usuarioId = null)
        {
            try
            {
                if (!usuarioId.HasValue)
                {
                    return null;
                }

                var request = new
                {
                    Password = password,
                    UsuarioId = usuarioId.Value
                };

                var response = await _httpClient.PostAsJsonAsync("api/auth/validar-password", request);
                
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<UsuarioDto>();
                }
                
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al validar contraseña: {ex.Message}");
                return null;
            }
        }

        public async Task<LoginResponse?> ValidarLoginCompleto(string usuario, string password)
        {
            try
            {
                var request = new
                {
                    Usuario = usuario,
                    Password = password
                };

                var response = await _httpClient.PostAsJsonAsync("api/auth/login", request);
                
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<LoginResponse>();
                }
                
                // Si la respuesta no es exitosa, intentar leer el mensaje de error
                var errorContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Error en login: Status {response.StatusCode}, Content: {errorContent}");
                
                // Si es un 405, podría ser un problema de configuración del servidor
                if (response.StatusCode == System.Net.HttpStatusCode.MethodNotAllowed)
                {
                    Console.WriteLine("ERROR: El método HTTP no está permitido. Verifique la configuración del servidor API.");
                }
                
                return new LoginResponse 
                { 
                    Success = false, 
                    Message = $"Error al autenticar: {response.StatusCode}" 
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al validar login completo: {ex.Message}");
                return new LoginResponse 
                { 
                    Success = false, 
                    Message = $"Error de conexión: {ex.Message}" 
                };
            }
        }
    }
}
