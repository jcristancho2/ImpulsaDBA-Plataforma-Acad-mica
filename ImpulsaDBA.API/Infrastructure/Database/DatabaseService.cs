using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Threading.Tasks;

namespace ImpulsaDBA.API.Infrastructure.Database
{
    /// <summary>
    /// Servicio para realizar operaciones con la base de datos SQL Server.
    /// 
    /// CONFIGURACIÓN DE CONEXIÓN:
    /// La cadena de conexión se configura en:
    /// 1. appsettings.json -> ConnectionStrings.DefaultConnection (prioridad más alta)
    /// 2. Variables de entorno (.env) -> DB_SERVER, DB_NAME, DB_USER, DB_PASSWORD
    /// 3. Valores por defecto -> localhost, ImpulsaDBA, sa, sin password
    /// 
    /// FORMATO DE CONEXIÓN:
    /// Server=servidor;Database=nombre_bd;User Id=usuario;Password=contraseña;TrustServerCertificate=true;Encrypt=false;
    /// 
    /// EJEMPLOS:
    /// - Local: Server=localhost;Database=ImpulsaDBA;User Id=sa;Password=mi_password;
    /// - Remoto: Server=10.211.55.3;Database=ImpulsaDBA;User Id=sa;Password=mi_password;
    /// - Con instancia: Server=10.211.55.3\\SQLEXPRESS;Database=ImpulsaDBA;User Id=sa;Password=mi_password;
    /// </summary>
    public class DatabaseService
    {
        // Cadena de conexión a SQL Server
        // Se inyecta desde Program.cs donde se construye según la configuración disponible
        private readonly string _connectionString;

        /// <summary>
        /// Constructor que recibe la cadena de conexión configurada.
        /// La conexión se establece de forma asíncrona cuando se ejecuta una operación.
        /// </summary>
        /// <param name="connectionString">Cadena de conexión a SQL Server</param>
        public DatabaseService(string connectionString)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        }

