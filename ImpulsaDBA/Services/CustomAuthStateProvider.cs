using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Authorization;
using Blazored.LocalStorage;
using ImpulsaDBA.Models;

namespace ImpulsaDBA.Services
{
    /// <summary>
    /// Proveedor de estado de autenticación personalizado para Blazor.
    /// 
    /// ALMACENAMIENTO:
    /// Utiliza Blazored.LocalStorage para guardar la sesión del usuario en el navegador.
    /// La información de autenticación se guarda como JSON en LocalStorage.
    /// 
    /// ESTADO DE AUTENTICACIÓN:
    /// - Si hay sesión guardada en LocalStorage, crea ClaimsIdentity con los datos del usuario
    /// - Si no hay sesión, retorna un usuario no autenticado
    /// </summary>
    public class CustomAuthStateProvider : AuthenticationStateProvider
    {
        private readonly ILocalStorageService _localStorage;
        private const string SESSION_KEY = "usuario_sesion";

        /// <summary>
        /// Constructor que recibe el servicio de LocalStorage inyectado.
        /// LocalStorage permite guardar datos en el navegador del cliente.
        /// </summary>
        public CustomAuthStateProvider(ILocalStorageService localStorage)
        {
            _localStorage = localStorage ?? throw new ArgumentNullException(nameof(localStorage));
        }

        /// <summary>
        /// Obtiene el estado de autenticación actual del usuario.
        /// 
        /// PROCESO:
        /// 1. Intenta leer la sesión guardada en LocalStorage
        /// 2. Si existe sesión, crea ClaimsIdentity con los datos del usuario
        /// 3. Si no existe, retorna un usuario no autenticado
        /// </summary>
        public override async Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            try
            {
                // Intentar obtener la sesión del usuario desde LocalStorage
                var sesionJson = await _localStorage.GetItemAsStringAsync(SESSION_KEY);
                
                if (string.IsNullOrWhiteSpace(sesionJson))
                {
                    // No hay sesión guardada, usuario no autenticado
                    return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
                }

                // Deserializar la sesión del usuario
                var sesion = System.Text.Json.JsonSerializer.Deserialize<SesionUsuario>(sesionJson);
                
                if (sesion == null)
                {
                    return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
                }

                // Crear claims (información del usuario) para la autenticación
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, sesion.UsuarioId.ToString()),
                    new Claim(ClaimTypes.Name, sesion.NombreCompleto ?? string.Empty),
                    new Claim(ClaimTypes.Email, sesion.Email ?? string.Empty),
                    new Claim("Perfil", sesion.Perfil ?? string.Empty),
                    new Claim("FotoUrl", sesion.FotoUrl ?? string.Empty)
                };

                // Crear identidad autenticada con los claims
                var identity = new ClaimsIdentity(claims, "custom");
                var user = new ClaimsPrincipal(identity);

                return new AuthenticationState(user);
            }
            catch
            {
                // En caso de error, retornar usuario no autenticado
                return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
            }
        }

        /// <summary>
        /// Guarda la sesión del usuario en LocalStorage y notifica el cambio de estado.
        /// 
        /// ALMACENAMIENTO:
        /// Guarda la información de sesión en LocalStorage del navegador como JSON.
        /// Esto permite que la sesión persista entre recargas de página.
        /// </summary>
        /// <param name="sesion">Información de sesión del usuario a guardar</param>
        public async Task GuardarSesionAsync(SesionUsuario sesion)
        {
            try
            {
                // Serializar y guardar la sesión en LocalStorage
                var sesionJson = System.Text.Json.JsonSerializer.Serialize(sesion);
                await _localStorage.SetItemAsStringAsync(SESSION_KEY, sesionJson);

                // Notificar cambio de estado de autenticación
                NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al guardar sesión: {ex.Message}");
            }
        }

        /// <summary>
        /// Elimina la sesión del usuario de LocalStorage y notifica el cambio de estado.
        /// 
        /// CERRAR SESIÓN:
        /// Elimina la información de sesión de LocalStorage y marca al usuario como no autenticado.
        /// </summary>
        public async Task CerrarSesionAsync()
        {
            try
            {
                // Eliminar sesión de LocalStorage
                await _localStorage.RemoveItemAsync(SESSION_KEY);

                // Notificar cambio de estado de autenticación (usuario no autenticado)
                NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()))));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al cerrar sesión: {ex.Message}");
            }
        }
    }
}
