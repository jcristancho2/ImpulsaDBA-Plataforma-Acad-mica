using Microsoft.Data.SqlClient;
using System.Data;

namespace ImpulsaDBA.Services
{
    /// <summary>
    /// Fábrica de conexiones a la base de datos SQL Server.
    /// Utiliza la cadena de conexión configurada en appsettings.json.
    /// </summary>
    public class DbConnectionFactory
    {
        private readonly IConfiguration _configuration;

        public DbConnectionFactory(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        /// <summary>
        /// Crea una nueva conexión a la base de datos usando la cadena de conexión configurada.
        /// </summary>
        /// <returns>Una instancia de IDbConnection configurada pero no abierta</returns>
        public IDbConnection CreateConnection()
            => new SqlConnection(
                _configuration.GetConnectionString("DefaultConnection"));
    }
}
