using Blazored.LocalStorage;
using CurrieTechnologies.Razor.SweetAlert2;
using ImpulsaDBA.Client;
using ImpulsaDBA.Client.Services;
using ImpulsaDBA.Client.Auth;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Configurar HttpClient base para llamadas a la API
// WebAssemblyHostBuilder.CreateDefault carga automáticamente appsettings.json y appsettings.{Environment}.json desde wwwroot
var apiBaseUrl = builder.Configuration["ApiBaseUrl"];

// Si no hay configuración, usar valores por defecto según el entorno
if (string.IsNullOrWhiteSpace(apiBaseUrl))
{
    if (builder.HostEnvironment.IsDevelopment())
    {
        // En desarrollo, la API corre en http://localhost:5001
        apiBaseUrl = "http://localhost:5001";
    }
    else
    {
        // En producción, usar la misma URL base del cliente (si API y cliente están en el mismo servidor)
        apiBaseUrl = builder.HostEnvironment.BaseAddress;
    }
}

// Log para diagnóstico (visible en la consola del navegador)
Console.WriteLine($"🔗 HttpClient BaseAddress configurado: {apiBaseUrl}");
Console.WriteLine($"🔗 Entorno: {builder.HostEnvironment.Environment}");
Console.WriteLine($"🔗 BaseAddress del cliente: {builder.HostEnvironment.BaseAddress}");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(apiBaseUrl) });

// Servicios de Blazor
builder.Services.AddBlazoredLocalStorage();
builder.Services.AddSweetAlert2();

// Servicios de autenticación
builder.Services.AddScoped<CustomAuthStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(sp =>
    sp.GetRequiredService<CustomAuthStateProvider>());
builder.Services.AddAuthorizationCore();

// Servicios de la aplicación (ahora usan HttpClient para llamar a la API)
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<AsignaturaService>();
builder.Services.AddScoped<CalendarioService>();
builder.Services.AddScoped<AyudaService>();
builder.Services.AddScoped<ForoService>();
builder.Services.AddScoped<ThemeService>();

var host = builder.Build();

// Cargar paleta de colores al iniciar la aplicación
try
{
    using var scope = host.Services.CreateScope();
    var themeService = scope.ServiceProvider.GetRequiredService<ThemeService>();
    await themeService.CargarYAplicarPaletaAsync();
}
catch (Exception ex)
{
    Console.WriteLine($"Error al inicializar la paleta de colores: {ex.Message}");
}

await host.RunAsync();
