using System.Net.Http.Json;
using ImpulsaDBA.Shared.DTOs;
using System.Text.Json;

namespace ImpulsaDBA.Client.Services
{
    public class AyudaService
    {
        private readonly HttpClient _httpClient;

        public AyudaService(HttpClient httpClient)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        }

        public async Task<(AyudaDto? PDF, AyudaDto? VIDEO)> ObtenerAyudasPorComponente(int idComponente)
        {
            try
            {
                Console.WriteLine($"🌐 Cliente - Llamando API: api/ayudas/componente/{idComponente}");
                var response = await _httpClient.GetAsync($"api/ayudas/componente/{idComponente}");
                
                Console.WriteLine($"🌐 Cliente - Respuesta status: {response.StatusCode}");
                
                if (response.IsSuccessStatusCode)
                {
                    var jsonString = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"🌐 Cliente - JSON recibido (primeros 500 chars): {jsonString.Substring(0, Math.Min(500, jsonString.Length))}");
                    
                    var jsonDoc = JsonDocument.Parse(jsonString);
                    var root = jsonDoc.RootElement;
                    
                    Console.WriteLine($"🌐 Cliente - Root element tiene propiedades: {root.EnumerateObject().Count()}");
                    foreach (var prop in root.EnumerateObject())
                    {
                        Console.WriteLine($"   Propiedad: {prop.Name}, ValueKind: {prop.Value.ValueKind}");
                    }

                    AyudaDto? pdf = null;
                    AyudaDto? video = null;

                    // Configurar opciones de deserialización para manejar case-insensitive
                    var jsonOptions = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    };

                    if (root.TryGetProperty("pdf", out var pdfElement) && pdfElement.ValueKind != JsonValueKind.Null)
                    {
                        try
                        {
                            pdf = JsonSerializer.Deserialize<AyudaDto>(pdfElement.GetRawText(), jsonOptions);
                            Console.WriteLine($"🌐 Cliente - PDF deserializado: {(pdf != null ? $"Sí (Id: {pdf.Id}, URL: {pdf.UrlAyuda}, Nombre: {pdf.NombreAyuda})" : "No")}");
                            if (pdf != null)
                            {
                                Console.WriteLine($"   PDF.Id: {pdf.Id}");
                                Console.WriteLine($"   PDF.UrlAyuda: '{pdf.UrlAyuda}'");
                                Console.WriteLine($"   PDF.UrlAyuda es null o vacío: {string.IsNullOrEmpty(pdf.UrlAyuda)}");
                            }
                        }
                        catch (Exception exPdf)
                        {
                            Console.WriteLine($"❌ Cliente - Error al deserializar PDF: {exPdf.Message}");
                            Console.WriteLine($"❌ Cliente - Stack trace PDF: {exPdf.StackTrace}");
                        }
                    }
                    else
                    {
                        Console.WriteLine($"🌐 Cliente - PDF no encontrado en respuesta");
                        if (root.TryGetProperty("pdf", out var _))
                        {
                            Console.WriteLine($"   pdfElement.ValueKind: {root.GetProperty("pdf").ValueKind}");
                        }
                    }

                    if (root.TryGetProperty("video", out var videoElement) && videoElement.ValueKind != JsonValueKind.Null)
                    {
                        try
                        {
                            video = JsonSerializer.Deserialize<AyudaDto>(videoElement.GetRawText(), jsonOptions);
                            Console.WriteLine($"🌐 Cliente - VIDEO deserializado: {(video != null ? $"Sí (Id: {video.Id}, URL: {video.UrlAyuda}, Nombre: {video.NombreAyuda})" : "No")}");
                            if (video != null)
                            {
                                Console.WriteLine($"   VIDEO.Id: {video.Id}");
                                Console.WriteLine($"   VIDEO.UrlAyuda: '{video.UrlAyuda}'");
                                Console.WriteLine($"   VIDEO.UrlAyuda es null o vacío: {string.IsNullOrEmpty(video.UrlAyuda)}");
                            }
                        }
                        catch (Exception exVideo)
                        {
                            Console.WriteLine($"❌ Cliente - Error al deserializar VIDEO: {exVideo.Message}");
                            Console.WriteLine($"❌ Cliente - Stack trace VIDEO: {exVideo.StackTrace}");
                        }
                    }
                    else
                    {
                        Console.WriteLine($"🌐 Cliente - VIDEO no encontrado en respuesta");
                        if (root.TryGetProperty("video", out var _))
                        {
                            Console.WriteLine($"   videoElement.ValueKind: {root.GetProperty("video").ValueKind}");
                        }
                    }

                    return (pdf, video);
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"❌ Cliente - Error en respuesta: {response.StatusCode} - {errorContent}");
                }

                return (null, null);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Cliente - Error al obtener ayudas por componente: {ex.Message}");
                Console.WriteLine($"❌ Cliente - Stack trace: {ex.StackTrace}");
                return (null, null);
            }
        }
    }
}
