using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using ImpulsaDBA.Models;

namespace ImpulsaDBA.Services
{
    public class CalendarioService
    {
        private readonly DatabaseService _databaseService;

        public CalendarioService(DatabaseService databaseService)
        {
            _databaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService));
        }

        /// <summary>
        /// Obtiene las actividades para una fecha y asignatura específica
        /// </summary>
        public async Task<List<ActividadCalendario>> ObtenerActividadesPorFecha(int idAsignacionAcademica, DateTime fecha)
        {
            try
            {
                // Primero obtener el año y periodo de la asignación académica
                var queryAsignacion = @"
                    SELECT 
                        G.id_anio,
                        P.id AS id_periodo
                    FROM col.asignacion_academica AS AA
                    INNER JOIN aca.grupo AS G ON AA.id_grupo = G.id
                    INNER JOIN bas.anio AS A ON G.id_anio = A.id
                    INNER JOIN bas.periodo AS P ON A.id = P.id_anio
                    WHERE AA.id = @IdAsignacionAcademica
                      AND CAST(GETDATE() AS DATE) BETWEEN P.fec_inicio AND P.fec_final";

                var paramsAsignacion = new Dictionary<string, object>
                {
                    { "@IdAsignacionAcademica", idAsignacionAcademica }
                };

                var resultAsignacion = await _databaseService.ExecuteQueryAsync(queryAsignacion, paramsAsignacion);
                
                if (resultAsignacion.Rows.Count == 0)
                    return new List<ActividadCalendario>();

                var idAnio = Convert.ToInt32(resultAsignacion.Rows[0]["id_anio"]);
                var idPeriodo = Convert.ToInt32(resultAsignacion.Rows[0]["id_periodo"]);

                // Obtener actividades que coincidan con el año, periodo y fecha
                var query = @"
                    SELECT 
                        A.id,
                        A.id_t_actividad,
                        TA.t_actividad AS tipo_actividad,
                        A.fec_inicio,
                        A.fec_final,
                        A.id_anio,
                        A.id_periodo,
                        A.eliminado
                    FROM bas.actividad AS A
                    INNER JOIN bas.t_actividad AS TA ON A.id_t_actividad = TA.id
                    WHERE A.id_anio = @IdAnio
                      AND A.id_periodo = @IdPeriodo
                      AND A.eliminado = 0
                      AND (
                          CAST(A.fec_inicio AS DATE) = CAST(@Fecha AS DATE)
                          OR CAST(A.fec_final AS DATE) = CAST(@Fecha AS DATE)
                          OR (CAST(@Fecha AS DATE) BETWEEN CAST(A.fec_inicio AS DATE) AND CAST(A.fec_final AS DATE))
                      )";

                var parameters = new Dictionary<string, object>
                {
                    { "@IdAnio", idAnio },
                    { "@IdPeriodo", idPeriodo },
                    { "@Fecha", fecha }
                };

                var result = await _databaseService.ExecuteQueryAsync(query, parameters);
                var actividades = new List<ActividadCalendario>();

                foreach (DataRow row in result.Rows)
                {
                    actividades.Add(new ActividadCalendario
                    {
                        Id = Convert.ToInt32(row["id"]),
                        IdTipoActividad = Convert.ToInt32(row["id_t_actividad"]),
                        TipoActividad = row["tipo_actividad"]?.ToString() ?? string.Empty,
                        FechaInicio = Convert.ToDateTime(row["fec_inicio"]),
                        FechaFinal = row["fec_final"] != DBNull.Value ? Convert.ToDateTime(row["fec_final"]) : null,
                        IdPeriodo = Convert.ToInt32(row["id_periodo"]),
                        IdAnio = Convert.ToInt32(row["id_anio"]),
                        Eliminado = Convert.ToBoolean(row["eliminado"])
                    });
                }

                return actividades;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al obtener actividades por fecha: {ex.Message}");
                return new List<ActividadCalendario>();
            }
        }

        /// <summary>
        /// Obtiene todas las actividades de un mes para una asignación académica
        /// </summary>
        public async Task<List<ActividadCalendario>> ObtenerActividadesPorMes(int idAsignacionAcademica, int año, int mes)
        {
            try
            {
                var fechaInicio = new DateTime(año, mes, 1);
                var fechaFin = fechaInicio.AddMonths(1).AddDays(-1);

                // Obtener año y periodo de la asignación
                var queryAsignacion = @"
                    SELECT 
                        G.id_anio,
                        P.id AS id_periodo
                    FROM col.asignacion_academica AS AA
                    INNER JOIN aca.grupo AS G ON AA.id_grupo = G.id
                    INNER JOIN bas.anio AS A ON G.id_anio = A.id
                    INNER JOIN bas.periodo AS P ON A.id = P.id_anio
                    WHERE AA.id = @IdAsignacionAcademica
                      AND CAST(GETDATE() AS DATE) BETWEEN P.fec_inicio AND P.fec_final";

                var paramsAsignacion = new Dictionary<string, object>
                {
                    { "@IdAsignacionAcademica", idAsignacionAcademica }
                };

                var resultAsignacion = await _databaseService.ExecuteQueryAsync(queryAsignacion, paramsAsignacion);
                
                if (resultAsignacion.Rows.Count == 0)
                    return new List<ActividadCalendario>();

                var idAnio = Convert.ToInt32(resultAsignacion.Rows[0]["id_anio"]);
                var idPeriodo = Convert.ToInt32(resultAsignacion.Rows[0]["id_periodo"]);

                // Obtener actividades del mes
                var query = @"
                    SELECT 
                        A.id,
                        A.id_t_actividad,
                        TA.t_actividad AS tipo_actividad,
                        A.fec_inicio,
                        A.fec_final,
                        A.id_anio,
                        A.id_periodo,
                        A.eliminado
                    FROM bas.actividad AS A
                    INNER JOIN bas.t_actividad AS TA ON A.id_t_actividad = TA.id
                    WHERE A.id_anio = @IdAnio
                      AND A.id_periodo = @IdPeriodo
                      AND A.eliminado = 0
                      AND (
                          (CAST(A.fec_inicio AS DATE) BETWEEN @FechaInicio AND @FechaFin)
                          OR (CAST(A.fec_final AS DATE) BETWEEN @FechaInicio AND @FechaFin)
                          OR (CAST(A.fec_inicio AS DATE) <= @FechaInicio AND CAST(A.fec_final AS DATE) >= @FechaFin)
                      )";

                var parameters = new Dictionary<string, object>
                {
                    { "@IdAnio", idAnio },
                    { "@IdPeriodo", idPeriodo },
                    { "@FechaInicio", fechaInicio },
                    { "@FechaFin", fechaFin }
                };

                var result = await _databaseService.ExecuteQueryAsync(query, parameters);
                var actividades = new List<ActividadCalendario>();

                foreach (DataRow row in result.Rows)
                {
                    actividades.Add(new ActividadCalendario
                    {
                        Id = Convert.ToInt32(row["id"]),
                        IdTipoActividad = Convert.ToInt32(row["id_t_actividad"]),
                        TipoActividad = row["tipo_actividad"]?.ToString() ?? string.Empty,
                        FechaInicio = Convert.ToDateTime(row["fec_inicio"]),
                        FechaFinal = row["fec_final"] != DBNull.Value ? Convert.ToDateTime(row["fec_final"]) : null,
                        IdPeriodo = Convert.ToInt32(row["id_periodo"]),
                        IdAnio = Convert.ToInt32(row["id_anio"]),
                        Eliminado = Convert.ToBoolean(row["eliminado"])
                    });
                }

                return actividades;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al obtener actividades por mes: {ex.Message}");
                return new List<ActividadCalendario>();
            }
        }

        /// <summary>
        /// Obtiene los días festivos (por ahora retorna lista vacía, se puede extender)
        /// </summary>
        public async Task<List<DateTime>> ObtenerDiasFestivos(int año)
        {
            try
            {
                // Por ahora retornamos lista vacía
                // Si hay tabla de días festivos, se puede implementar aquí
                return new List<DateTime>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al obtener días festivos: {ex.Message}");
                return new List<DateTime>();
            }
        }

        /// <summary>
        /// Obtiene todos los tipos de recursos disponibles desde tab.tipo_recurso
        /// Solo retorna los que tienen en_uso = 1 (true)
        /// </summary>
        public async Task<List<TipoActividad>> ObtenerTiposActividades()
        {
            try
            {
                var query = @"
                    SELECT 
                        id,
                        tipo_recurso AS nombre,
                        abreviatura,
                        orden
                    FROM tab.tipo_recurso
                    WHERE en_uso = 1
                    ORDER BY orden";

                var result = await _databaseService.ExecuteQueryAsync(query);
                var tipos = new List<TipoActividad>();

                // Mapeo de tipos a iconos Bootstrap Icons
                var iconosMap = new Dictionary<string, string>
                {
                    { "Video de Enganche", "bi-play-circle" },
                    { "Preguntas problematizadoras", "bi-question-circle" },
                    { "Lección Interactiva", "bi-book" },
                    { "Tarea", "bi-file-text" },
                    { "Trabajo", "bi-briefcase" },
                    { "Taller", "bi-tools" },
                    { "Investigación", "bi-search" },
                    { "Proyecto", "bi-folder" },
                    { "Actividad Práctica", "bi-clipboard-check" },
                    { "Juego Educativo", "bi-controller" },
                    { "Presentación (Sliders)", "bi-presentation" },
                    { "Documento (Archivo)", "bi-file-earmark" },
                    { "Recurso de Lectura", "bi-journal-text" },
                    { "Clase Virtual", "bi-camera-video" },
                    { "Encuentro", "bi-people" },
                    { "Resumen clase", "bi-file-earmark-text" },
                    { "Evaluación", "bi-clipboard-check" }
                };

                foreach (DataRow row in result.Rows)
                {
                    var nombre = row["nombre"]?.ToString() ?? string.Empty;
                    tipos.Add(new TipoActividad
                    {
                        Id = Convert.ToInt32(row["id"]),
                        Nombre = nombre,
                        Icono = iconosMap.ContainsKey(nombre) ? iconosMap[nombre] : "bi-calendar-event"
                    });
                }

                return tipos;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al obtener tipos de recursos: {ex.Message}");
                return new List<TipoActividad>();
            }
        }
    }
}