        /// <summary>
        /// Ejecuta una consulta SELECT y retorna los resultados en un DataTable.
        /// 
        /// CONEXIÓN:
        /// - Abre una conexión a SQL Server usando la cadena configurada
        /// - La conexión se cierra automáticamente al finalizar (using statement)
        /// - Si hay error de conexión, se lanza una excepción con detalles
        /// 
        /// USO:
        /// var query = "SELECT * FROM Usuarios WHERE Email = @Email";
        /// var parameters = new Dictionary&lt;string, object&gt; { { "@Email", "user@example.com" } };
        /// var result = await ExecuteQueryAsync(query, parameters);
        /// </summary>
        /// <param name="query">Consulta SQL SELECT</param>
        /// <param name="parameters">Parámetros opcionales para la consulta (previene SQL injection)</param>
        /// <returns>DataTable con los resultados de la consulta</returns>
        public async Task<DataTable> ExecuteQueryAsync(string query, Dictionary<string, object>? parameters = null)
        {
            try
            {
                // Crear y abrir conexión a SQL Server
                // La conexión se cierra automáticamente al salir del bloque using
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    
                    using (var command = new SqlCommand(query, connection))
                    {
                        // Agregar parámetros si existen (previene SQL injection)
                        if (parameters != null)
                        {
                            foreach (var param in parameters)
                            {
                                command.Parameters.AddWithValue(param.Key, param.Value ?? DBNull.Value);
                            }
                        }

                        // Ejecutar consulta y llenar DataTable
                        using (var adapter = new SqlDataAdapter(command))
                        {
                            var dataTable = new DataTable();
                            adapter.Fill(dataTable);
                            return dataTable;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error en ExecuteQueryAsync: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Ejecuta comandos INSERT, UPDATE o DELETE y retorna el número de filas afectadas.
        /// 
        /// CONEXIÓN:
        /// - Abre una conexión a SQL Server usando la cadena configurada
        /// - La conexión se cierra automáticamente al finalizar
        /// 
        /// USO:
        /// var query = "INSERT INTO Usuarios (Email, Nombre) VALUES (@Email, @Nombre)";
        /// var parameters = new Dictionary&lt;string, object&gt; { { "@Email", "user@example.com" }, { "@Nombre", "Juan" } };
        /// var rowsAffected = await ExecuteNonQueryAsync(query, parameters);
        /// </summary>
        /// <param name="query">Comando SQL (INSERT, UPDATE, DELETE)</param>
        /// <param name="parameters">Parámetros opcionales para el comando</param>
        /// <returns>Número de filas afectadas</returns>
        public async Task<int> ExecuteNonQueryAsync(string query, Dictionary<string, object>? parameters = null)
        {
            try
            {
                // Crear y abrir conexión a SQL Server
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    
                    using (var command = new SqlCommand(query, connection))
                    {
                        // Agregar parámetros si existen
                        if (parameters != null)
                        {
                            foreach (var param in parameters)
                            {
                                command.Parameters.AddWithValue(param.Key, param.Value ?? DBNull.Value);
                            }
                        }

                        // Ejecutar comando y retornar filas afectadas
                        return await command.ExecuteNonQueryAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error en ExecuteNonQueryAsync: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Ejecuta una consulta que retorna un solo valor (escalar).
        /// Útil para COUNT, MAX, MIN, o consultas que retornan un único valor.
        /// 
        /// CONEXIÓN:
        /// - Abre una conexión a SQL Server usando la cadena configurada
        /// - La conexión se cierra automáticamente al finalizar
        /// 
        /// USO:
        /// var query = "SELECT COUNT(*) FROM Usuarios WHERE Activo = 1";
        /// var count = await ExecuteScalarAsync(query);
        /// </summary>
        /// <param name="query">Consulta SQL que retorna un solo valor</param>
        /// <param name="parameters">Parámetros opcionales para la consulta</param>
        /// <returns>El valor escalar retornado por la consulta</returns>
        public async Task<object?> ExecuteScalarAsync(string query, Dictionary<string, object>? parameters = null)
        {
            try
            {
                // Crear y abrir conexión a SQL Server
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    
                    using (var command = new SqlCommand(query, connection))
                    {
                        // Agregar parámetros si existen
                        if (parameters != null)
                        {
                            foreach (var param in parameters)
                            {
                                command.Parameters.AddWithValue(param.Key, param.Value ?? DBNull.Value);
                            }
                        }

                        // Ejecutar consulta y retornar valor escalar
                        return await command.ExecuteScalarAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error en ExecuteScalarAsync: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Verifica si la conexión a la base de datos SQL Server es exitosa.
        /// 
        /// CONEXIÓN:
        /// - Intenta abrir una conexión usando la cadena configurada
        /// - Si la conexión es exitosa, retorna true
        /// - Si hay error, muestra mensajes descriptivos según el tipo de error
        /// 
        /// TIPOS DE ERROR:
        /// - Red/Timeout: Problemas de conectividad, IP incorrecta, puerto cerrado, firewall
        /// - Autenticación: Usuario o contraseña incorrectos
        /// - Otros: Errores generales de conexión
        /// </summary>
        /// <returns>true si la conexión es exitosa, false en caso contrario</returns>
        public async Task<bool> TestConnectionAsync()
        {
            try
            {
                // Intentar abrir conexión a SQL Server
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    // Verificar que el estado de la conexión sea Open
                    return connection.State == ConnectionState.Open;
                }
            }
            catch (Exception ex)
            {
                var errorMessage = ex.Message;
                // Mensajes más descriptivos según el tipo de error de conexión
                if (errorMessage.Contains("network-related") || errorMessage.Contains("timeout"))
                {
                    Console.WriteLine($"Error de red: No se pudo conectar al servidor. Verifica:");
                    Console.WriteLine($"  - Que el servidor SQL esté ejecutándose");
                    Console.WriteLine($"  - Que la IP/hostname sea correcta");
                    Console.WriteLine($"  - Que el puerto esté abierto (por defecto 1433)");
                    Console.WriteLine($"  - Que el firewall de Windows permita conexiones entrantes");
                }
                else if (errorMessage.Contains("login failed") || errorMessage.Contains("authentication"))
                {
                    Console.WriteLine($"Error de autenticación: Usuario o contraseña incorrectos");
                }
                else
                {
                    Console.WriteLine($"Error al conectar a la base de datos: {errorMessage}");
                }
                return false;
            }
        }
    }
}