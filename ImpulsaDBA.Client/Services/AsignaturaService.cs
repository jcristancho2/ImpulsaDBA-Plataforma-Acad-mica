using System.Net.Http.Json;
using ImpulsaDBA.Shared.DTOs;

namespace ImpulsaDBA.Client.Services
{
    public class AsignaturaService
    {
        private readonly HttpClient _httpClient;

        /// <summary>
        /// Color hex por defecto cuando la API no devuelve color (no se usan variables CSS).
        /// </summary>
        private const string ColorHexDefault = "#e0e0e0";

        public AsignaturaService(HttpClient httpClient)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        }

        public async Task<List<AsignaturaDto>> ObtenerAsignaturasPorProfesor(int profesorId)
        {
            try
            {
                var asignaturas = await _httpClient.GetFromJsonAsync<List<AsignaturaDto>>($"api/asignaturas/profesor/{profesorId}");
                
                if (asignaturas != null)
                {
                    // Fallback: si la API no devolvió color, usar color hex por defecto (no CSS)
                    foreach (var asignatura in asignaturas)
                    {
                        if (string.IsNullOrWhiteSpace(asignatura.ColorHex))
                            asignatura.ColorHex = ColorHexDefault;
                    }
                }
                
                return asignaturas ?? new List<AsignaturaDto>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al obtener asignaturas del profesor: {ex.Message}");
                return new List<AsignaturaDto>();
            }
        }

        public async Task<int> ObtenerCantidadEstudiantesPorGrupo(int grupoId)
        {
            try
            {
                var response = await _httpClient.GetFromJsonAsync<dynamic>($"api/asignaturas/grupo/{grupoId}/estudiantes");
                return response?.cantidad ?? 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al obtener cantidad de estudiantes: {ex.Message}");
                return 0;
            }
        }

        public async Task<EstadisticasActividadesDto> ObtenerEstadisticasActividades(int idAsignacionAcademica)
        {
            try
            {
                var estadisticas = await _httpClient.GetFromJsonAsync<EstadisticasActividadesDto>(
                    $"api/asignaturas/asignacion/{idAsignacionAcademica}/estadisticas");
                return estadisticas ?? new EstadisticasActividadesDto();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al obtener estadísticas de actividades: {ex.Message}");
                return new EstadisticasActividadesDto();
            }
        }

        public async Task<(int PeriodoId, string Periodo, int AnioId, string Anio)> ObtenerPeriodoActual()
        {
            try
            {
                var response = await _httpClient.GetFromJsonAsync<dynamic>("api/asignaturas/periodo-actual");
                
                if (response != null)
                {
                    return (
                        response.periodoId ?? 0,
                        response.periodo?.ToString() ?? "1",
                        response.anioId ?? 0,
                        response.anio?.ToString() ?? DateTime.Now.Year.ToString()
                    );
                }
                
                return (0, "1", 0, DateTime.Now.Year.ToString());
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al obtener periodo actual: {ex.Message}");
                return (0, "1", 0, DateTime.Now.Year.ToString());
            }
        }

        public async Task<string> ObtenerNombreInstitucion(int colegioId)
        {
            // Este método puede necesitar un endpoint adicional en la API
            // Por ahora retornamos un valor por defecto
            return "INSTITUCIÓN EDUCATIVA";
        }

        public async Task<int> ObtenerColegioIdPorProfesor(int profesorId)
        {
            try
            {
                var response = await _httpClient.GetFromJsonAsync<dynamic>($"api/asignaturas/colegio/profesor/{profesorId}");
                return response?.colegioId ?? 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al obtener colegio del profesor: {ex.Message}");
                return 0;
            }
        }

        public async Task<int> ObtenerPrimeraAsignacionAcademicaPorGrupo(int grupoId)
        {
            try
            {
                var response = await _httpClient.GetFromJsonAsync<dynamic>($"api/asignaturas/grupo/{grupoId}/primera-asignacion");
                return response?.idAsignacionAcademica ?? 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al obtener asignación académica del grupo: {ex.Message}");
                return 0;
            }
        }
    }
}
