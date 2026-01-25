using System.Data;
using Microsoft.Data.SqlClient;
using ImpulsaDBA.API.Application.Services;
using ImpulsaDBA.API.Infrastructure.Database;
using ImpulsaDBA.API.Infrastructure.Repositories;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configurar CORS para permitir llamadas desde Blazor WebAssembly
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowBlazorClient", policy =>
    {
        policy.WithOrigins(
                "https://localhost:7023",
                "http://localhost:5079",
                "https://localhost:5001",
                "http://localhost:5000",
                builder.Configuration["BlazorClientUrl"] ?? "https://localhost:7023"
            )
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

// Registrar DatabaseService con la cadena de conexión
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddSingleton<ImpulsaDBA.API.Infrastructure.Database.DatabaseService>(sp => new ImpulsaDBA.API.Infrastructure.Database.DatabaseService(connectionString));

builder.Services.AddScoped<IDbConnection>(sp =>
    new SqlConnection(connectionString));

builder.Services.AddScoped<UsuarioRepository>();
builder.Services.AddScoped<ImpulsaDBA.API.Application.Services.AuthService>();
builder.Services.AddScoped<ImpulsaDBA.API.Application.Services.AsignaturaService>();
builder.Services.AddScoped<ImpulsaDBA.API.Application.Services.CalendarioService>();
builder.Services.AddScoped<ImpulsaDBA.API.Application.Services.AyudaService>();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseRouting();
app.UseCors("AllowBlazorClient");
app.MapControllers();

// Logging para diagnóstico
app.Logger.LogInformation("API iniciada. Endpoints disponibles:");
app.Logger.LogInformation("  POST /api/auth/login");
app.Logger.LogInformation("  POST /api/auth/validar-usuario");
app.Logger.LogInformation("  POST /api/auth/validar-password");

app.Run();
