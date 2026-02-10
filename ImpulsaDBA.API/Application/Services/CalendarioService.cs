using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.IO;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using System.Threading.Tasks;
using ImpulsaDBA.API.Domain.Entities;
using ImpulsaDBA.API.Infrastructure.Database;
using ImpulsaDBA.Shared.DTOs;

namespace ImpulsaDBA.API.Application.Services
{
    public class CalendarioService
    {
        private readonly ImpulsaDBA.API.Infrastructure.Database.DatabaseService _databaseService;
        private readonly string _fileStorageRoot;

        public CalendarioService(
            ImpulsaDBA.API.Infrastructure.Database.DatabaseService databaseService,
            IHostEnvironment env,
            IConfiguration configuration)
        {
            _databaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService));

            // Carpeta base donde se guardan los archivos. Se configura en appsettings.json → FileStorage:Root
            // Si es ruta relativa (ej: "files"), se combina con ContentRootPath; si es absoluta, se usa tal cual.
            var rootConfig = configuration["FileStorage:Root"]?.Trim();
            if (!string.IsNullOrEmpty(rootConfig) && Path.IsPathRooted(rootConfig))
                _fileStorageRoot = rootConfig;
            else
                _fileStorageRoot = Path.Combine(env.ContentRootPath, rootConfig ?? "files");
        }

        /// <summary>
        /// Obtiene el código DANE del colegio asociado a una asignación académica.
        /// (id_colegio está en bas.anio, se llega por grupo -> anio -> colegio)
        /// </summary>
        private async Task<string> ObtenerCodigoDanePorAsignacionAsync(int asignacionAcademicaId)
        {
            try
            {
                var query = @"
                    SELECT TOP 1 C.codigo_dane
                    FROM col.asignacion_academica AS AA
                    INNER JOIN aca.grupo AS GR ON AA.id_grupo = GR.id
                    INNER JOIN bas.anio AS A ON GR.id_anio = A.id
                    INNER JOIN bas.colegio AS C ON A.id_colegio = C.id
                    WHERE AA.id = @IdAsignacionAcademica";

                var parameters = new Dictionary<string, object>
                {
                    { "@IdAsignacionAcademica", asignacionAcademicaId }
                };

                var result = await _databaseService.ExecuteScalarAsync(query, parameters);
                return result?.ToString() ?? "SIN_DANE";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al obtener código DANE por asignación: {ex.Message}");
                return "SIN_DANE";
            }
        }

        /// <summary>
        /// Obtiene el código DANE del colegio asociado a un registro de asignacion_academica_recurso.
        /// (id_colegio está en bas.anio, se llega por grupo -> anio -> colegio)
        /// </summary>
        private async Task<string> ObtenerCodigoDanePorRecursoAsync(int asignacionAcademicaRecursoId)
        {
            try
            {
                var query = @"
                    SELECT TOP 1 C.codigo_dane
                    FROM tab.asignacion_academica_recurso AS AAR
                    INNER JOIN col.asignacion_academica AS AA ON AAR.id_asignacion_academica = AA.id
                    INNER JOIN aca.grupo AS GR ON AA.id_grupo = GR.id
                    INNER JOIN bas.anio AS A ON GR.id_anio = A.id
                    INNER JOIN bas.colegio AS C ON A.id_colegio = C.id
                    WHERE AAR.id = @IdAsignacionAcademicaRecurso";

                var parameters = new Dictionary<string, object>
                {
                    { "@IdAsignacionAcademicaRecurso", asignacionAcademicaRecursoId }
                };

                var result = await _databaseService.ExecuteScalarAsync(query, parameters);
                return result?.ToString() ?? "SIN_DANE";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al obtener código DANE por recurso: {ex.Message}");
                return "SIN_DANE";
            }
        }

        /// <summary>
        /// Elimina un archivo: borra el fichero en disco y los registros en tab.archivo_recurso y tab.archivo.
        /// Desde ahora, en tab.archivo.file_name_unico se guarda la ruta completa en disco (fullPath).
        /// Para registros antiguos (solo nombre o ruta relativa), se reconstruye el fullPath a partir del código DANE.
        /// </summary>
        private async Task EliminarArchivoAsync(int idArchivo)
        {
            try
            {
                var paramsArchivo = new Dictionary<string, object> { { "@IdArchivo", idArchivo } };
                // Obtener file_name_unico y id de asignacion_academica_recurso para construir ruta (compatibilidad con registros antiguos)
                var queryInfo = @"
                    SELECT A.file_name_unico, AAR.id AS id_aar
                    FROM tab.archivo A
                    INNER JOIN tab.archivo_recurso AR ON AR.id_archivo = A.id
                    INNER JOIN tab.asignacion_academica_recurso AAR ON AAR.id_recurso = AR.id_recurso
                    WHERE A.id = @IdArchivo";
                var resultInfo = await _databaseService.ExecuteQueryAsync(queryInfo, paramsArchivo);
                if (resultInfo.Rows.Count > 0)
                {
                    var fileNameUnico = resultInfo.Rows[0]["file_name_unico"]?.ToString()?.Trim();
                    var idAar = resultInfo.Rows[0]["id_aar"];
                    if (!string.IsNullOrEmpty(fileNameUnico))
                    {
                        string fullPath;

                        if (Path.IsPathRooted(fileNameUnico))
                        {
                            // Nuevo comportamiento: file_name_unico ya contiene el fullPath
                            fullPath = fileNameUnico.Replace('\\', Path.DirectorySeparatorChar)
                                                    .Replace('/', Path.DirectorySeparatorChar);
                        }
                        else
                        {
                            // Compatibilidad: nombre suelto o ruta relativa bajo _fileStorageRoot
                            string rutaRelativa;
                            if (fileNameUnico.Contains('/') || fileNameUnico.Contains('\\'))
                            {
                                rutaRelativa = fileNameUnico
                                    .Replace('\\', Path.DirectorySeparatorChar)
                                    .Replace('/', Path.DirectorySeparatorChar);
                            }
                            else
                            {
                                if (idAar == null || idAar == DBNull.Value)
                                    return;

                                var codigoDane = await ObtenerCodigoDanePorRecursoAsync(Convert.ToInt32(idAar));
                                rutaRelativa = Path.Combine(codigoDane, fileNameUnico)
                                    .Replace('\\', Path.DirectorySeparatorChar)
                                    .Replace('/', Path.DirectorySeparatorChar);
                            }

                            fullPath = Path.GetFullPath(Path.Combine(_fileStorageRoot, rutaRelativa));
                        }
                        if (File.Exists(fullPath))
                        {
                            File.Delete(fullPath);
                            Console.WriteLine($"✅ Archivo eliminado de disco: {fullPath}");
                        }
                        else
                        {
                            Console.WriteLine($"⚠️ Archivo no encontrado en disco (se eliminará de BD): {fullPath}");
                        }
                    }
                }

                var queryDeleteRecurso = "DELETE FROM tab.archivo_recurso WHERE id_archivo = @IdArchivo";
                await _databaseService.ExecuteNonQueryAsync(queryDeleteRecurso, paramsArchivo);

                var queryDeleteArchivo = "DELETE FROM tab.archivo WHERE id = @IdArchivo";
                await _databaseService.ExecuteNonQueryAsync(queryDeleteArchivo, paramsArchivo);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al eliminar archivo id={idArchivo}: {ex.Message}");
            }
        }

        /// <summary>
        /// Obtiene la ruta física y nombre original de un archivo para descarga.
        /// Desde ahora, en tab.archivo.file_name_unico se guarda la ruta completa en disco (fullPath).
        /// Para registros antiguos (solo nombre o ruta relativa), se reconstruye el fullPath a partir del código DANE.
        /// </summary>
        public async Task<(string? fullPath, string contentType, string fileName)> ObtenerArchivoParaDescargaAsync(int idArchivo)
        {
            var parameters = new Dictionary<string, object> { { "@IdArchivo", idArchivo } };
            // Obtener file_name_original, file_name_unico y id asignacion_academica_recurso para construir ruta (compatibilidad con registros antiguos)
            var query = @"
                SELECT A.file_name_original, A.file_name_unico, AAR.id AS id_aar
                FROM tab.archivo A
                INNER JOIN tab.archivo_recurso AR ON AR.id_archivo = A.id
                INNER JOIN tab.asignacion_academica_recurso AAR ON AAR.id_recurso = AR.id_recurso
                WHERE A.id = @IdArchivo";
            var result = await _databaseService.ExecuteQueryAsync(query, parameters);
            if (result.Rows.Count == 0)
                return (null!, "application/octet-stream", "");

            var nombreOriginal = result.Rows[0]["file_name_original"]?.ToString() ?? "archivo";
            var fileNameUnico = result.Rows[0]["file_name_unico"]?.ToString() ?? "";
            var idAar = result.Rows[0]["id_aar"];
            if (string.IsNullOrEmpty(fileNameUnico))
                return (null!, "application/octet-stream", nombreOriginal);

            string fullPath;
            if (Path.IsPathRooted(fileNameUnico))
            {
                // Nuevo comportamiento: file_name_unico ya contiene el fullPath
                fullPath = fileNameUnico.Replace('\\', Path.DirectorySeparatorChar)
                                        .Replace('/', Path.DirectorySeparatorChar);
            }
            else
            {
                // Compatibilidad con registros antiguos: nombre suelto o ruta relativa
                string rutaRelativa;
                if (fileNameUnico.Contains('/') || fileNameUnico.Contains('\\'))
                {
                    rutaRelativa = fileNameUnico
                        .Replace('\\', Path.DirectorySeparatorChar)
                        .Replace('/', Path.DirectorySeparatorChar);
                }
                else
                {
                    if (idAar == null || idAar == DBNull.Value)
                        return (null!, "application/octet-stream", nombreOriginal);

                    var codigoDane = await ObtenerCodigoDanePorRecursoAsync(Convert.ToInt32(idAar));
                    rutaRelativa = Path.Combine(codigoDane, fileNameUnico)
                        .Replace('\\', Path.DirectorySeparatorChar)
                        .Replace('/', Path.DirectorySeparatorChar);
                }

                fullPath = Path.Combine(_fileStorageRoot, rutaRelativa);
            }
            if (!File.Exists(fullPath))
                return (null!, "application/octet-stream", nombreOriginal);

            var ext = Path.GetExtension(nombreOriginal).ToLowerInvariant();
            var contentType = ext switch
            {
                ".pdf"  => "application/pdf",
                ".doc"  => "application/msword",
                ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                ".xls"  => "application/vnd.ms-excel",
                ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                ".ppt"  => "application/vnd.ms-powerpoint",
                ".pptx" => "application/vnd.openxmlformats-officedocument.presentationml.presentation",
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png"  => "image/png",
                ".gif"  => "image/gif",
                ".bmp"  => "image/bmp",
                ".webp" => "image/webp",
                _ => "application/octet-stream"
            };
            return (fullPath, contentType, nombreOriginal);
        }

        /// <summary>
        /// Obtiene las actividades para una fecha y asignatura específica
        /// </summary>
        public async Task<List<ActividadCalendarioDto>> ObtenerActividadesPorFecha(int idAsignacionAcademica, DateTime fecha)
        {
            try
            {
                // Primero obtener el año y periodo de la asignación académica
                // Buscar el periodo que contiene la fecha de la actividad, o el periodo activo actual
                var queryAsignacion = @"
                    SELECT TOP 1
                        G.id_anio,
                        P.id AS id_periodo
                    FROM col.asignacion_academica AS AA
                    INNER JOIN aca.grupo AS G ON AA.id_grupo = G.id
                    INNER JOIN bas.anio AS A ON G.id_anio = A.id
                    INNER JOIN bas.periodo AS P ON A.id = P.id_anio
                    WHERE AA.id = @IdAsignacionAcademica
                      AND (
                          CAST(@Fecha AS DATE) BETWEEN P.fec_inicio AND P.fec_termina
                          OR CAST(GETDATE() AS DATE) BETWEEN P.fec_inicio AND P.fec_termina
                      )
                    ORDER BY 
                        CASE WHEN CAST(@Fecha AS DATE) BETWEEN P.fec_inicio AND P.fec_termina THEN 0 ELSE 1 END,
                        P.fec_inicio DESC";

                var paramsAsignacion = new Dictionary<string, object>
                {
                    { "@IdAsignacionAcademica", idAsignacionAcademica },
                    { "@Fecha", fecha }
                };

                var resultAsignacion = await _databaseService.ExecuteQueryAsync(queryAsignacion, paramsAsignacion);
                
                // Si no hay periodo que contenga la fecha, obtener solo el año del grupo
                int idAnio = 0;
                int idPeriodo = 0;
                
                if (resultAsignacion.Rows.Count > 0)
                {
                    idAnio = Convert.ToInt32(resultAsignacion.Rows[0]["id_anio"]);
                    idPeriodo = Convert.ToInt32(resultAsignacion.Rows[0]["id_periodo"]);
                }
                else
                {
                    // Si no hay periodo, obtener solo el año del grupo
                    var queryAnio = @"
                        SELECT G.id_anio
                        FROM col.asignacion_academica AS AA
                        INNER JOIN aca.grupo AS G ON AA.id_grupo = G.id
                        WHERE AA.id = @IdAsignacionAcademica";
                    
                    var resultAnio = await _databaseService.ExecuteQueryAsync(queryAnio, paramsAsignacion);
                    if (resultAnio.Rows.Count > 0)
                    {
                        idAnio = Convert.ToInt32(resultAnio.Rows[0]["id_anio"]);
                    }
                }

                var parameters = new Dictionary<string, object>
                {
                    { "@IdAsignacionAcademica", idAsignacionAcademica },
                    { "@Fecha", fecha }
                };

                // NOTA: bas.actividad (sistema antiguo) no tiene id_asignacion_academica
                // Solo usamos el sistema nuevo (tab.recurso) que sí tiene esa relación
                var queryBasActividad = "";

                // Obtener actividades desde tab.recurso y tab.asignacion_academica_recurso (sistema nuevo)
                // No necesitamos año y periodo para estas actividades, están directamente asociadas a la asignación
                var queryRecursos = @"
                    SELECT 
                        AAR.id AS id,
                        R.id_tipo_recurso AS id_t_actividad,
                        ISNULL(TR.tipo_recurso, 'Sin tipo') AS tipo_actividad,
                        R.titulo AS titulo,
                        AAR.fecha_calendario AS fec_inicio,
                        CAST(NULL AS DATETIME) AS fec_final,
                        AAR.fecha_creacion_registro AS fec_creacion,
                        @IdAnio AS id_anio,
                        @IdPeriodo AS id_periodo,
                        CASE WHEN AAR.visible = 0 THEN 1 ELSE 0 END AS eliminado,
                        AAR.id_asignacion_academica AS id_asignacion_academica,
                        P.id AS id_profesor,
                        P.primer_nombre + ' ' + P.primer_apellido AS nombre_profesor,
                        ASIG.asignatura AS nombre_asignatura
                    FROM tab.asignacion_academica_recurso AS AAR
                    INNER JOIN tab.recurso AS R ON AAR.id_recurso = R.id
                    LEFT JOIN tab.tipo_recurso AS TR ON R.id_tipo_recurso = TR.id
                    INNER JOIN col.asignacion_academica AS AA ON AAR.id_asignacion_academica = AA.id
                    LEFT JOIN col.persona AS P ON AA.id_profesor = P.id
                    LEFT JOIN col.asignatura AS ASIG ON AA.id_asignatura = ASIG.id
                    WHERE AAR.id_asignacion_academica = @IdAsignacionAcademica
                      AND AAR.fecha_calendario IS NOT NULL
                      AND CAST(AAR.fecha_calendario AS DATE) = CAST(@Fecha AS DATE)";

                // Ejecutar ambas consultas y combinar resultados
                var actividades = new List<ActividadCalendarioDto>();

                // Procesar actividades de bas.actividad (solo si tenemos año y periodo)
                if (!string.IsNullOrEmpty(queryBasActividad))
                {
                    var resultBas = await _databaseService.ExecuteQueryAsync(queryBasActividad, parameters);
                    foreach (DataRow row in resultBas.Rows)
                    {
                        actividades.Add(new ActividadCalendarioDto
                        {
                            Id = Convert.ToInt32(row["id"]),
                            IdTipoActividad = Convert.ToInt32(row["id_t_actividad"]),
                            TipoActividad = row["tipo_actividad"]?.ToString() ?? string.Empty,
                            FechaInicio = Convert.ToDateTime(row["fec_inicio"]),
                            FechaFinal = row["fec_final"] != DBNull.Value ? Convert.ToDateTime(row["fec_final"]) : null,
                            FechaCreacion = row["fec_creacion"] != DBNull.Value ? Convert.ToDateTime(row["fec_creacion"]) : null,
                            IdAsignacionAcademica = row["id_asignacion_academica"] != DBNull.Value ? Convert.ToInt32(row["id_asignacion_academica"]) : idAsignacionAcademica,
                            IdPeriodo = Convert.ToInt32(row["id_periodo"]),
                            IdAnio = Convert.ToInt32(row["id_anio"]),
                            Eliminado = Convert.ToBoolean(row["eliminado"]),
                            IdProfesor = row["id_profesor"] != DBNull.Value ? Convert.ToInt32(row["id_profesor"]) : null,
                            NombreProfesor = row["nombre_profesor"]?.ToString(),
                            NombreAsignatura = row["nombre_asignatura"]?.ToString()
                        });
                    }
                }

                // Procesar actividades de tab.recurso (nuevas actividades) - con manejo de errores
                try
                {
                    // Agregar año y periodo a los parámetros si no están
                    if (!parameters.ContainsKey("@IdAnio"))
                    {
                        parameters.Add("@IdAnio", idAnio);
                    }
                    if (!parameters.ContainsKey("@IdPeriodo"))
                    {
                        parameters.Add("@IdPeriodo", idPeriodo);
                    }
                    
                    var resultRecursos = await _databaseService.ExecuteQueryAsync(queryRecursos, parameters);
                    Console.WriteLine($"📊 Actividades encontradas en tab.recurso: {resultRecursos.Rows.Count}");
                    
                    foreach (DataRow row in resultRecursos.Rows)
                    {
                        var idTipoActividad = Convert.ToInt32(row["id_t_actividad"]);
                        var tipoActividad = row["tipo_actividad"]?.ToString() ?? "Sin tipo";
                        Console.WriteLine($"  - Actividad ID: {row["id"]}, Tipo: {idTipoActividad} ({tipoActividad}), Título: {row["titulo"]?.ToString()}");
                        
                        actividades.Add(new ActividadCalendarioDto
                        {
                            Id = Convert.ToInt32(row["id"]),
                            IdTipoActividad = idTipoActividad,
                            TipoActividad = tipoActividad,
                            Titulo = row["titulo"] != DBNull.Value ? row["titulo"]?.ToString() : null,
                            FechaInicio = Convert.ToDateTime(row["fec_inicio"]),
                            FechaFinal = row["fec_final"] != DBNull.Value ? Convert.ToDateTime(row["fec_final"]) : null,
                            FechaCreacion = row["fec_creacion"] != DBNull.Value ? Convert.ToDateTime(row["fec_creacion"]) : null,
                            IdAsignacionAcademica = row["id_asignacion_academica"] != DBNull.Value ? Convert.ToInt32(row["id_asignacion_academica"]) : idAsignacionAcademica,
                            IdPeriodo = Convert.ToInt32(row["id_periodo"]),
                            IdAnio = Convert.ToInt32(row["id_anio"]),
                            Eliminado = Convert.ToBoolean(row["eliminado"]),
                            IdProfesor = row["id_profesor"] != DBNull.Value ? Convert.ToInt32(row["id_profesor"]) : null,
                            NombreProfesor = row["nombre_profesor"]?.ToString(),
                            NombreAsignatura = row["nombre_asignatura"]?.ToString()
                        });
                    }
                }
                catch (Exception exRecursos)
                {
                    Console.WriteLine($"❌ Error al obtener actividades de tab.recurso: {exRecursos.Message}");
                    Console.WriteLine($"Stack trace: {exRecursos.StackTrace}");
                    // Continuar sin las actividades de tab.recurso si hay error
                }

                return actividades;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al obtener actividades por fecha: {ex.Message}");
                return new List<ActividadCalendarioDto>();
            }
        }

        /// <summary>
        /// Obtiene todas las actividades de un mes para una asignación académica
        /// </summary>
        public async Task<List<ActividadCalendarioDto>> ObtenerActividadesPorMes(int idAsignacionAcademica, int año, int mes)
        {
            try
            {
                var fechaInicio = new DateTime(año, mes, 1);
                var fechaFin = fechaInicio.AddMonths(1).AddDays(-1);

                // Obtener año y periodo de la asignación
                // Buscar el periodo que contiene alguna fecha del mes, o el periodo activo actual
                var queryAsignacion = @"
                    SELECT TOP 1
                        G.id_anio,
                        P.id AS id_periodo
                    FROM col.asignacion_academica AS AA
                    INNER JOIN aca.grupo AS G ON AA.id_grupo = G.id
                    INNER JOIN bas.anio AS A ON G.id_anio = A.id
                    INNER JOIN bas.periodo AS P ON A.id = P.id_anio
                    WHERE AA.id = @IdAsignacionAcademica
                      AND (
                          (@FechaInicio BETWEEN P.fec_inicio AND P.fec_termina)
                          OR (@FechaFin BETWEEN P.fec_inicio AND P.fec_termina)
                          OR (P.fec_inicio BETWEEN @FechaInicio AND @FechaFin)
                          OR CAST(GETDATE() AS DATE) BETWEEN P.fec_inicio AND P.fec_termina
                      )
                    ORDER BY 
                        CASE WHEN CAST(GETDATE() AS DATE) BETWEEN P.fec_inicio AND P.fec_termina THEN 0 ELSE 1 END,
                        P.fec_inicio DESC";

                var paramsAsignacion = new Dictionary<string, object>
                {
                    { "@IdAsignacionAcademica", idAsignacionAcademica },
                    { "@FechaInicio", fechaInicio },
                    { "@FechaFin", fechaFin }
                };

                var resultAsignacion = await _databaseService.ExecuteQueryAsync(queryAsignacion, paramsAsignacion);
                
                // Si no hay periodo, obtener solo el año del grupo
                int idAnio = 0;
                int idPeriodo = 0;
                
                if (resultAsignacion.Rows.Count > 0)
                {
                    idAnio = Convert.ToInt32(resultAsignacion.Rows[0]["id_anio"]);
                    idPeriodo = Convert.ToInt32(resultAsignacion.Rows[0]["id_periodo"]);
                }
                else
                {
                    // Si no hay periodo, obtener solo el año del grupo
                    var queryAnio = @"
                        SELECT G.id_anio
                        FROM col.asignacion_academica AS AA
                        INNER JOIN aca.grupo AS G ON AA.id_grupo = G.id
                        WHERE AA.id = @IdAsignacionAcademica";
                    
                    var resultAnio = await _databaseService.ExecuteQueryAsync(queryAnio, new Dictionary<string, object> { { "@IdAsignacionAcademica", idAsignacionAcademica } });
                    if (resultAnio.Rows.Count > 0)
                    {
                        idAnio = Convert.ToInt32(resultAnio.Rows[0]["id_anio"]);
                    }
                }

                var parameters = new Dictionary<string, object>
                {
                    { "@IdAsignacionAcademica", idAsignacionAcademica },
                    { "@FechaInicio", fechaInicio },
                    { "@FechaFin", fechaFin }
                };

                // NOTA: bas.actividad (sistema antiguo) no tiene id_asignacion_academica
                // Solo usamos el sistema nuevo (tab.recurso) que sí tiene esa relación
                var queryBasActividad = "";

                // Obtener actividades desde tab.recurso (sistema nuevo)
                // No necesitamos año y periodo para estas actividades, están directamente asociadas a la asignación
                var queryRecursos = @"
                    SELECT 
                        AAR.id AS id,
                        R.id_tipo_recurso AS id_t_actividad,
                        ISNULL(TR.tipo_recurso, 'Sin tipo') AS tipo_actividad,
                        R.titulo AS titulo,
                        AAR.fecha_calendario AS fec_inicio,
                        CAST(NULL AS DATETIME) AS fec_final,
                        AAR.fecha_creacion_registro AS fec_creacion,
                        @IdAnio AS id_anio,
                        @IdPeriodo AS id_periodo,
                        CASE WHEN AAR.visible = 0 THEN 1 ELSE 0 END AS eliminado,
                        AAR.id_asignacion_academica AS id_asignacion_academica,
                        P.id AS id_profesor,
                        P.primer_nombre + ' ' + P.primer_apellido AS nombre_profesor,
                        ASIG.asignatura AS nombre_asignatura
                    FROM tab.asignacion_academica_recurso AS AAR
                    INNER JOIN tab.recurso AS R ON AAR.id_recurso = R.id
                    LEFT JOIN tab.tipo_recurso AS TR ON R.id_tipo_recurso = TR.id
                    INNER JOIN col.asignacion_academica AS AA ON AAR.id_asignacion_academica = AA.id
                    LEFT JOIN col.persona AS P ON AA.id_profesor = P.id
                    LEFT JOIN col.asignatura AS ASIG ON AA.id_asignatura = ASIG.id
                    WHERE AAR.id_asignacion_academica = @IdAsignacionAcademica
                      AND AAR.fecha_calendario IS NOT NULL
                      AND CAST(AAR.fecha_calendario AS DATE) BETWEEN @FechaInicio AND @FechaFin";

                // Ejecutar ambas consultas y combinar resultados
                var actividades = new List<ActividadCalendarioDto>();

                // Procesar actividades de bas.actividad (solo si tenemos año y periodo)
                if (!string.IsNullOrEmpty(queryBasActividad))
                {
                    var resultBas = await _databaseService.ExecuteQueryAsync(queryBasActividad, parameters);
                    foreach (DataRow row in resultBas.Rows)
                    {
                        actividades.Add(new ActividadCalendarioDto
                        {
                            Id = Convert.ToInt32(row["id"]),
                            IdTipoActividad = Convert.ToInt32(row["id_t_actividad"]),
                            TipoActividad = row["tipo_actividad"]?.ToString() ?? string.Empty,
                            FechaInicio = Convert.ToDateTime(row["fec_inicio"]),
                            FechaFinal = row["fec_final"] != DBNull.Value ? Convert.ToDateTime(row["fec_final"]) : null,
                            FechaCreacion = row["fec_creacion"] != DBNull.Value ? Convert.ToDateTime(row["fec_creacion"]) : null,
                            IdAsignacionAcademica = row["id_asignacion_academica"] != DBNull.Value ? Convert.ToInt32(row["id_asignacion_academica"]) : idAsignacionAcademica,
                            IdPeriodo = Convert.ToInt32(row["id_periodo"]),
                            IdAnio = Convert.ToInt32(row["id_anio"]),
                            Eliminado = Convert.ToBoolean(row["eliminado"]),
                            IdProfesor = row["id_profesor"] != DBNull.Value ? Convert.ToInt32(row["id_profesor"]) : null,
                            NombreProfesor = row["nombre_profesor"]?.ToString(),
                            NombreAsignatura = row["nombre_asignatura"]?.ToString()
                        });
                    }
                }

                // Procesar actividades de tab.recurso (nuevas actividades) - con manejo de errores
                try
                {
                    // Agregar año y periodo a los parámetros si no están
                    if (!parameters.ContainsKey("@IdAnio"))
                    {
                        parameters.Add("@IdAnio", idAnio);
                    }
                    if (!parameters.ContainsKey("@IdPeriodo"))
                    {
                        parameters.Add("@IdPeriodo", idPeriodo);
                    }
                    
                    var resultRecursos = await _databaseService.ExecuteQueryAsync(queryRecursos, parameters);
                    Console.WriteLine($"📊 Actividades encontradas en tab.recurso: {resultRecursos.Rows.Count}");
                    
                    foreach (DataRow row in resultRecursos.Rows)
                    {
                        var idTipoActividad = Convert.ToInt32(row["id_t_actividad"]);
                        var tipoActividad = row["tipo_actividad"]?.ToString() ?? "Sin tipo";
                        Console.WriteLine($"  - Actividad ID: {row["id"]}, Tipo: {idTipoActividad} ({tipoActividad}), Título: {row["titulo"]?.ToString()}");
                        
                        actividades.Add(new ActividadCalendarioDto
                        {
                            Id = Convert.ToInt32(row["id"]),
                            IdTipoActividad = idTipoActividad,
                            TipoActividad = tipoActividad,
                            Titulo = row["titulo"] != DBNull.Value ? row["titulo"]?.ToString() : null,
                            FechaInicio = Convert.ToDateTime(row["fec_inicio"]),
                            FechaFinal = row["fec_final"] != DBNull.Value ? Convert.ToDateTime(row["fec_final"]) : null,
                            FechaCreacion = row["fec_creacion"] != DBNull.Value ? Convert.ToDateTime(row["fec_creacion"]) : null,
                            IdAsignacionAcademica = row["id_asignacion_academica"] != DBNull.Value ? Convert.ToInt32(row["id_asignacion_academica"]) : idAsignacionAcademica,
                            IdPeriodo = Convert.ToInt32(row["id_periodo"]),
                            IdAnio = Convert.ToInt32(row["id_anio"]),
                            Eliminado = Convert.ToBoolean(row["eliminado"]),
                            IdProfesor = row["id_profesor"] != DBNull.Value ? Convert.ToInt32(row["id_profesor"]) : null,
                            NombreProfesor = row["nombre_profesor"]?.ToString(),
                            NombreAsignatura = row["nombre_asignatura"]?.ToString()
                        });
                    }
                }
                catch (Exception exRecursos)
                {
                    Console.WriteLine($"❌ Error al obtener actividades de tab.recurso: {exRecursos.Message}");
                    Console.WriteLine($"Stack trace: {exRecursos.StackTrace}");
                    // Continuar sin las actividades de tab.recurso si hay error
                }

                return actividades;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al obtener actividades por mes: {ex.Message}");
                return new List<ActividadCalendarioDto>();
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
        public async Task<List<TipoActividadDto>> ObtenerTiposActividades()
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
                var tipos = new List<TipoActividadDto>();

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
                    tipos.Add(new TipoActividadDto
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
                return new List<TipoActividadDto>();
            }
        }

        /// <summary>
        /// Obtiene todas las actividades de un grupo (de todos los docentes del grupo) para una fecha específica
        /// </summary>
        public async Task<List<ActividadCalendarioDto>> ObtenerActividadesPorGrupoYFecha(int grupoId, DateTime fecha)
        {
            try
            {
                // Obtener año y periodo del grupo
                // Buscar el periodo que contiene la fecha, o el periodo activo actual
                var queryGrupo = @"
                    SELECT TOP 1
                        G.id_anio,
                        P.id AS id_periodo
                    FROM aca.grupo AS G
                    INNER JOIN bas.anio AS A ON G.id_anio = A.id
                    INNER JOIN bas.periodo AS P ON A.id = P.id_anio
                    WHERE G.id = @GrupoId
                      AND (
                          CAST(@Fecha AS DATE) BETWEEN P.fec_inicio AND P.fec_termina
                          OR CAST(GETDATE() AS DATE) BETWEEN P.fec_inicio AND P.fec_termina
                      )
                    ORDER BY 
                        CASE WHEN CAST(@Fecha AS DATE) BETWEEN P.fec_inicio AND P.fec_termina THEN 0 ELSE 1 END,
                        P.fec_inicio DESC";

                var paramsGrupo = new Dictionary<string, object>
                {
                    { "@GrupoId", grupoId },
                    { "@Fecha", fecha }
                };

                var resultGrupo = await _databaseService.ExecuteQueryAsync(queryGrupo, paramsGrupo);
                
                // Si no hay periodo, obtener solo el año del grupo
                int idAnio = 0;
                int idPeriodo = 0;
                
                if (resultGrupo.Rows.Count > 0)
                {
                    idAnio = Convert.ToInt32(resultGrupo.Rows[0]["id_anio"]);
                    idPeriodo = Convert.ToInt32(resultGrupo.Rows[0]["id_periodo"]);
                }
                else
                {
                    // Si no hay periodo, obtener solo el año del grupo
                    var queryAnio = @"
                        SELECT G.id_anio
                        FROM aca.grupo AS G
                        WHERE G.id = @GrupoId";
                    
                    var resultAnio = await _databaseService.ExecuteQueryAsync(queryAnio, new Dictionary<string, object> { { "@GrupoId", grupoId } });
                    if (resultAnio.Rows.Count > 0)
                    {
                        idAnio = Convert.ToInt32(resultAnio.Rows[0]["id_anio"]);
                    }
                }

                var parameters = new Dictionary<string, object>
                {
                    { "@GrupoId", grupoId },
                    { "@Fecha", fecha }
                };

                // NOTA: bas.actividad (sistema antiguo) no tiene id_asignacion_academica
                // Solo usamos el sistema nuevo (tab.recurso) que sí tiene esa relación
                var queryBasActividad = "";

                // Obtener actividades desde tab.recurso (sistema nuevo)
                // No necesitamos año y periodo para estas actividades, están directamente asociadas a la asignación
                var queryRecursos = @"
                    SELECT 
                        AAR.id AS id,
                        R.id_tipo_recurso AS id_t_actividad,
                        ISNULL(TR.tipo_recurso, 'Sin tipo') AS tipo_actividad,
                        R.titulo AS titulo,
                        AAR.fecha_calendario AS fec_inicio,
                        CAST(NULL AS DATETIME) AS fec_final,
                        AAR.fecha_creacion_registro AS fec_creacion,
                        @IdAnio AS id_anio,
                        @IdPeriodo AS id_periodo,
                        CASE WHEN AAR.visible = 0 THEN 1 ELSE 0 END AS eliminado,
                        AAR.id_asignacion_academica AS id_asignacion_academica,
                        P.id AS id_profesor,
                        P.primer_nombre + ' ' + P.primer_apellido AS nombre_profesor,
                        ASIG.asignatura AS nombre_asignatura
                    FROM tab.asignacion_academica_recurso AS AAR
                    INNER JOIN tab.recurso AS R ON AAR.id_recurso = R.id
                    LEFT JOIN tab.tipo_recurso AS TR ON R.id_tipo_recurso = TR.id
                    INNER JOIN col.asignacion_academica AS AA ON AAR.id_asignacion_academica = AA.id
                    INNER JOIN aca.grupo AS G ON AA.id_grupo = G.id
                    INNER JOIN col.persona AS P ON AA.id_profesor = P.id
                    INNER JOIN col.asignatura AS ASIG ON AA.id_asignatura = ASIG.id
                    WHERE G.id = @GrupoId
                      AND AAR.fecha_calendario IS NOT NULL
                      AND CAST(AAR.fecha_calendario AS DATE) = CAST(@Fecha AS DATE)
                    ORDER BY AAR.fecha_calendario, ASIG.asignatura";

                // Ejecutar ambas consultas y combinar resultados
                var actividades = new List<ActividadCalendarioDto>();

                // Procesar actividades de bas.actividad (solo si tenemos año y periodo)
                if (!string.IsNullOrEmpty(queryBasActividad))
                {
                    var resultBas = await _databaseService.ExecuteQueryAsync(queryBasActividad, parameters);
                    foreach (DataRow row in resultBas.Rows)
                    {
                        actividades.Add(new ActividadCalendarioDto
                        {
                            Id = Convert.ToInt32(row["id"]),
                            IdTipoActividad = Convert.ToInt32(row["id_t_actividad"]),
                            TipoActividad = row["tipo_actividad"]?.ToString() ?? string.Empty,
                            FechaInicio = Convert.ToDateTime(row["fec_inicio"]),
                            FechaFinal = row["fec_final"] != DBNull.Value ? Convert.ToDateTime(row["fec_final"]) : null,
                            FechaCreacion = row["fec_creacion"] != DBNull.Value ? Convert.ToDateTime(row["fec_creacion"]) : null,
                            IdAsignacionAcademica = row["id_asignacion_academica"] != DBNull.Value ? Convert.ToInt32(row["id_asignacion_academica"]) : 0,
                            IdPeriodo = Convert.ToInt32(row["id_periodo"]),
                            IdAnio = Convert.ToInt32(row["id_anio"]),
                            Eliminado = Convert.ToBoolean(row["eliminado"]),
                            IdProfesor = row["id_profesor"] != DBNull.Value ? Convert.ToInt32(row["id_profesor"]) : null,
                            NombreProfesor = row["nombre_profesor"]?.ToString(),
                            NombreAsignatura = row["nombre_asignatura"]?.ToString()
                        });
                    }
                }

                // Procesar actividades de tab.recurso (nuevas actividades) - con manejo de errores
                try
                {
                    // Agregar año y periodo a los parámetros si no están
                    if (!parameters.ContainsKey("@IdAnio"))
                    {
                        parameters.Add("@IdAnio", idAnio);
                    }
                    if (!parameters.ContainsKey("@IdPeriodo"))
                    {
                        parameters.Add("@IdPeriodo", idPeriodo);
                    }
                    
                    var resultRecursos = await _databaseService.ExecuteQueryAsync(queryRecursos, parameters);
                    foreach (DataRow row in resultRecursos.Rows)
                    {
                        actividades.Add(new ActividadCalendarioDto
                        {
                            Id = Convert.ToInt32(row["id"]),
                            IdTipoActividad = Convert.ToInt32(row["id_t_actividad"]),
                            TipoActividad = row["tipo_actividad"]?.ToString() ?? string.Empty,
                            Titulo = row["titulo"] != DBNull.Value ? row["titulo"]?.ToString() : null,
                            FechaInicio = Convert.ToDateTime(row["fec_inicio"]),
                            FechaFinal = row["fec_final"] != DBNull.Value ? Convert.ToDateTime(row["fec_final"]) : null,
                            FechaCreacion = row["fec_creacion"] != DBNull.Value ? Convert.ToDateTime(row["fec_creacion"]) : null,
                            IdAsignacionAcademica = row["id_asignacion_academica"] != DBNull.Value ? Convert.ToInt32(row["id_asignacion_academica"]) : 0,
                            IdPeriodo = Convert.ToInt32(row["id_periodo"]),
                            IdAnio = Convert.ToInt32(row["id_anio"]),
                            Eliminado = Convert.ToBoolean(row["eliminado"]),
                            IdProfesor = row["id_profesor"] != DBNull.Value ? Convert.ToInt32(row["id_profesor"]) : null,
                            NombreProfesor = row["nombre_profesor"]?.ToString(),
                            NombreAsignatura = row["nombre_asignatura"]?.ToString()
                        });
                    }
                }
                catch (Exception exRecursos)
                {
                    Console.WriteLine($"Error al obtener actividades de tab.recurso: {exRecursos.Message}");
                    // Continuar sin las actividades de tab.recurso si hay error
                }

                return actividades;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al obtener actividades por grupo y fecha: {ex.Message}");
                return new List<ActividadCalendarioDto>();
            }
        }

        /// <summary>
        /// Crea una nueva actividad en la base de datos según el tipo especificado
        /// </summary>
        public async Task<int> CrearActividad(ImpulsaDBA.Shared.Requests.CrearActividadCompletaRequest request)
        {
            try
            {
                // Validar que el id_asignacion_academica existe
                if (request.AsignacionAcademicaId <= 0)
                {
                    throw new Exception($"El ID de asignación académica es inválido: {request.AsignacionAcademicaId}");
                }

                var queryValidarAsignacion = @"
                    SELECT COUNT(*) AS existe
                    FROM col.asignacion_academica
                    WHERE id = @IdAsignacionAcademica";

                var paramsValidarAsignacion = new Dictionary<string, object>
                {
                    { "@IdAsignacionAcademica", request.AsignacionAcademicaId }
                };

                var resultValidar = await _databaseService.ExecuteScalarAsync(queryValidarAsignacion, paramsValidarAsignacion);
                int existe = Convert.ToInt32(resultValidar);

                if (existe == 0)
                {
                    throw new Exception($"La asignación académica con ID {request.AsignacionAcademicaId} no existe en la base de datos");
                }

                Console.WriteLine($"✅ Asignación académica {request.AsignacionAcademicaId} validada correctamente");

                // Obtener año y periodo si no vienen en el request
                int idAnio = request.AnioId;
                int idPeriodo = request.PeriodoId;
                
                if (idAnio == 0 || idPeriodo == 0)
                {
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
                        { "@IdAsignacionAcademica", request.AsignacionAcademicaId }
                    };

                    var resultAsignacion = await _databaseService.ExecuteQueryAsync(queryAsignacion, paramsAsignacion);
                    
                    if (resultAsignacion.Rows.Count == 0)
                        throw new Exception("No se pudo obtener el año y periodo de la asignación académica");

                    idAnio = Convert.ToInt32(resultAsignacion.Rows[0]["id_anio"]);
                    idPeriodo = Convert.ToInt32(resultAsignacion.Rows[0]["id_periodo"]);
                }

                // 1. Insertar en tab.recurso
                Console.WriteLine($"📝 Creando actividad - Tipo: {request.TipoActividadId}, Título: {request.Titulo}, Fecha: {request.FechaPublicacion}");
                
                var queryRecurso = @"
                    INSERT INTO tab.recurso (titulo, descripcion, id_tipo_recurso, debe_entregar_archivo_estudiante)
                    OUTPUT INSERTED.id
                    VALUES (@Titulo, @Descripcion, @IdTipoRecurso, @DebeEntregarArchivoEstudiante)";

                var paramsRecurso = new Dictionary<string, object>
                {
                    { "@Titulo", request.Titulo },
                    { "@Descripcion", request.Descripcion ?? string.Empty },
                    { "@IdTipoRecurso", request.TipoActividadId },
                    { "@DebeEntregarArchivoEstudiante", request.GeneraEntregable }
                };

                var idRecurso = await _databaseService.ExecuteScalarAsync(queryRecurso, paramsRecurso);
                if (idRecurso == null)
                    throw new Exception("Error al crear el recurso");

                int idRecursoInt = Convert.ToInt32(idRecurso);
                Console.WriteLine($"✅ Recurso creado con ID: {idRecursoInt}");

                // 2. Insertar en tab.asignacion_academica_recurso
                var queryAsignacionRecurso = @"
                    INSERT INTO tab.asignacion_academica_recurso 
                        (presencial, visible, fecha_calendario, fecha_creacion_registro, id_asignacion_academica, id_recurso)
                    VALUES (@Presencial, @Visible, @FechaCalendario, @FechaCreacion, @IdAsignacionAcademica, @IdRecurso)";

                var paramsAsignacionRecurso = new Dictionary<string, object>
                {
                    { "@Presencial", false }, // Por defecto no presencial
                    { "@Visible", request.ActividadActiva },
                    { "@FechaCalendario", request.FechaPublicacion ?? DateTime.Now },
                    { "@FechaCreacion", DateTime.Now },
                    { "@IdAsignacionAcademica", request.AsignacionAcademicaId },
                    { "@IdRecurso", idRecursoInt }
                };

                await _databaseService.ExecuteNonQueryAsync(queryAsignacionRecurso, paramsAsignacionRecurso);
                Console.WriteLine($"✅ Actividad asociada a asignación académica {request.AsignacionAcademicaId}, Visible: {request.ActividadActiva}, Fecha: {request.FechaPublicacion}");

                // 3. Manejar hipertexto según el tipo de actividad
                string? hipertextoFinal = null;

                // Si tiene hipertexto directo (Asignaciones con editor Quill)
                if (!string.IsNullOrEmpty(request.Hipertexto))
                {
                    hipertextoFinal = request.Hipertexto;
                }
                // Si tiene videos (Video de Enganche)
                else if (request.Videos != null && request.Videos.Any())
                {
                    // Para videos, concatenamos los URLs
                    var urlsVideos = request.Videos
                        .Where(v => !string.IsNullOrEmpty(v.Url))
                        .OrderBy(v => v.Orden)
                        .Select(v => v.Url);
                    hipertextoFinal = string.Join("\n", urlsVideos);
                }
                // Si tiene preguntas (Preguntas Problematizadoras)
                else if (request.Preguntas != null && request.Preguntas.Any())
                {
                    // Las preguntas se almacenan en el hipertexto como una lista
                    var preguntasTexto = string.Join("\n", request.Preguntas
                        .Where(p => !string.IsNullOrEmpty(p.Enunciado))
                        .OrderBy(p => p.Orden)
                        .Select((p, index) => $"{index + 1}. {p.Enunciado}"));
                    hipertextoFinal = preguntasTexto;
                }

                // Insertar hipertexto si existe
                if (!string.IsNullOrEmpty(hipertextoFinal))
                {
                    var queryHipertexto = @"
                        INSERT INTO tab.hipertexto_recurso (hipertexto, id_recurso)
                        VALUES (@Hipertexto, @IdRecurso)";

                    var paramsHipertexto = new Dictionary<string, object>
                    {
                        { "@Hipertexto", hipertextoFinal },
                        { "@IdRecurso", idRecursoInt }
                    };

                    await _databaseService.ExecuteNonQueryAsync(queryHipertexto, paramsHipertexto);
                }

                // 6. Si tiene archivos (MaterialApoyo, Asignaciones)
                if (request.Archivos != null && request.Archivos.Any())
                {
                    // Carpeta por colegio usando código DANE
                    var codigoDane = await ObtenerCodigoDanePorAsignacionAsync(request.AsignacionAcademicaId);

                    foreach (var archivo in request.Archivos)
                    {
                        try
                        {
                            // 6.1 Guardar físicamente el archivo en files/<codigoDane>/<NombreOriginal>
                            if (archivo.Datos != null && archivo.Datos.Length > 0)
                            {
                                var relativePath = Path.Combine(codigoDane, archivo.NombreOriginal);
                                var fullPath = Path.GetFullPath(Path.Combine(_fileStorageRoot, relativePath));

                                var directory = Path.GetDirectoryName(fullPath);
                                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                                {
                                    Directory.CreateDirectory(directory);
                                }

                                File.WriteAllBytes(fullPath, archivo.Datos);

                                // 6.2 Insertar metadatos en tab.archivo (id, file_name_original, file_name_unico = fullPath, id_tipo_archivo)
                                var queryArchivo = @"
                            INSERT INTO tab.archivo (file_name_original, file_name_unico, id_tipo_archivo)
                            OUTPUT INSERTED.id
                            VALUES (@FileNameOriginal, @FileNameUnico, @IdTipoArchivo)";

                                var paramsArchivo = new Dictionary<string, object>
                                {
                                    { "@FileNameOriginal", archivo.NombreOriginal },
                                    // Guardamos el fullPath para que otras aplicaciones puedan localizar directamente el archivo
                                    { "@FileNameUnico", fullPath.Replace('\\', '/') },
                                    { "@IdTipoArchivo", archivo.TipoArchivoId }
                                };

                                var idArchivo = await _databaseService.ExecuteScalarAsync(queryArchivo, paramsArchivo);
                                if (idArchivo == null)
                                    continue; // Saltar este archivo si hay error

                                int idArchivoInt = Convert.ToInt32(idArchivo);

                                // 6.3 Relacionar archivo con el recurso
                                var queryArchivoRecurso = @"
                            INSERT INTO tab.archivo_recurso (renderizable, id_archivo, id_recurso)
                            VALUES (@Renderizable, @IdArchivo, @IdRecurso)";

                                var paramsArchivoRecurso = new Dictionary<string, object>
                                {
                                    { "@Renderizable", archivo.Renderizable },
                                    { "@IdArchivo", idArchivoInt },
                                    { "@IdRecurso", idRecursoInt }
                                };

                                await _databaseService.ExecuteNonQueryAsync(queryArchivoRecurso, paramsArchivoRecurso);
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error al guardar archivo '{archivo.NombreOriginal}': {ex.Message}");
                        }
                    }
                }

                return idRecursoInt;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al crear actividad: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Actualiza una actividad existente en la base de datos
        /// </summary>
        public async Task<bool> ActualizarActividad(ImpulsaDBA.Shared.Requests.ActualizarActividadRequest request)
        {
            try
            {
                Console.WriteLine($"📝 Actualizando actividad - ID: {request.Id}, Título: {request.Titulo}, Fecha: {request.FechaPublicacion}, Visible: {request.ActividadActiva}, UsuarioId: {request.UsuarioId}");

                // 1. Verificar que el usuario sea el creador de la actividad
                var queryVerificarCreador = @"
                    SELECT AA.id_profesor
                    FROM tab.asignacion_academica_recurso AS AAR
                    INNER JOIN col.asignacion_academica AS AA ON AAR.id_asignacion_academica = AA.id
                    WHERE AAR.id = @Id";

                var paramsVerificar = new Dictionary<string, object>
                {
                    { "@Id", request.Id }
                };

                var resultVerificar = await _databaseService.ExecuteQueryAsync(queryVerificarCreador, paramsVerificar);
                
                if (resultVerificar.Rows.Count == 0)
                    throw new Exception($"No se encontró la actividad con ID {request.Id}");

                var idProfesorCreador = resultVerificar.Rows[0]["id_profesor"] != DBNull.Value 
                    ? Convert.ToInt32(resultVerificar.Rows[0]["id_profesor"]) 
                    : 0;

                if (idProfesorCreador != request.UsuarioId)
                {
                    throw new UnauthorizedAccessException($"No tienes permiso para editar esta actividad. Solo el docente que la creó puede modificarla.");
                }

                Console.WriteLine($"✅ Validación de creador exitosa - Profesor ID: {idProfesorCreador}");

                // 2. Obtener el id_recurso desde tab.asignacion_academica_recurso
                var queryObtenerRecurso = @"
                    SELECT id_recurso
                    FROM tab.asignacion_academica_recurso
                    WHERE id = @Id";

                var paramsObtenerRecurso = new Dictionary<string, object>
                {
                    { "@Id", request.Id }
                };

                var resultRecurso = await _databaseService.ExecuteQueryAsync(queryObtenerRecurso, paramsObtenerRecurso);
                
                if (resultRecurso.Rows.Count == 0)
                    throw new Exception($"No se encontró la actividad con ID {request.Id}");

                int idRecursoInt = Convert.ToInt32(resultRecurso.Rows[0]["id_recurso"]);
                Console.WriteLine($"✅ Recurso encontrado con ID: {idRecursoInt}");

                // 2. Actualizar tab.recurso
                var queryActualizarRecurso = @"
                    UPDATE tab.recurso
                    SET titulo = @Titulo,
                        descripcion = @Descripcion,
                        debe_entregar_archivo_estudiante = @DebeEntregarArchivoEstudiante
                    WHERE id = @IdRecurso";

                var paramsActualizarRecurso = new Dictionary<string, object>
                {
                    { "@Titulo", request.Titulo },
                    { "@Descripcion", request.Descripcion ?? string.Empty },
                    { "@DebeEntregarArchivoEstudiante", request.GeneraEntregable },
                    { "@IdRecurso", idRecursoInt }
                };

                await _databaseService.ExecuteNonQueryAsync(queryActualizarRecurso, paramsActualizarRecurso);
                Console.WriteLine($"✅ Recurso actualizado");

                // 3. Actualizar tab.asignacion_academica_recurso
                var queryActualizarAsignacion = @"
                    UPDATE tab.asignacion_academica_recurso
                    SET visible = @Visible,
                        fecha_calendario = @FechaCalendario
                    WHERE id = @Id";

                var paramsActualizarAsignacion = new Dictionary<string, object>
                {
                    { "@Visible", request.ActividadActiva },
                    { "@FechaCalendario", request.FechaPublicacion ?? DateTime.Now },
                    { "@Id", request.Id }
                };

                await _databaseService.ExecuteNonQueryAsync(queryActualizarAsignacion, paramsActualizarAsignacion);
                Console.WriteLine($"✅ Asignación académica recurso actualizada - Visible: {request.ActividadActiva}, Fecha: {request.FechaPublicacion}");

                // 4. Actualizar o insertar hipertexto según el tipo de actividad
                string? hipertextoFinal = null;

                // Si tiene hipertexto directo (Asignaciones con editor Quill)
                if (!string.IsNullOrEmpty(request.Hipertexto))
                {
                    hipertextoFinal = request.Hipertexto;
                }
                // Si tiene videos (Video de Enganche)
                else if (request.Videos != null && request.Videos.Any())
                {
                    // Para videos, concatenamos los URLs
                    var urlsVideos = request.Videos
                        .Where(v => !string.IsNullOrEmpty(v.Url))
                        .OrderBy(v => v.Orden)
                        .Select(v => v.Url);
                    hipertextoFinal = string.Join("\n", urlsVideos);
                }
                // Si tiene preguntas (Preguntas Problematizadoras)
                else if (request.Preguntas != null && request.Preguntas.Any())
                {
                    // Las preguntas se almacenan en el hipertexto como una lista
                    var preguntasTexto = string.Join("\n", request.Preguntas
                        .Where(p => !string.IsNullOrEmpty(p.Enunciado))
                        .OrderBy(p => p.Orden)
                        .Select((p, index) => $"{index + 1}. {p.Enunciado}"));
                    hipertextoFinal = preguntasTexto;
                }

                // Actualizar o insertar hipertexto si existe
                if (!string.IsNullOrEmpty(hipertextoFinal))
                {
                    // Verificar si ya existe hipertexto para este recurso
                    var queryExisteHipertexto = @"
                        SELECT COUNT(*) 
                        FROM tab.hipertexto_recurso 
                        WHERE id_recurso = @IdRecurso";

                    var paramsExisteHipertexto = new Dictionary<string, object>
                    {
                        { "@IdRecurso", idRecursoInt }
                    };

                    var existeHipertexto = await _databaseService.ExecuteScalarAsync(queryExisteHipertexto, paramsExisteHipertexto);
                    int countHipertexto = Convert.ToInt32(existeHipertexto);

                    if (countHipertexto > 0)
                    {
                        // Actualizar hipertexto existente
                        var queryActualizarHipertexto = @"
                            UPDATE tab.hipertexto_recurso
                            SET hipertexto = @Hipertexto
                            WHERE id_recurso = @IdRecurso";

                        var paramsActualizarHipertexto = new Dictionary<string, object>
                        {
                            { "@Hipertexto", hipertextoFinal },
                            { "@IdRecurso", idRecursoInt }
                        };

                        await _databaseService.ExecuteNonQueryAsync(queryActualizarHipertexto, paramsActualizarHipertexto);
                        Console.WriteLine($"✅ Hipertexto actualizado");
                    }
                    else
                    {
                        // Insertar nuevo hipertexto
                        var queryInsertarHipertexto = @"
                            INSERT INTO tab.hipertexto_recurso (hipertexto, id_recurso)
                            VALUES (@Hipertexto, @IdRecurso)";

                        var paramsInsertarHipertexto = new Dictionary<string, object>
                        {
                            { "@Hipertexto", hipertextoFinal },
                            { "@IdRecurso", idRecursoInt }
                        };

                        await _databaseService.ExecuteNonQueryAsync(queryInsertarHipertexto, paramsInsertarHipertexto);
                        Console.WriteLine($"✅ Hipertexto insertado");
                    }
                }

                // 5.1 Eliminar archivos que el usuario quitó al editar (disco + BD)
                if (request.ArchivosEliminados != null && request.ArchivosEliminados.Any())
                {
                    foreach (var idArchivo in request.ArchivosEliminados)
                    {
                        await EliminarArchivoAsync(idArchivo);
                    }
                    Console.WriteLine($"✅ Archivos eliminados: {request.ArchivosEliminados.Count}");
                }

                // 5.2 Agregar solo archivos nuevos (con Datos); los que tienen Id y no Datos se mantienen
                var archivosNuevos = request.Archivos?.Where(a => a.Datos != null && a.Datos.Length > 0).ToList();
                if (archivosNuevos != null && archivosNuevos.Any())
                {
                    var codigoDane = await ObtenerCodigoDanePorRecursoAsync(request.Id);

                    foreach (var archivo in archivosNuevos)
                    {
                        try
                        {
                            var relativePath = Path.Combine(codigoDane, archivo.NombreOriginal);
                            var fullPath = Path.GetFullPath(Path.Combine(_fileStorageRoot, relativePath));

                            var directory = Path.GetDirectoryName(fullPath);
                            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                            {
                                Directory.CreateDirectory(directory);
                            }

                            if (archivo.Datos != null && archivo.Datos.Length > 0)
                                File.WriteAllBytes(fullPath, archivo.Datos);

                            var queryArchivo = @"
                            INSERT INTO tab.archivo (file_name_original, file_name_unico, id_tipo_archivo)
                            OUTPUT INSERTED.id
                            VALUES (@FileNameOriginal, @FileNameUnico, @IdTipoArchivo)";

                            var paramsArchivo = new Dictionary<string, object>
                            {
                                { "@FileNameOriginal", archivo.NombreOriginal },
                                // Guardamos el fullPath para que otras aplicaciones puedan localizar directamente el archivo
                                { "@FileNameUnico", fullPath.Replace('\\', '/') },
                                { "@IdTipoArchivo", archivo.TipoArchivoId }
                            };

                            var idArchivo = await _databaseService.ExecuteScalarAsync(queryArchivo, paramsArchivo);
                            if (idArchivo == null) continue;

                            int idArchivoInt = Convert.ToInt32(idArchivo);
                            var queryArchivoRecurso = @"
                            INSERT INTO tab.archivo_recurso (renderizable, id_archivo, id_recurso)
                            VALUES (@Renderizable, @IdArchivo, @IdRecurso)";
                            var paramsArchivoRecurso = new Dictionary<string, object>
                            {
                                { "@Renderizable", archivo.Renderizable },
                                { "@IdArchivo", idArchivoInt },
                                { "@IdRecurso", idRecursoInt }
                            };
                            await _databaseService.ExecuteNonQueryAsync(queryArchivoRecurso, paramsArchivoRecurso);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error al guardar archivo '{archivo.NombreOriginal}' durante actualización: {ex.Message}");
                        }
                    }
                    Console.WriteLine($"✅ Archivos nuevos agregados");
                }

                Console.WriteLine($"✅ Actividad actualizada exitosamente");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al actualizar actividad: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Obtiene los datos completos de una actividad incluyendo archivos, videos, preguntas e hipertexto
        /// </summary>
        public async Task<ImpulsaDBA.Shared.DTOs.ActividadCompletaDto> ObtenerActividadCompleta(int idAsignacionAcademicaRecurso)
        {
            try
            {
                Console.WriteLine($"📖 Obteniendo datos completos de actividad - ID: {idAsignacionAcademicaRecurso}");

                // 1. Obtener datos básicos de la actividad
                var queryBasica = @"
                    SELECT 
                        AAR.id,
                        AAR.visible,
                        AAR.fecha_calendario,
                        R.titulo,
                        R.descripcion,
                        R.debe_entregar_archivo_estudiante
                    FROM tab.asignacion_academica_recurso AS AAR
                    INNER JOIN tab.recurso AS R ON AAR.id_recurso = R.id
                    WHERE AAR.id = @Id";

                var paramsBasica = new Dictionary<string, object>
                {
                    { "@Id", idAsignacionAcademicaRecurso }
                };

                var resultBasica = await _databaseService.ExecuteQueryAsync(queryBasica, paramsBasica);
                
                if (resultBasica.Rows.Count == 0)
                    throw new Exception($"No se encontró la actividad con ID {idAsignacionAcademicaRecurso}");

                var row = resultBasica.Rows[0];
                var actividad = new ImpulsaDBA.Shared.DTOs.ActividadCompletaDto
                {
                    Id = Convert.ToInt32(row["id"]),
                    Titulo = row["titulo"]?.ToString() ?? string.Empty,
                    Descripcion = row["descripcion"]?.ToString(),
                    FechaPublicacion = row["fecha_calendario"] != DBNull.Value ? Convert.ToDateTime(row["fecha_calendario"]) : null,
                    ActividadActiva = Convert.ToBoolean(row["visible"]),
                    GeneraEntregable = row["debe_entregar_archivo_estudiante"] != DBNull.Value && Convert.ToBoolean(row["debe_entregar_archivo_estudiante"])
                };

                // 2. Obtener hipertexto si existe
                var queryHipertexto = @"
                    SELECT hipertexto
                    FROM tab.hipertexto_recurso
                    WHERE id_recurso = (SELECT id_recurso FROM tab.asignacion_academica_recurso WHERE id = @Id)";

                try
                {
                    var resultHipertexto = await _databaseService.ExecuteQueryAsync(queryHipertexto, paramsBasica);
                    if (resultHipertexto.Rows.Count > 0 && resultHipertexto.Rows[0]["hipertexto"] != DBNull.Value)
                    {
                        actividad.Hipertexto = resultHipertexto.Rows[0]["hipertexto"]?.ToString();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"⚠️ No se pudo obtener hipertexto: {ex.Message}");
                }

                // 3. Obtener videos (si el hipertexto contiene URLs de video)
                if (!string.IsNullOrEmpty(actividad.Hipertexto))
                {
                    var urls = actividad.Hipertexto.Split('\n', StringSplitOptions.RemoveEmptyEntries)
                        .Where(url => !string.IsNullOrWhiteSpace(url) && 
                                     (url.Contains("youtube.com") || url.Contains("youtu.be") || 
                                      url.Contains("vimeo.com") || url.Contains("dailymotion.com")))
                        .Select((url, index) => new ImpulsaDBA.Shared.DTOs.VideoEngancheDto
                        {
                            Url = url.Trim(),
                            Orden = index + 1
                        })
                        .ToList();
                    
                    if (urls.Any())
                    {
                        actividad.Videos = urls;
                    }
                }

                // 4. Obtener preguntas (si el hipertexto contiene preguntas en formato específico)
                // Por ahora, si el hipertexto contiene "?" múltiples veces, podría ser preguntas
                // Esto se puede mejorar con un formato específico

                // 5. Obtener archivos (tab.archivo: file_name_original, file_name_unico).
                // Para registros nuevos, file_name_unico ya contiene el fullPath.
                // Para registros antiguos, puede contener solo el nombre o una ruta relativa que se complementa con el código DANE.
                var queryArchivos = @"
                    SELECT A.id, A.file_name_original AS nombre_original, A.file_name_unico AS nombre_unico,
                           A.id_tipo_archivo, AR.renderizable
                    FROM tab.archivo_recurso AS AR
                    INNER JOIN tab.archivo AS A ON AR.id_archivo = A.id
                    WHERE AR.id_recurso = (SELECT id_recurso FROM tab.asignacion_academica_recurso WHERE id = @Id)
                    ORDER BY A.id";

                try
                {
                    var resultArchivos = await _databaseService.ExecuteQueryAsync(queryArchivos, paramsBasica);
                    var codigoDane = await ObtenerCodigoDanePorRecursoAsync(idAsignacionAcademicaRecurso);

                    var archivos = new List<ImpulsaDBA.Shared.DTOs.ArchivoDto>();
                    foreach (DataRow archivoRow in resultArchivos.Rows)
                    {
                        var nomOrig = archivoRow["nombre_original"]?.ToString() ?? string.Empty;
                        var nomUnico = archivoRow["nombre_unico"]?.ToString() ?? string.Empty;
                        if (string.IsNullOrEmpty(nomUnico))
                            continue;

                        // Normalizar path y extraer solo el nombre de archivo para mostrar
                        var fullOrRelativePath = nomUnico.Replace('\\', '/');
                        var fileNameOnly = Path.GetFileName(fullOrRelativePath);

                        // Para compatibilidad: si nomUnico es solo nombre, construir una ruta relativa con el código DANE
                        string rutaConstruida;
                        if (Path.IsPathRooted(fullOrRelativePath) || fullOrRelativePath.Contains('/'))
                        {
                            // fullPath o ruta relativa ya armada
                            rutaConstruida = fullOrRelativePath;
                        }
                        else
                        {
                            rutaConstruida = Path.Combine(codigoDane, fullOrRelativePath).Replace('\\', '/');
                        }

                        archivos.Add(new ImpulsaDBA.Shared.DTOs.ArchivoDto
                        {
                            Id = Convert.ToInt32(archivoRow["id"]),
                            NombreOriginal = string.IsNullOrEmpty(nomOrig) ? fileNameOnly : nomOrig,
                            // NombreUnico se usa para mostrar en UI: solo el nombre del archivo, no la ruta completa
                            NombreUnico = fileNameOnly,
                            TipoArchivoId = Convert.ToInt32(archivoRow["id_tipo_archivo"]),
                            // Ruta contiene la ruta que se usará al reenviar el archivo al backend (fullPath o relativa)
                            Ruta = rutaConstruida,
                            Renderizable = archivoRow["renderizable"] != DBNull.Value && Convert.ToBoolean(archivoRow["renderizable"])
                        });
                    }
                    if (archivos.Any())
                    {
                        actividad.Archivos = archivos;
                        Console.WriteLine($"✅ Archivos cargados para actividad: {archivos.Count}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"⚠️ No se pudieron obtener archivos: {ex.Message}");
                }

                Console.WriteLine($"✅ Datos completos obtenidos - Título: {actividad.Titulo}, Videos: {actividad.Videos?.Count ?? 0}, Archivos: {actividad.Archivos?.Count ?? 0}");
                return actividad;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al obtener actividad completa: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Elimina una actividad de la base de datos (eliminación física)
        /// </summary>
        public async Task<bool> EliminarActividad(int idAsignacionAcademicaRecurso, int usuarioId)
        {
            try
            {
                Console.WriteLine($"🗑️ Eliminando actividad - ID: {idAsignacionAcademicaRecurso}, UsuarioId: {usuarioId}");

                // 1. Verificar que el usuario sea el creador de la actividad
                var queryVerificarCreador = @"
                    SELECT AA.id_profesor
                    FROM tab.asignacion_academica_recurso AS AAR
                    INNER JOIN col.asignacion_academica AS AA ON AAR.id_asignacion_academica = AA.id
                    WHERE AAR.id = @Id";

                var paramsVerificar = new Dictionary<string, object>
                {
                    { "@Id", idAsignacionAcademicaRecurso }
                };

                var resultVerificar = await _databaseService.ExecuteQueryAsync(queryVerificarCreador, paramsVerificar);
                
                if (resultVerificar.Rows.Count == 0)
                    throw new Exception($"No se encontró la actividad con ID {idAsignacionAcademicaRecurso}");

                var idProfesorCreador = resultVerificar.Rows[0]["id_profesor"] != DBNull.Value 
                    ? Convert.ToInt32(resultVerificar.Rows[0]["id_profesor"]) 
                    : 0;

                if (idProfesorCreador != usuarioId)
                {
                    throw new UnauthorizedAccessException($"No tienes permiso para eliminar esta actividad. Solo el docente que la creó puede eliminarla.");
                }

                Console.WriteLine($"✅ Validación de creador exitosa - Profesor ID: {idProfesorCreador}");

                // 2. Obtener el id_recurso
                var queryObtenerRecurso = @"
                    SELECT id_recurso
                    FROM tab.asignacion_academica_recurso
                    WHERE id = @Id";

                var paramsObtenerRecurso = new Dictionary<string, object>
                {
                    { "@Id", idAsignacionAcademicaRecurso }
                };

                var resultRecurso = await _databaseService.ExecuteQueryAsync(queryObtenerRecurso, paramsObtenerRecurso);
                
                if (resultRecurso.Rows.Count == 0)
                    throw new Exception($"No se encontró la actividad con ID {idAsignacionAcademicaRecurso}");

                int idRecursoInt = Convert.ToInt32(resultRecurso.Rows[0]["id_recurso"]);
                Console.WriteLine($"✅ Recurso encontrado con ID: {idRecursoInt}");

                // 2. Eliminar archivos: físico en disco + tab.archivo_recurso + tab.archivo
                var queryIdsArchivo = "SELECT id_archivo FROM tab.archivo_recurso WHERE id_recurso = @IdRecurso";
                var paramsEliminarArchivos = new Dictionary<string, object> { { "@IdRecurso", idRecursoInt } };
                var resultIds = await _databaseService.ExecuteQueryAsync(queryIdsArchivo, paramsEliminarArchivos);
                for (int i = 0; i < resultIds.Rows.Count; i++)
                {
                    int idArchivo = Convert.ToInt32(resultIds.Rows[i]["id_archivo"]);
                    await EliminarArchivoAsync(idArchivo);
                }
                if (resultIds.Rows.Count > 0)
                    Console.WriteLine($"✅ Archivos eliminados (disco y BD): {resultIds.Rows.Count}");

                // 3. Eliminar hipertexto asociado
                var queryEliminarHipertexto = @"
                    DELETE FROM tab.hipertexto_recurso
                    WHERE id_recurso = @IdRecurso";

                await _databaseService.ExecuteNonQueryAsync(queryEliminarHipertexto, paramsEliminarArchivos);
                Console.WriteLine($"✅ Hipertexto eliminado");

                // 4. Eliminar asignacion_academica_recurso
                var queryEliminarAsignacionRecurso = @"
                    DELETE FROM tab.asignacion_academica_recurso
                    WHERE id = @Id";

                await _databaseService.ExecuteNonQueryAsync(queryEliminarAsignacionRecurso, paramsObtenerRecurso);
                Console.WriteLine($"✅ Asignación académica recurso eliminada");

                // 5. Eliminar el recurso
                var queryEliminarRecurso = @"
                    DELETE FROM tab.recurso
                    WHERE id = @IdRecurso";

                await _databaseService.ExecuteNonQueryAsync(queryEliminarRecurso, paramsEliminarArchivos);
                Console.WriteLine($"✅ Recurso eliminado");

                Console.WriteLine($"✅ Actividad eliminada exitosamente");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al eliminar actividad: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Mueve una actividad a otra fecha (solo actualiza fecha_calendario en tab.asignacion_academica_recurso).
        /// </summary>
        public async Task<bool> MoverActividadAsync(int idAsignacionAcademicaRecurso, DateTime nuevaFecha)
        {
            var query = @"
                UPDATE tab.asignacion_academica_recurso
                SET fecha_calendario = @NuevaFecha
                WHERE id = @Id";
            var parameters = new Dictionary<string, object>
            {
                { "@Id", idAsignacionAcademicaRecurso },
                { "@NuevaFecha", nuevaFecha }
            };
            await _databaseService.ExecuteNonQueryAsync(query, parameters);
            return true;
        }

        /// <summary>
        /// Lista actividades del docente para una asignatura (para la pantalla de duplicar).
        /// </summary>
        public async Task<List<ActividadParaDuplicarDto>> ObtenerActividadesParaDuplicarAsync(int profesorId, int idAsignatura)
        {
            var query = @"
                SELECT 
                    AAR.id AS id,
                    R.titulo AS titulo,
                    AAR.fecha_calendario AS fecha_publicacion,
                    AAR.id_asignacion_academica AS id_asignacion_academica,
                    GR.id_grado AS id_grado,
                    GR.nombre AS nombre_grupo,
                    A.asignatura AS nombre_asignatura
                FROM tab.asignacion_academica_recurso AS AAR
                INNER JOIN tab.recurso AS R ON AAR.id_recurso = R.id
                INNER JOIN col.asignacion_academica AS AA ON AAR.id_asignacion_academica = AA.id
                INNER JOIN aca.grupo AS GR ON AA.id_grupo = GR.id
                INNER JOIN col.asignatura AS A ON AA.id_asignatura = A.id
                WHERE AA.id_profesor = @ProfesorId
                  AND AA.id_asignatura = @IdAsignatura
                ORDER BY AAR.fecha_calendario DESC, R.titulo";

            var parameters = new Dictionary<string, object>
            {
                { "@ProfesorId", profesorId },
                { "@IdAsignatura", idAsignatura }
            };

            var result = await _databaseService.ExecuteQueryAsync(query, parameters);
            var list = new List<ActividadParaDuplicarDto>();
            foreach (DataRow row in result.Rows)
            {
                list.Add(new ActividadParaDuplicarDto
                {
                    Id = Convert.ToInt32(row["id"]),
                    Titulo = row["titulo"]?.ToString() ?? string.Empty,
                    FechaPublicacion = row["fecha_publicacion"] != DBNull.Value ? Convert.ToDateTime(row["fecha_publicacion"]) : null,
                    IdAsignacionAcademica = Convert.ToInt32(row["id_asignacion_academica"]),
                    IdGrado = Convert.ToInt32(row["id_grado"]),
                    NombreGrupo = row["nombre_grupo"]?.ToString() ?? string.Empty,
                    NombreAsignatura = row["nombre_asignatura"]?.ToString() ?? string.Empty
                });
            }
            return list;
        }

        /// <summary>
        /// Obtiene otros cursos (grupos) con la misma asignatura para duplicar. Solo usa col.asignacion_academica y aca.grupo.
        /// </summary>
        public async Task<List<GrupoMismoGradoDto>> ObtenerGruposMismoGradoAsync(int idAsignacionAcademica, int profesorId)
        {
            Console.WriteLine($"🔁 Duplicar: idAsignacionAcademica={idAsignacionAcademica}, profesorId={profesorId}");
            var queryOrigen = @"
                SELECT id_asignatura, id_profesor
                FROM col.asignacion_academica
                WHERE id = @Id";
            var dr = await _databaseService.ExecuteQueryAsync(queryOrigen, new Dictionary<string, object> { { "@Id", idAsignacionAcademica } });
            if (dr.Rows.Count == 0)
            {
                Console.WriteLine($"⚠️ Duplicar: no se encontró asignación id={idAsignacionAcademica} en col.asignacion_academica (revisar BD o conexión)");
                return new List<GrupoMismoGradoDto>();
            }
            int idAsignatura = Convert.ToInt32(dr.Rows[0]["id_asignatura"]);
            int idProfesor = Convert.ToInt32(dr.Rows[0]["id_profesor"]);
            Console.WriteLine($"🔁 Duplicar: id_asignatura={idAsignatura}, id_profesor={idProfesor}");

            // aca.grupo: usar nombre_grupo desde GR.nombre o GR.grupo según exista
            var queryDestinos = @"
                SELECT 
                    AA.id AS id_asignacion_academica,
                    GR.id AS id_grupo,
                    ISNULL(GR.nombre, GR.grupo) AS nombre_grupo
                FROM col.asignacion_academica AS AA
                INNER JOIN aca.grupo AS GR ON AA.id_grupo = GR.id
                WHERE AA.id_asignatura = @IdAsignatura
                  AND AA.id_profesor = @IdProfesor
                  AND AA.id <> @IdAsignacionAcademica
                ORDER BY ISNULL(GR.nombre, GR.grupo)";
            var parameters = new Dictionary<string, object>
            {
                { "@IdAsignatura", idAsignatura },
                { "@IdProfesor", idProfesor },
                { "@IdAsignacionAcademica", idAsignacionAcademica }
            };
            DataTable result;
            try
            {
                result = await _databaseService.ExecuteQueryAsync(queryDestinos, parameters);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Duplicar: error en query destinos: {ex.Message}");
                throw;
            }
            var list = new List<GrupoMismoGradoDto>();
            foreach (DataRow row in result.Rows)
            {
                list.Add(new GrupoMismoGradoDto
                {
                    IdAsignacionAcademica = Convert.ToInt32(row["id_asignacion_academica"]),
                    IdGrupo = Convert.ToInt32(row["id_grupo"]),
                    NombreGrupo = row["nombre_grupo"]?.ToString() ?? string.Empty
                });
            }
            Console.WriteLine($"✅ Duplicar: {list.Count} destino(s) encontrado(s)");
            return list;
        }

        /// <summary>
        /// Duplica una actividad a los grupos destino con sus fechas/horas. Un solo recurso compartido por todos los destinos.
        /// </summary>
        public async Task<int> DuplicarActividadAsync(ImpulsaDBA.Shared.Requests.DuplicarActividadRequest request)
        {
            if (request?.Destinos == null || !request.Destinos.Any())
                throw new ArgumentException("Debe indicar al menos un grupo destino.");

            var actividad = await ObtenerActividadCompleta(request.IdAsignacionAcademicaRecursoOrigen);

            // 1. Insertar nuevo recurso (copia)
            var queryRecurso = @"
                INSERT INTO tab.recurso (titulo, descripcion, id_tipo_recurso, debe_entregar_archivo_estudiante)
                OUTPUT INSERTED.id
                VALUES (@Titulo, @Descripcion, (SELECT TOP 1 id_tipo_recurso FROM tab.recurso WHERE id = (SELECT id_recurso FROM tab.asignacion_academica_recurso WHERE id = @IdOrigen)), @DebeEntregar)";

            var idRecursoParam = await _databaseService.ExecuteScalarAsync(
                "SELECT id_recurso FROM tab.asignacion_academica_recurso WHERE id = @IdOrigen",
                new Dictionary<string, object> { { "@IdOrigen", request.IdAsignacionAcademicaRecursoOrigen } });
            if (idRecursoParam == null)
                throw new Exception("No se encontró la actividad origen.");
            int idRecursoOrigen = Convert.ToInt32(idRecursoParam);

            var paramsRecurso = new Dictionary<string, object>
            {
                { "@Titulo", actividad.Titulo },
                { "@Descripcion", actividad.Descripcion ?? string.Empty },
                { "@IdOrigen", request.IdAsignacionAcademicaRecursoOrigen },
                { "@DebeEntregar", actividad.GeneraEntregable }
            };
            var idRecursoNuevoObj = await _databaseService.ExecuteScalarAsync(queryRecurso, paramsRecurso);
            if (idRecursoNuevoObj == null)
                throw new Exception("Error al crear el recurso duplicado.");
            int idRecursoNuevo = Convert.ToInt32(idRecursoNuevoObj);

            // 2. Copiar hipertexto
            var queryHipertexto = @"
                INSERT INTO tab.hipertexto_recurso (hipertexto, id_recurso)
                SELECT hipertexto, @IdRecursoNuevo
                FROM tab.hipertexto_recurso
                WHERE id_recurso = @IdRecursoOrigen";
            await _databaseService.ExecuteNonQueryAsync(queryHipertexto, new Dictionary<string, object>
            {
                { "@IdRecursoNuevo", idRecursoNuevo },
                { "@IdRecursoOrigen", idRecursoOrigen }
            });

            // 3. Copiar archivo_recurso (mismo id_archivo, nuevo id_recurso)
            var queryArchivoRecurso = @"
                INSERT INTO tab.archivo_recurso (renderizable, id_archivo, id_recurso)
                SELECT renderizable, id_archivo, @IdRecursoNuevo
                FROM tab.archivo_recurso
                WHERE id_recurso = @IdRecursoOrigen";
            await _databaseService.ExecuteNonQueryAsync(queryArchivoRecurso, new Dictionary<string, object>
            {
                { "@IdRecursoNuevo", idRecursoNuevo },
                { "@IdRecursoOrigen", idRecursoOrigen }
            });

            // 4. Una fila en asignacion_academica_recurso por destino (fecha_calendario = Fecha + Hora)
            foreach (var d in request.Destinos)
            {
                if (!TimeSpan.TryParse(d.Hora, out var hora))
                    hora = TimeSpan.Zero;
                var fechaCalendario = d.Fecha.Date.Add(hora);

                var queryAAR = @"
                    INSERT INTO tab.asignacion_academica_recurso (presencial, visible, fecha_calendario, fecha_creacion_registro, id_asignacion_academica, id_recurso)
                    VALUES (0, 1, @FechaCalendario, GETDATE(), @IdAsignacionAcademica, @IdRecurso)";
                await _databaseService.ExecuteNonQueryAsync(queryAAR, new Dictionary<string, object>
                {
                    { "@FechaCalendario", fechaCalendario },
                    { "@IdAsignacionAcademica", d.IdAsignacionAcademica },
                    { "@IdRecurso", idRecursoNuevo }
                });
            }

            return idRecursoNuevo;
        }
    }
}
