using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using ImpulsaDBA.API.Domain.Entities;
using ImpulsaDBA.API.Infrastructure.Database;
using ImpulsaDBA.Shared.DTOs;

namespace ImpulsaDBA.API.Application.Services
{
    public class AsignaturaService
    {
        private readonly ImpulsaDBA.API.Infrastructure.Database.DatabaseService _databaseService;

        // Paleta de colores pastel para asignar a las cards de asignaturas.
        // Los valores corresponden a variables definidas en wwwroot/css/app.css
        private static readonly string[] PastelColorVariables = new[]
        {
            "--color-pastel-sky-blue",
            "--color-pastel-blue-green",
            "--color-pastel-ocean",
            "--color-pastel-teal",
            "--color-pastel-verde",
            "--color-pastel-salmon",
            "--color-pastel-tulip",
            "--color-pastel-amber",
            "--color-pastel-mauve",
            "--color-pastel-peach"
        };

        public AsignaturaService(ImpulsaDBA.API.Infrastructure.Database.DatabaseService databaseService)
        {
            _databaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService));
        }

        /// <summary>
        /// Obtiene las asignaturas asignadas a un profesor usando la estructura de tablas y vistas
        /// indicada: persona, plla.View_Asignacion_Academica, grupo, sede, anio, colegio y lista.
        /// </summary>
        public async Task<List<AsignaturaDto>> ObtenerAsignaturasPorProfesor(int profesorId)
        {
            try
            {
                var query = @"
                    SELECT 
                        AA.id              AS id_asignacion_academica,
                        P.id               AS id_profesor,
                        A.id               AS id_asignatura,
                        G.id               AS id_grupo,
                        A.asignatura       AS nombre,
                        G.nombre           AS nombre_grupo,
                        C.colegio          AS nombre_institucion,
                        S.sede             AS nombre_sede,
                        (
                            SELECT COUNT(*) 
                            FROM aca.lista AS L 
                            WHERE L.id_grupo = G.id 
                              AND ISNULL(L.inactivo, 0) = 0
                        ) AS cantidad_estudiantes
                    FROM 
                        col.persona AS P
                    INNER JOIN 
                        plla.View_Asignacion_Academica AS AA ON P.id = AA.id_profesor
                    INNER JOIN 
                        aca.grupo AS G ON AA.id_grupo = G.id
                    INNER JOIN 
                        col.sede AS S ON G.id_sede = S.id
                    INNER JOIN 
                        bas.anio AS AN ON G.id_anio = AN.id
                    INNER JOIN 
                        bas.colegio AS C ON AN.id_colegio = C.id
                    INNER JOIN 
                        col.asignatura AS A ON AA.id_asignatura = A.id
                    WHERE 
                        P.id = @ProfesorId
                    ORDER BY 
                        A.asignatura, G.nombre";

                var parameters = new Dictionary<string, object>
                {
                    { "@ProfesorId", profesorId }
                };

                var result = await _databaseService.ExecuteQueryAsync(query, parameters);
                var asignaturas = new List<AsignaturaDto>();

                foreach (DataRow row in result.Rows)
                {
                    var asignaturaId = Convert.ToInt32(row["id_asignatura"]);
                    var idAsignacionAcademica = Convert.ToInt32(row["id_asignacion_academica"]);
                    
                    var asignatura = new AsignaturaDto
                    {
                        Id = asignaturaId,
                        IdAsignacionAcademica = idAsignacionAcademica,
                        Nombre = row["nombre"]?.ToString() ?? string.Empty,
                        ProfesorId = Convert.ToInt32(row["id_profesor"]),
                        GrupoId = Convert.ToInt32(row["id_grupo"]),
                        Grupo = row["nombre_grupo"]?.ToString() ?? string.Empty,
                        Institucion = row["nombre_institucion"]?.ToString() 
                                      ?? row["nombre_sede"]?.ToString() 
                                      ?? string.Empty
                    };

                    // Asignar un color pastel pseudo-aleatorio pero estable por asignatura,
                    // usando el id de la asignatura como base.
                    var colorIndex = Math.Abs(asignatura.Id.GetHashCode()) % PastelColorVariables.Length;
                    asignatura.ColorHex = $"var({PastelColorVariables[colorIndex]})";

                    // Cantidad de estudiantes ya viene contada en la consulta
                    asignatura.CantidadEstudiantes = Convert.ToInt32(row["cantidad_estudiantes"] ?? 0);

                    // Obtener estadísticas de actividades (usa la asignación académica)
                    var estadisticas = await ObtenerEstadisticasActividades(idAsignacionAcademica);
                    asignatura.Estadisticas = estadisticas != null ? new EstadisticasActividadesDto
                    {
                        Totales = estadisticas.Totales,
                        Activas = estadisticas.Activas,
                        Inactivas = estadisticas.Inactivas,
                        Pendientes = estadisticas.Pendientes
                    } : null;

                    asignaturas.Add(asignatura);
                }

                return asignaturas;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al obtener asignaturas del profesor: {ex.Message}");
                return new List<AsignaturaDto>();
            }
        }

        /// <summary>
        /// Obtiene la cantidad de estudiantes en un grupo
        /// </summary>
        public async Task<int> ObtenerCantidadEstudiantesPorGrupo(int grupoId)
        {
            try
            {
                var query = @"
                    SELECT COUNT(*) AS cantidad
                    FROM aca.lista
                    WHERE id_grupo = @GrupoId";

                var parameters = new Dictionary<string, object>
                {
                    { "@GrupoId", grupoId }
                };

                var result = await _databaseService.ExecuteScalarAsync(query, parameters);
                return result != null ? Convert.ToInt32(result) : 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al obtener cantidad de estudiantes: {ex.Message}");
                return 0;
            }
        }

        /// <summary>
        /// Obtiene estadísticas de actividades para una asignación académica
        /// Relaciona actividades por año y periodo de la asignación académica
        /// </summary>
        public async Task<EstadisticasActividades> ObtenerEstadisticasActividades(int idAsignacionAcademica)
        {
            try
            {
                // Primero obtener año y periodo de la asignación
                var queryAsignacion = @"
                    SELECT 
                        G.id_anio,
                        P.id AS id_periodo
                    FROM col.asignacion_academica AS AA
                    INNER JOIN aca.grupo AS G ON AA.id_grupo = G.id
                    INNER JOIN bas.anio AS A ON G.id_anio = A.id
                    INNER JOIN bas.periodo AS P ON A.id = P.id_anio
                    WHERE AA.id = @IdAsignacionAcademica
                      AND CAST(GETDATE() AS DATE) BETWEEN P.fec_inicio AND P.fec_termina";

                var paramsAsignacion = new Dictionary<string, object>
                {
                    { "@IdAsignacionAcademica", idAsignacionAcademica }
                };

                var resultAsignacion = await _databaseService.ExecuteQueryAsync(queryAsignacion, paramsAsignacion);
                
                if (resultAsignacion.Rows.Count == 0)
                    return new EstadisticasActividades();

                var idAnio = Convert.ToInt32(resultAsignacion.Rows[0]["id_anio"]);
                var idPeriodo = Convert.ToInt32(resultAsignacion.Rows[0]["id_periodo"]);

                var parameters = new Dictionary<string, object>
                {
                    { "@IdAnio", idAnio },
                    { "@IdPeriodo", idPeriodo },
                    { "@IdAsignacionAcademica", idAsignacionAcademica }
                };

                // Obtener estadísticas de actividades desde bas.actividad (sistema antiguo)
                // NOTA: bas.actividad no tiene id_asignacion_academica directamente
                // Solo obtenemos estadísticas del sistema nuevo (tab.recurso)
                var queryBas = @"
                    SELECT 
                        0 AS totales,
                        0 AS activas,
                        0 AS inactivas,
                        0 AS pendientes";

                // Obtener estadísticas de actividades desde tab.recurso (sistema nuevo)
                var queryRecursos = @"
                    SELECT 
                        COUNT(*) AS totales,
                        SUM(CASE WHEN AAR.visible = 1 AND CAST(AAR.fecha_calendario AS DATE) >= CAST(GETDATE() AS DATE) THEN 1 ELSE 0 END) AS activas,
                        SUM(CASE WHEN AAR.visible = 0 THEN 1 ELSE 0 END) AS inactivas,
                        SUM(CASE WHEN AAR.visible = 1 AND CAST(AAR.fecha_calendario AS DATE) < CAST(GETDATE() AS DATE) THEN 1 ELSE 0 END) AS pendientes
                    FROM tab.asignacion_academica_recurso AS AAR
                    WHERE AAR.id_asignacion_academica = @IdAsignacionAcademica";

                var resultBas = await _databaseService.ExecuteQueryAsync(queryBas, parameters);

                int totales = 0, activas = 0, inactivas = 0, pendientes = 0;

                // Procesar estadísticas de bas.actividad
                if (resultBas.Rows.Count > 0)
                {
                    var row = resultBas.Rows[0];
                    totales += Convert.ToInt32(row["totales"] ?? 0);
                    activas += Convert.ToInt32(row["activas"] ?? 0);
                    inactivas += Convert.ToInt32(row["inactivas"] ?? 0);
                    pendientes += Convert.ToInt32(row["pendientes"] ?? 0);
                }

                // Procesar estadísticas de tab.recurso - con manejo de errores
                try
                {
                    var resultRecursos = await _databaseService.ExecuteQueryAsync(queryRecursos, parameters);
                    if (resultRecursos.Rows.Count > 0)
                    {
                        var row = resultRecursos.Rows[0];
                        totales += Convert.ToInt32(row["totales"] ?? 0);
                        activas += Convert.ToInt32(row["activas"] ?? 0);
                        inactivas += Convert.ToInt32(row["inactivas"] ?? 0);
                        pendientes += Convert.ToInt32(row["pendientes"] ?? 0);
                    }
                }
                catch (Exception exRecursos)
                {
                    Console.WriteLine($"Error al obtener estadísticas de tab.recurso: {exRecursos.Message}");
                    // Continuar sin las estadísticas de tab.recurso si hay error
                }

                return new EstadisticasActividades
                {
                    Totales = totales,
                    Activas = activas,
                    Inactivas = inactivas,
                    Pendientes = pendientes
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al obtener estadísticas de actividades: {ex.Message}");
                return new EstadisticasActividades();
            }
        }

        /// <summary>
        /// Obtiene el periodo académico actual
        /// </summary>
        public async Task<(int PeriodoId, string Periodo, int AnioId, string Anio)> ObtenerPeriodoActual()
        {
            try
            {
                var query = @"
                    SELECT TOP 1
                        P.id AS periodo_id,
                        P.periodo,
                        A.id AS anio_id,
                        A.anio
                    FROM bas.periodo AS P
                    INNER JOIN bas.anio AS A ON P.id_anio = A.id
                    WHERE CAST(GETDATE() AS DATE) BETWEEN P.fec_inicio AND P.fec_termina
                    ORDER BY P.periodo";

                var result = await _databaseService.ExecuteQueryAsync(query);

                if (result.Rows.Count > 0)
                {
                    var row = result.Rows[0];
                    return (
                        Convert.ToInt32(row["periodo_id"]),
                        row["periodo"]?.ToString() ?? "1",
                        Convert.ToInt32(row["anio_id"]),
                        row["anio"]?.ToString() ?? DateTime.Now.Year.ToString()
                    );
                }

                // Si no hay periodo activo, obtener el último periodo del año actual
                var queryUltimo = @"
                    SELECT TOP 1
                        P.id AS periodo_id,
                        P.periodo,
                        A.id AS anio_id,
                        A.anio
                    FROM bas.periodo AS P
                    INNER JOIN bas.anio AS A ON P.id_anio = A.id
                    WHERE A.anio = YEAR(GETDATE())
                    ORDER BY P.periodo DESC";

                var resultUltimo = await _databaseService.ExecuteQueryAsync(queryUltimo);

                if (resultUltimo.Rows.Count > 0)
                {
                    var row = resultUltimo.Rows[0];
                    return (
                        Convert.ToInt32(row["periodo_id"]),
                        row["periodo"]?.ToString() ?? "1",
                        Convert.ToInt32(row["anio_id"]),
                        row["anio"]?.ToString() ?? DateTime.Now.Year.ToString()
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

        /// <summary>
        /// Obtiene el nombre de la institución desde la base de datos
        /// </summary>
        public async Task<string> ObtenerNombreInstitucion(int colegioId)
        {
            try
            {
                var query = @"
                    SELECT TOP 1 colegio
                    FROM bas.colegio
                    WHERE id = @ColegioId";

                var parameters = new Dictionary<string, object>
                {
                    { "@ColegioId", colegioId }
                };

                var result = await _databaseService.ExecuteScalarAsync(query, parameters);
                return result?.ToString() ?? "INSTITUCIÓN EDUCATIVA";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al obtener nombre de institución: {ex.Message}");
                return "INSTITUCIÓN EDUCATIVA";
            }
        }

        /// <summary>
        /// Obtiene el ID del colegio al que pertenece el docente,
        /// navegando por grupo y grado académicos.
        /// </summary>
        public async Task<int> ObtenerColegioIdPorProfesor(int profesorId)
        {
            try
            {
                var query = @"
                    SELECT TOP 1 
                        A.id_colegio
                    FROM col.asignacion_academica AS AA
                    INNER JOIN aca.grupo  AS GR ON AA.id_grupo  = GR.id
                    INNER JOIN bas.grado  AS G  ON GR.id_grado  = G.id
                    INNER JOIN bas.anio   AS A  ON GR.id_anio   = A.id
                    WHERE AA.id_profesor = @ProfesorId";

                var parameters = new Dictionary<string, object>
                {
                    { "@ProfesorId", profesorId }
                };

                var result = await _databaseService.ExecuteScalarAsync(query, parameters);
                return result != null ? Convert.ToInt32(result) : 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al obtener colegio del profesor: {ex.Message}");
                return 0;
            }
        }

        /// <summary>
        /// Obtiene la primera asignación académica de un grupo para usar al crear actividades
        /// </summary>
        public async Task<int> ObtenerPrimeraAsignacionAcademicaPorGrupo(int grupoId)
        {
            try
            {
                var query = @"
                    SELECT TOP 1 AA.id AS id_asignacion_academica
                    FROM col.asignacion_academica AS AA
                    WHERE AA.id_grupo = @GrupoId
                    ORDER BY AA.id";

                var parameters = new Dictionary<string, object>
                {
                    { "@GrupoId", grupoId }
                };

                var result = await _databaseService.ExecuteQueryAsync(query, parameters);
                
                if (result.Rows.Count > 0)
                {
                    return Convert.ToInt32(result.Rows[0]["id_asignacion_academica"]);
                }
                
                throw new Exception($"No se encontró ninguna asignación académica para el grupo {grupoId}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al obtener asignación académica del grupo: {ex.Message}");
                throw;
            }
        }
    }
}
