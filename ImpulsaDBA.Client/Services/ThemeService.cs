using System.Net.Http.Json;
using ImpulsaDBA.Shared.DTOs;
using Microsoft.JSInterop;

namespace ImpulsaDBA.Client.Services
{
    /// <summary>
    /// Servicio para cargar la paleta de colores desde la API y aplicarla como variables CSS.
    /// </summary>
    public class ThemeService
    {
        private readonly HttpClient _httpClient;
        private readonly IJSRuntime _jsRuntime;

        public ThemeService(HttpClient httpClient, IJSRuntime jsRuntime)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _jsRuntime = jsRuntime ?? throw new ArgumentNullException(nameof(jsRuntime));
        }

        /// <summary>
        /// Carga la paleta desde api/paleta y la aplica mediante window.impulsaTheme.applyPalette.
        /// </summary>
        public async Task CargarYAplicarPaletaAsync()
        {
            try
            {
                var colores = await _httpClient.GetFromJsonAsync<List<ColorPaletaDto>>("api/paleta");
                if (colores == null || colores.Count == 0)
                    return;

                await _jsRuntime.InvokeVoidAsync("impulsaTheme.applyPalette", colores);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al cargar/aplicar paleta de colores: {ex.Message}");
            }
        }
    }
}
