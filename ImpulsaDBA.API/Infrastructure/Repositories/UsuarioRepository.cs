using System.Data;
using Dapper;
using ImpulsaDBA.API.Domain.Entities;

namespace ImpulsaDBA.API.Infrastructure.Repositories;

public class UsuarioRepository
{
    private readonly IDbConnection _db;

    public UsuarioRepository(IDbConnection db)
    {
        _db = db;
    }

    public async Task<IEnumerable<Usuario>> ObtenerUsuariosAsync()
    {
        const string sql = "SELECT * FROM Usuarios";
        return await _db.QueryAsync<Usuario>(sql);
    }

    public async Task<Usuario?> ObtenerUsuarioPorIdAsync(int id)
    {
        const string sql = "SELECT * FROM Usuarios WHERE Id = @Id";
        return await _db.QueryFirstOrDefaultAsync<Usuario>(sql, new { Id = id });
    }

    public async Task<object> ProbarConectividadCompletaAsync()
    {
        try
        {
            var resultados = new Dictionary<string, object>();
            
            // 1. Información básica del servidor
            var infoServidor = await _db.QueryFirstOrDefaultAsync<dynamic>(
                "SELECT @@SERVERNAME AS Servidor, DB_NAME() AS BaseDatos, USER_NAME() AS Usuario, GETDATE() AS FechaHora, @@VERSION AS VersionSQL");
            
            resultados["servidor"] = new
            {
                nombre = infoServidor?.Servidor?.ToString(),
                baseDatos = infoServidor?.BaseDatos?.ToString(),
                usuario = infoServidor?.Usuario?.ToString(),
                fechaHora = infoServidor?.FechaHora?.ToString(),
                version = infoServidor?.VersionSQL?.ToString()
            };
            
            // 2. Contar registros en tablas principales
            var conteos = new Dictionary<string, int>();
            
            try
            {
                var countPersonas = await _db.QueryFirstOrDefaultAsync<int>("SELECT COUNT(*) FROM col.persona");
                conteos["personas"] = countPersonas;
            }
            catch { conteos["personas"] = -1; }
            
            try
            {
                var countGrupos = await _db.QueryFirstOrDefaultAsync<int>("SELECT COUNT(*) FROM aca.grupo");
                conteos["grupos"] = countGrupos;
            }
            catch { conteos["grupos"] = -1; }
            
            try
            {
                var countAsignaturas = await _db.QueryFirstOrDefaultAsync<int>("SELECT COUNT(*) FROM col.asignatura");
                conteos["asignaturas"] = countAsignaturas;
            }
            catch { conteos["asignaturas"] = -1; }
            
            try
            {
                var countEstudiantes = await _db.QueryFirstOrDefaultAsync<int>("SELECT COUNT(DISTINCT id_estudiante) FROM aca.lista");
                conteos["estudiantes"] = countEstudiantes;
            }
            catch { conteos["estudiantes"] = -1; }
            
            resultados["conteos"] = conteos;
            
            // 3. Probar función definida
            try
            {
                var resultadoFuncion = await _db.QueryFirstOrDefaultAsync<int>(
                    "SELECT dbo.fEsUnPeriodo('1', '1;2;3;4;5') AS Resultado");
                resultados["funcion"] = new { resultado = resultadoFuncion, mensaje = "Función ejecutada correctamente" };
            }
            catch (Exception exFunc)
            {
                resultados["funcion"] = new { error = exFunc.Message };
            }
            
            return resultados;
        }
        catch (Exception ex)
        {
            throw new Exception($"Error al probar conectividad: {ex.Message}", ex);
        }
    }
}
