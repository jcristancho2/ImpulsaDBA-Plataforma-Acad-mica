using System.Net.Http.Json;
using ImpulsaDBA.Shared.DTOs;
using ImpulsaDBA.Shared.Requests;

namespace ImpulsaDBA.Client.Services
{
    public class CalendarioService
    {
        private readonly HttpClient _httpClient;

        public CalendarioService(HttpClient httpClient)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        }

        public async Task<List<ActividadCalendarioDto>> ObtenerActividadesPorFecha(int idAsignacionAcademica, DateTime fecha)
        {
            try
            {
                var actividades = await _httpClient.GetFromJsonAsync<List<ActividadCalendarioDto>>(
                    $"api/calendario/asignacion/{idAsignacionAcademica}/fecha?fecha={fecha:yyyy-MM-dd}");
                return actividades ?? new List<ActividadCalendarioDto>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al obtener actividades por fecha: {ex.Message}");
                return new List<ActividadCalendarioDto>();
            }
        }

        public async Task<List<ActividadCalendarioDto>> ObtenerActividadesPorMes(int idAsignacionAcademica, int año, int mes)
        {
            try
            {
                var actividades = await _httpClient.GetFromJsonAsync<List<ActividadCalendarioDto>>(
                    $"api/calendario/asignacion/{idAsignacionAcademica}/mes?año={año}&mes={mes}");
                return actividades ?? new List<ActividadCalendarioDto>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al obtener actividades por mes: {ex.Message}");
                return new List<ActividadCalendarioDto>();
            }
        }

        public async Task<List<DateTime>> ObtenerDiasFestivos(int año)
        {
            try
            {
                var dias = await _httpClient.GetFromJsonAsync<List<DateTime>>($"api/calendario/dias-festivos/{año}");
                return dias ?? new List<DateTime>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al obtener días festivos: {ex.Message}");
                return new List<DateTime>();
            }
        }

        public async Task<List<TipoActividadDto>> ObtenerTiposActividades()
        {
            try
            {
                var tipos = await _httpClient.GetFromJsonAsync<List<TipoActividadDto>>("api/calendario/tipos-actividades");
                return tipos ?? new List<TipoActividadDto>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al obtener tipos de recursos: {ex.Message}");
                return new List<TipoActividadDto>();
            }
        }

        public async Task<List<ActividadCalendarioDto>> ObtenerActividadesPorGrupoYFecha(int grupoId, DateTime fecha)
        {
            try
            {
                var actividades = await _httpClient.GetFromJsonAsync<List<ActividadCalendarioDto>>(
                    $"api/calendario/grupo/{grupoId}/fecha?fecha={fecha:yyyy-MM-dd}");
                return actividades ?? new List<ActividadCalendarioDto>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al obtener actividades por grupo y fecha: {ex.Message}");
                return new List<ActividadCalendarioDto>();
            }
        }

        public async Task<int> CrearActividad(CrearActividadCompletaRequest request)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("api/calendario/crear", request);
                
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
                    // El resultado debería tener un campo 'id'
                    if (result.TryGetProperty("id", out var idElement))
                    {
                        return idElement.GetInt32();
                    }
                    return 0;
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Error al crear actividad: {errorContent}");
                    throw new Exception($"Error al crear actividad: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al crear actividad: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> ActualizarActividad(ActualizarActividadRequest request)
        {
            try
            {
                var response = await _httpClient.PutAsJsonAsync("api/calendario/actualizar", request);
                
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
                    // El resultado debería tener un campo 'exito'
                    if (result.TryGetProperty("exito", out var exitoElement))
                    {
                        return exitoElement.GetBoolean();
                    }
                    return true; // Si no hay campo exito, asumimos éxito
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Error al actualizar actividad: {errorContent}");
                    
                    if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
                    {
                        var errorJson = System.Text.Json.JsonDocument.Parse(errorContent);
                        var mensaje = errorJson.RootElement.TryGetProperty("mensaje", out var msg) ? msg.GetString() : "No autorizado";
                        throw new UnauthorizedAccessException(mensaje);
                    }
                    
                    throw new Exception($"Error al actualizar actividad: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al actualizar actividad: {ex.Message}");
                throw;
            }
        }

        public async Task<ActividadCompletaDto> ObtenerActividadCompleta(int idAsignacionAcademicaRecurso)
        {
            try
            {
                var actividad = await _httpClient.GetFromJsonAsync<ActividadCompletaDto>(
                    $"api/calendario/completa/{idAsignacionAcademicaRecurso}");
                return actividad ?? new ActividadCompletaDto();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al obtener actividad completa: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> EliminarActividad(int idAsignacionAcademicaRecurso, int usuarioId)
        {
            try
            {
                var response = await _httpClient.DeleteAsync($"api/calendario/eliminar/{idAsignacionAcademicaRecurso}?usuarioId={usuarioId}");
                
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
                    if (result.TryGetProperty("exito", out var exitoElement))
                    {
                        return exitoElement.GetBoolean();
                    }
                    return true;
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Error al eliminar actividad: {errorContent}");
                    
                    if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
                    {
                        var errorJson = System.Text.Json.JsonDocument.Parse(errorContent);
                        var mensaje = errorJson.RootElement.TryGetProperty("mensaje", out var msg) ? msg.GetString() : "No autorizado";
                        throw new UnauthorizedAccessException(mensaje);
                    }
                    
                    throw new Exception($"Error al eliminar actividad: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al eliminar actividad: {ex.Message}");
                throw;
            }
        }
    }
}
