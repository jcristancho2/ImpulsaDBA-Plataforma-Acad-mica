using System.Data;
using ImpulsaDBA.API.Infrastructure.Database;
using ImpulsaDBA.Shared.DTOs;

namespace ImpulsaDBA.API.Application.Services
{
    /// <summary>
    /// Servicio para obtener ayudas desde la tabla bas.ayuda
    /// </summary>
    public class AyudaService
    {
        private readonly ImpulsaDBA.API.Infrastructure.Database.DatabaseService _databaseService;

        public AyudaService(ImpulsaDBA.API.Infrastructure.Database.DatabaseService databaseService)
        {
            _databaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService));
        }

        /// <summary>
        /// Obtiene las ayudas (PDF y VIDEO) para un componente específico
        /// Según los datos proporcionados:
        /// 63	101	VIDEO	https://www.youtube.com/@TecnoEducaColombia
        /// 64	102	PDF	https://colegiotic.com/wp-content/uploads/2021/01/BrochureKitQuieroSaber.pdf
        /// 65	103	VIDEO	https://www.youtube.com/@TecnoEducaColombia
        /// 66	104	PDF	https://colegiotic.com/wp-content/uploads/2021/01/BrochureKitQuieroSaber.pdf
        /// 67	105	VIDEO	https://www.youtube.com/@TecnoEducaColombia
        /// 68	106	PDF	https://colegiotic.com/wp-content/uploads/2021/01/BrochureKitQuieroSaber.pdf
        /// 
        /// El parámetro idComponente es el codigo_aplicacion del VIDEO (101, 103, 105)
        /// El PDF siempre tiene codigo_aplicacion = idComponente + 1 (102, 104, 106)
        /// </summary>
        public async Task<(AyudaDto? PDF, AyudaDto? VIDEO)> ObtenerAyudasPorComponente(int idComponente)
        {
            try
            {
                Console.WriteLine($"🔍 ObtenerAyudasPorComponente - idComponente: {idComponente}");
                
                // idComponente es el codigo_aplicacion del VIDEO
                // PDF tiene codigo_aplicacion = idComponente + 1
                var codigoPDF = idComponente + 1;
                var codigoVIDEO = idComponente;
                
                Console.WriteLine($"🔍 Buscando ayudas - PDF codigo: {codigoPDF}, VIDEO codigo: {codigoVIDEO}");
                
                var parameters = new Dictionary<string, object>
                {
                    { "@CodigoPDF", codigoPDF },
                    { "@CodigoVIDEO", codigoVIDEO }
                };

                // Consulta base - intentar con tipo, si falla usar sin tipo
                DataTable result;
                bool tieneColumnaTipo = false;
                
                // Primero intentar consulta con columna tipo
                try
                {
                    var queryConTipo = @"
                        SELECT 
                            id,
                            codigo_aplicacion AS CodigoAplicacion,
                            nombre_ayuda AS NombreAyuda,
                            url_ayuda AS UrlAyuda,
                            tipo AS Tipo
                        FROM bas.ayuda
                        WHERE codigo_aplicacion = @CodigoPDF OR codigo_aplicacion = @CodigoVIDEO
                        ORDER BY codigo_aplicacion";
                    
                    result = await _databaseService.ExecuteQueryAsync(queryConTipo, parameters);
                    tieneColumnaTipo = true;
                    Console.WriteLine($"✅ Consulta ejecutada con columna tipo - Filas encontradas: {result.Rows.Count}");
                }
                catch (Exception exTipo)
                {
                    // Si falla, usar consulta sin tipo (fallback)
                    Console.WriteLine($"⚠️ Columna tipo no encontrada: {exTipo.Message}, usando fallback por codigo_aplicacion");
                    var querySinTipo = @"
                        SELECT 
                            id,
                            codigo_aplicacion AS CodigoAplicacion,
                            nombre_ayuda AS NombreAyuda,
                            url_ayuda AS UrlAyuda
                        FROM bas.ayuda
                        WHERE codigo_aplicacion = @CodigoPDF OR codigo_aplicacion = @CodigoVIDEO
                        ORDER BY codigo_aplicacion";
                    
                    result = await _databaseService.ExecuteQueryAsync(querySinTipo, parameters);
                    tieneColumnaTipo = false;
                    Console.WriteLine($"✅ Consulta ejecutada sin tipo - Filas encontradas: {result.Rows.Count}");
                }
                
                AyudaDto? pdf = null;
                AyudaDto? video = null;

                Console.WriteLine($"📊 Procesando {result.Rows.Count} filas de ayudas");
                
                foreach (DataRow row in result.Rows)
                {
                    var codigoAplicacion = Convert.ToInt32(row["CodigoAplicacion"]);
                    var urlAyuda = row["UrlAyuda"]?.ToString() ?? "";
                    var tipo = "";
                    
                    // Obtener tipo solo si la columna existe
                    if (tieneColumnaTipo && result.Columns.Contains("Tipo"))
                    {
                        tipo = row["Tipo"]?.ToString()?.ToUpper() ?? "";
                    }
                    
                    Console.WriteLine($"  - Fila: id={row["id"]}, codigo={codigoAplicacion}, tipo={tipo}, url={urlAyuda}");
                    
                    var ayuda = new AyudaDto
                    {
                        Id = Convert.ToInt32(row["id"]),
                        CodigoAplicacion = row["CodigoAplicacion"]?.ToString() ?? string.Empty,
                        NombreAyuda = row["NombreAyuda"]?.ToString() ?? string.Empty,
                        UrlAyuda = urlAyuda
                    };

                    // Determinar si es PDF o VIDEO
                    bool asignado = false;
                    
                    if (tieneColumnaTipo && !string.IsNullOrEmpty(tipo))
                    {
                        // Usar columna tipo si está disponible
                        if (tipo == "PDF")
                        {
                            pdf = ayuda;
                            Console.WriteLine($"    → Asignado como PDF (por tipo)");
                            asignado = true;
                        }
                        else if (tipo == "VIDEO")
                        {
                            video = ayuda;
                            Console.WriteLine($"    → Asignado como VIDEO (por tipo)");
                            asignado = true;
                        }
                    }
                    
                    // Fallback: usar codigo_aplicacion si no se asignó por tipo
                    if (!asignado)
                    {
                        if (codigoAplicacion == codigoPDF)
                        {
                            pdf = ayuda;
                            Console.WriteLine($"    → Asignado como PDF (por codigo {codigoPDF})");
                        }
                        else if (codigoAplicacion == codigoVIDEO)
                        {
                            video = ayuda;
                            Console.WriteLine($"    → Asignado como VIDEO (por codigo {codigoVIDEO})");
                        }
                        else
                        {
                            Console.WriteLine($"    ⚠️ Código {codigoAplicacion} no coincide con PDF ({codigoPDF}) ni VIDEO ({codigoVIDEO})");
                        }
                    }
                }

                Console.WriteLine($"✅ Ayudas obtenidas - Componente: {idComponente}, PDF: {(pdf != null ? $"Sí (URL: {pdf.UrlAyuda})" : "No")}, VIDEO: {(video != null ? $"Sí (URL: {video.UrlAyuda})" : "No")}");
                
                // Si no se encontraron ayudas, verificar si existen en la base de datos
                if (pdf == null && video == null)
                {
                    Console.WriteLine($"⚠️ No se encontraron ayudas para componente {idComponente}. Verificando si existen registros en bas.ayuda...");
                    var queryVerificar = @"
                        SELECT codigo_aplicacion, nombre_ayuda, url_ayuda
                        FROM bas.ayuda
                        WHERE codigo_aplicacion = @CodigoPDF OR codigo_aplicacion = @CodigoVIDEO";
                    var resultVerificar = await _databaseService.ExecuteQueryAsync(queryVerificar, parameters);
                    Console.WriteLine($"🔍 Registros encontrados en bas.ayuda: {resultVerificar.Rows.Count}");
                    foreach (DataRow row in resultVerificar.Rows)
                    {
                        Console.WriteLine($"   - codigo_aplicacion: {row["codigo_aplicacion"]}, nombre: {row["nombre_ayuda"]}, url: {row["url_ayuda"]}");
                    }
                }
                
                return (pdf, video);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error al obtener ayudas por componente: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return (null, null);
            }
        }
    }
}
