using Microsoft.AspNetCore.Mvc;
using System.Data;
using Microsoft.Data.SqlClient;
using Dapper;
using ImpulsaDBA.API.Infrastructure.Database;

namespace ImpulsaDBA.API.Controllers
{
    /// <summary>
    /// Controlador para verificar el estado de la aplicación y la conexión a la base de datos.
    /// Útil para pruebas rápidas sin necesidad de interactividad en el navegador.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class HealthController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly ImpulsaDBA.API.Infrastructure.Database.DbConnectionFactory _dbFactory;

        public HealthController(IConfiguration configuration, ImpulsaDBA.API.Infrastructure.Database.DbConnectionFactory dbFactory)
        {
            _configuration = configuration;
            _dbFactory = dbFactory;
        }

        /// <summary>
        /// Prueba simple de conexión a la base de datos.
        /// GET: api/health/db
        /// </summary>
        [HttpGet("db")]
        public async Task<IActionResult> TestDatabase()
        {
            try
            {
                var connectionString = _configuration.GetConnectionString("DefaultConnection");
                
                if (string.IsNullOrEmpty(connectionString))
                {
                    return BadRequest(new 
                    { 
                        exito = false,
                        mensaje = "No se encontró la cadena de conexión en appsettings.json",
                        sugerencia = "Verifica que 'ConnectionStrings:DefaultConnection' esté configurada"
                    });
                }

                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();
                
                var command = new SqlCommand("SELECT @@SERVERNAME AS Servidor, DB_NAME() AS BaseDatos, USER_NAME() AS Usuario, GETDATE() AS FechaHora", connection);
                var reader = await command.ExecuteReaderAsync();
                
                if (await reader.ReadAsync())
                {
                    return Ok(new 
                    { 
                        exito = true,
                        mensaje = "✅ Conexión exitosa a la base de datos",
                        servidor = reader["Servidor"]?.ToString(),
                        baseDatos = reader["BaseDatos"]?.ToString(),
                        usuario = reader["Usuario"]?.ToString(),
                        fechaHora = reader["FechaHora"]?.ToString(),
                        cadenaConexion = OcultarPassword(connectionString)
                    });
                }
                
                return Ok(new { exito = true, mensaje = "Conexión establecida pero no se pudo leer información" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new 
                { 
                    exito = false,
                    mensaje = "❌ Error al conectar con la base de datos",
                    error = ex.Message,
                    detalles = ex.InnerException?.Message,
                    sugerencias = new[]
                    {
                        "Verifica que SQL Server esté ejecutándose",
                        "Verifica que la cadena de conexión sea correcta en appsettings.json",
                        "Verifica que el servidor sea accesible desde esta máquina",
                        "Verifica credenciales (usuario y contraseña)"
                    }
                });
            }
        }

        /// <summary>
        /// Prueba completa de conectividad usando Dapper.
        /// GET: api/health/db-full
        /// </summary>
        [HttpGet("db-full")]
        public async Task<IActionResult> TestDatabaseFull()
        {
            try
            {
                var resultados = await _dbFactory.CreateConnection().QueryFirstOrDefaultAsync<dynamic>(
                    "SELECT @@SERVERNAME AS Servidor, DB_NAME() AS BaseDatos, USER_NAME() AS Usuario, GETDATE() AS FechaHora");
                
                var conteos = new Dictionary<string, int>();
                using var connection = _dbFactory.CreateConnection();
                
                conteos["personas"] = await connection.QueryFirstOrDefaultAsync<int>("SELECT COUNT(*) FROM col.persona");
                conteos["grupos"] = await connection.QueryFirstOrDefaultAsync<int>("SELECT COUNT(*) FROM aca.grupo");
                conteos["asignaturas"] = await connection.QueryFirstOrDefaultAsync<int>("SELECT COUNT(*) FROM col.asignatura");
                conteos["estudiantes"] = await connection.QueryFirstOrDefaultAsync<int>("SELECT COUNT(DISTINCT id_estudiante) FROM aca.lista");
                
                return Ok(new 
                { 
                    exito = true,
                    mensaje = "✅ Prueba completa exitosa",
                    servidor = resultados?.Servidor,
                    baseDatos = resultados?.BaseDatos,
                    usuario = resultados?.Usuario,
                    fechaHora = resultados?.FechaHora,
                    conteos = conteos
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new 
                { 
                    exito = false,
                    mensaje = "❌ Error en la prueba completa",
                    error = ex.Message,
                    detalles = ex.InnerException?.Message
                });
            }
        }

        /// <summary>
        /// Muestra información de la configuración (sin exponer contraseñas).
        /// GET: api/health/config
        /// </summary>
        [HttpGet("config")]
        public IActionResult GetConfig()
        {
            var connectionString = _configuration.GetConnectionString("DefaultConnection");
            
            return Ok(new 
            { 
                tieneCadenaConexion = !string.IsNullOrEmpty(connectionString),
                cadenaConexionOculta = OcultarPassword(connectionString ?? ""),
                variablesEntorno = new
                {
                    DB_SERVER = Environment.GetEnvironmentVariable("DB_SERVER") ?? "No configurada",
                    DB_NAME = Environment.GetEnvironmentVariable("DB_NAME") ?? "No configurada",
                    DB_USER = Environment.GetEnvironmentVariable("DB_USER") ?? "No configurada",
                    DB_PASSWORD = string.IsNullOrEmpty(Environment.GetEnvironmentVariable("DB_PASSWORD")) ? "No configurada" : "***"
                }
            });
        }

        private string OcultarPassword(string connectionString)
        {
            if (string.IsNullOrEmpty(connectionString))
                return "";
            
            // Ocultar la contraseña en la cadena de conexión para mostrarla de forma segura
            var parts = connectionString.Split(';');
            var result = new List<string>();
            
            foreach (var part in parts)
            {
                if (part.Trim().StartsWith("Password=", StringComparison.OrdinalIgnoreCase) ||
                    part.Trim().StartsWith("Pwd=", StringComparison.OrdinalIgnoreCase))
                {
                    result.Add("Password=***");
                }
                else
                {
                    result.Add(part);
                }
            }
            
            return string.Join(";", result);
        }
    }
}
