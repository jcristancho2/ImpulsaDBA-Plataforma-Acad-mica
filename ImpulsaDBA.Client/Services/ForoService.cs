using System.Net.Http.Json;
using ImpulsaDBA.Shared.DTOs;
using ImpulsaDBA.Shared.Requests;

namespace ImpulsaDBA.Client.Services
{
    public class ForoService
    {
        private readonly HttpClient _httpClient;

        public ForoService(HttpClient httpClient)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        }

        public async Task<List<ForoDto>> ObtenerPorAsignacionAsync(int idAsignacionAcademica)
        {
            try
            {
                var list = await _httpClient.GetFromJsonAsync<List<ForoDto>>(
                    $"api/foro/asignacion/{idAsignacionAcademica}");
                return list ?? new List<ForoDto>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al obtener comentarios del foro: {ex.Message}");
                return new List<ForoDto>();
            }
        }

        /// <summary>
        /// Listado de comentarios para el docente (incluye eliminados).
        /// </summary>
        public async Task<List<ForoDto>> ObtenerPorAsignacionDocenteAsync(int idAsignacionAcademica)
        {
            try
            {
                var list = await _httpClient.GetFromJsonAsync<List<ForoDto>>(
                    $"api/foro/asignacion/{idAsignacionAcademica}/docente");
                return list ?? new List<ForoDto>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al obtener comentarios del foro (docente): {ex.Message}");
                return new List<ForoDto>();
            }
        }

        public async Task<ForoDto?> CrearComentarioAsync(CrearComentarioForoRequest request)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("api/foro", request);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<ForoDto>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al crear comentario: {ex.Message}");
                return null;
            }
        }

        public async Task<bool> ActualizarComentarioAsync(ActualizarComentarioForoRequest request)
        {
            try
            {
                var response = await _httpClient.PutAsJsonAsync("api/foro", request);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al actualizar comentario: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> EliminarComentarioAsync(int idForo, int idPersona, int idAsignacionAcademica)
        {
            try
            {
                var response = await _httpClient.DeleteAsync(
                    $"api/foro/{idForo}/persona/{idPersona}?idAsignacionAcademica={idAsignacionAcademica}");
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al eliminar comentario: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Historial de ediciones de un comentario (solo docente).
        /// </summary>
        public async Task<List<ForoHistorialDto>> ObtenerHistorialAsync(int idForo)
        {
            try
            {
                var list = await _httpClient.GetFromJsonAsync<List<ForoHistorialDto>>(
                    $"api/foro/{idForo}/historial");
                return list ?? new List<ForoHistorialDto>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al obtener historial del comentario: {ex.Message}");
                return new List<ForoHistorialDto>();
            }
        }
    }
}
