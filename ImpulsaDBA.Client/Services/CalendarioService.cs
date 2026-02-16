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

        /// <summary>
        /// Devuelve la URL para descargar/abrir un archivo guardado (por id de tab.archivo).
        /// </summary>
        public string GetUrlDescargaArchivo(int idArchivo)
        {
            var baseUrl = _httpClient.BaseAddress?.ToString()?.TrimEnd('/') ?? "";
            return $"{baseUrl}/api/calendario/archivo/{idArchivo}";
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

        public async Task<List<ActividadParaDuplicarDto>> ObtenerActividadesParaDuplicarAsync(int profesorId, int idAsignatura)
        {
            try
            {
                var list = await _httpClient.GetFromJsonAsync<List<ActividadParaDuplicarDto>>(
                    $"api/calendario/duplicar/actividades?profesorId={profesorId}&idAsignatura={idAsignatura}");
                return list ?? new List<ActividadParaDuplicarDto>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al obtener actividades para duplicar: {ex.Message}");
                return new List<ActividadParaDuplicarDto>();
            }
        }

        public async Task<List<GrupoMismoGradoDto>> ObtenerGruposMismoGradoAsync(int idAsignacionAcademica, int profesorId)
        {
            try
            {
                var list = await _httpClient.GetFromJsonAsync<List<GrupoMismoGradoDto>>(
                    $"api/calendario/duplicar/grupos-mismo-grado?idAsignacionAcademica={idAsignacionAcademica}&profesorId={profesorId}");
                return list ?? new List<GrupoMismoGradoDto>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al obtener grupos mismo grado: {ex.Message}");
                return new List<GrupoMismoGradoDto>();
            }
        }

        public async Task MoverActividadAsync(int idAsignacionAcademicaRecurso, DateTime nuevaFecha)
        {
            var url = $"api/calendario/mover/{idAsignacionAcademicaRecurso}?nuevaFecha={nuevaFecha:yyyy-MM-ddTHH:mm:ss}";
            var response = await _httpClient.PutAsync(url, null);
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Error al mover actividad: {errorContent}");
                throw new Exception("Error al mover la actividad.");
            }
        }

        public async Task DuplicarActividadAsync(DuplicarActividadRequest request)
        {
            var response = await _httpClient.PostAsJsonAsync("api/calendario/duplicar", request);
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Error al duplicar actividad: {errorContent}");
                throw new Exception(response.StatusCode == System.Net.HttpStatusCode.BadRequest
                    ? (System.Text.Json.JsonDocument.Parse(errorContent).RootElement.TryGetProperty("error", out var err) ? err.GetString() : "Solicitud incorrecta")
                    : "Error al duplicar la actividad.");
            }
        }

        /// <summary>Lista actividades con entregas para que el docente califique (opcional filtro por grupo).</summary>
        public async Task<List<ActividadParaCalificarDto>> ObtenerActividadesParaCalificarAsync(int profesorId, int? idGrupo)
        {
            try
            {
                var url = $"api/calendario/docente/calificar/actividades?profesorId={profesorId}";
                if (idGrupo.HasValue && idGrupo.Value > 0)
                    url += $"&idGrupo={idGrupo.Value}";
                var list = await _httpClient.GetFromJsonAsync<List<ActividadParaCalificarDto>>(url);
                return list ?? new List<ActividadParaCalificarDto>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al obtener actividades para calificar: {ex.Message}");
                return new List<ActividadParaCalificarDto>();
            }
        }

        /// <summary>Lista entregas de estudiantes por recurso para calificar.</summary>
        public async Task<List<EntregaParaCalificarDto>> ObtenerEntregasParaCalificarAsync(int idRecurso)
        {
            try
            {
                var list = await _httpClient.GetFromJsonAsync<List<EntregaParaCalificarDto>>(
                    $"api/calendario/docente/calificar/entregas?idRecurso={idRecurso}");
                return list ?? new List<EntregaParaCalificarDto>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al obtener entregas para calificar: {ex.Message}");
                return new List<EntregaParaCalificarDto>();
            }
        }

        /// <summary>Asigna o actualiza la nota de una entrega.</summary>
        public async Task<bool> AsignarNotaAsync(int idNotaEstudiante, decimal nota)
        {
            try
            {
                var response = await _httpClient.PutAsJsonAsync("api/calendario/docente/calificar/nota",
                    new AsignarNotaRequest { IdNotaEstudiante = idNotaEstudiante, Nota = nota });
                if (!response.IsSuccessStatusCode) return false;
                var json = await response.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
                return json.TryGetProperty("exito", out var exito) && exito.GetBoolean();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al asignar nota: {ex.Message}");
                return false;
            }
        }

        /// <summary>URL para ver o descargar un archivo de entrega de estudiante (ruta relativa). Si descargar es true y se pasa nombreOriginal, la descarga usará ese nombre.</summary>
        public string GetUrlArchivoEntrega(string rutaRelativa, bool descargar = false, string? nombreOriginal = null)
        {
            if (string.IsNullOrWhiteSpace(rutaRelativa)) return "#";
            var baseUrl = _httpClient.BaseAddress?.ToString()?.TrimEnd('/') ?? "";
            var url = $"{baseUrl}/api/archivos/servir?ruta={Uri.EscapeDataString(rutaRelativa)}";
            if (descargar)
            {
                url += "&descargar=1";
                if (!string.IsNullOrWhiteSpace(nombreOriginal))
                    url += "&nombre=" + Uri.EscapeDataString(nombreOriginal.Trim());
            }
            return url;
        }
    }
}
