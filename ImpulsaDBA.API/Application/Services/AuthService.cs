using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using ImpulsaDBA.API.Domain.Entities;
using ImpulsaDBA.API.Infrastructure.Database;
using ImpulsaDBA.Shared.DTOs;
using ImpulsaDBA.Shared.Responses;
using ImpulsaDBA.Shared.Requests;
using BCrypt.Net;

namespace ImpulsaDBA.API.Application.Services
{
    /// <summary>
    /// Servicio de autenticación que valida credenciales contra la base de datos SQL Server.
    /// 
    /// CONEXIÓN A BASE DE DATOS:
    /// Utiliza DatabaseService que se conecta a SQL Server usando la cadena configurada en:
    /// - appsettings.json -> ConnectionStrings.DefaultConnection (prioridad más alta)
    /// - Variables de entorno (.env) -> DB_SERVER, DB_NAME, DB_USER, DB_PASSWORD
    /// 
    /// MÉTODOS DE VALIDACIÓN:
    /// - ValidarCredencialesUsuario: Valida correo, cédula o número de celular
    /// - ValidarCredencialesPassword: Valida que la contraseña (cédula) coincida con el usuario
    /// </summary>
    public class AuthService
    {
        private readonly ImpulsaDBA.API.Infrastructure.Database.DatabaseService _databaseService;

        /// <summary>
        /// Constructor que recibe el servicio de base de datos inyectado.
        /// La conexión a la BD se realiza a través de DatabaseService.
        /// </summary>
        public AuthService(ImpulsaDBA.API.Infrastructure.Database.DatabaseService databaseService)
        {
            _databaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService));
        }

        /// <summary>
        /// Valida las credenciales del usuario (correo, cédula o número de celular).
        /// 
        /// CONSULTA A BASE DE DATOS:
        /// Busca en la tabla col.persona un registro que coincida con:
        /// - e_mail (case-insensitive, sin espacios), o
        /// - nro_documento (sin espacios), o
        /// - celular (sin espacios, maneja NULL)
        /// 
        /// También obtiene el rol del usuario desde seg.rol_persona y seg.rol
        /// La consulta se ejecuta usando DatabaseService que maneja la conexión a SQL Server.
        /// </summary>
        /// <param name="correo">Correo electrónico del usuario</param>
        /// <param name="cedula">Número de documento (cédula) del usuario</param>
        /// <param name="numeroCelular">Número de celular del usuario</param>
        /// <returns>UsuarioDto si las credenciales son válidas, null en caso contrario</returns>
        public async Task<UsuarioDto?> ValidarCredencialesUsuario(string? correo, string? cedula, string? numeroCelular)
        {
            try
            {
                // Limpiar y normalizar los valores de entrada
                var emailLimpio = correo?.Trim() ?? string.Empty;
                var cedulaLimpia = cedula?.Trim() ?? string.Empty;
                var celularLimpio = numeroCelular?.Trim() ?? string.Empty;

                Console.WriteLine($"🔍 Buscando usuario con:");
                Console.WriteLine($"   Email: '{emailLimpio}'");
                Console.WriteLine($"   Cédula: '{cedulaLimpia}'");
                Console.WriteLine($"   Celular: '{celularLimpio}'");

                // Construir consulta SQL para buscar persona por correo, cédula o celular
                // La consulta se ejecuta contra la base de datos configurada en DatabaseService
                // Obtiene el rol activo del usuario desde seg.rol_persona
                // Usa LTRIM/RTRIM para eliminar espacios y LOWER para comparación case-insensitive del email
                var query = @"
                    SELECT TOP 1
                        p.id,
                        p.nro_documento,
                        p.e_mail,
                        p.celular,
                        p.primer_nombre,
                        p.primer_apellido,
                        p.segundo_apellido,
                        p.otros_nombres,
                        p.url_foto,
                        r.rol AS perfil
                    FROM col.persona AS p
                    LEFT OUTER JOIN seg.rol_persona AS rp ON p.id = rp.id_persona 
                        AND rp.plataforma = 1 
                        AND rp.eliminado = 0
                        AND (rp.fec_terminacion IS NULL OR rp.fec_terminacion >= CAST(GETDATE() AS DATE))
                    LEFT OUTER JOIN seg.rol AS r ON rp.id_rol = r.id
                    WHERE (
                        (LTRIM(RTRIM(LOWER(ISNULL(p.e_mail, '')))) = LTRIM(RTRIM(LOWER(@Email)))) OR
                        (LTRIM(RTRIM(ISNULL(p.nro_documento, ''))) = LTRIM(RTRIM(@Cedula))) OR
                        (LTRIM(RTRIM(ISNULL(p.celular, ''))) = LTRIM(RTRIM(@Celular)))
                    )
                    ORDER BY rp.id_anio DESC";

                var parameters = new Dictionary<string, object>
                {
                    { "@Email", emailLimpio },
                    { "@Cedula", cedulaLimpia },
                    { "@Celular", celularLimpio }
                };

                // Ejecutar consulta usando DatabaseService (conexión a SQL Server)
                var result = await _databaseService.ExecuteQueryAsync(query, parameters);

                Console.WriteLine($"🔍 Resultados encontrados: {result.Rows.Count}");

                // Si se encontró un usuario, mapear a objeto Usuario
                if (result.Rows.Count > 0)
                {
                    var row = result.Rows[0];
                    
                    // Construir nombre completo desde los campos de persona
                    var nombreCompleto = $"{row["primer_apellido"]?.ToString()?.Trim() ?? ""} " +
                                        $"{row["segundo_apellido"]?.ToString()?.Trim() ?? ""} " +
                                        $"{row["primer_nombre"]?.ToString()?.Trim() ?? ""} " +
                                        $"{row["otros_nombres"]?.ToString()?.Trim() ?? ""}";
                    nombreCompleto = nombreCompleto.Trim();
                    
                    return new UsuarioDto
                    {
                        Id = Convert.ToInt32(row["id"]),
                        NroDocumento = row["nro_documento"]?.ToString(),
                        Email = row["e_mail"]?.ToString(),
                        Celular = row["celular"]?.ToString(),
                        NombreCompleto = nombreCompleto,
                        Perfil = row["perfil"]?.ToString() ?? "Usuario",
                        FotoUrl = row["url_foto"]?.ToString(),
                        Activo = true // Asumimos activo si existe en la base de datos
                    };
                }

                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al validar credenciales de usuario: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Valida que la contraseña ingresada coincida con el hash almacenado en col.persona_password.
        /// 
        /// CONSULTA A BASE DE DATOS:
        /// 1. Busca el hash de la contraseña en col.persona_password para el usuario
        /// 2. Si existe, usa BCrypt para verificar la contraseña ingresada contra el hash
        /// 3. Si no existe hash, valida que el password sea el nro_documento (comportamiento legacy)
        /// 
        /// La consulta se ejecuta usando DatabaseService que maneja la conexión a SQL Server.
        /// </summary>
        /// <param name="password">Contraseña ingresada por el usuario</param>
        /// <param name="usuarioId">ID del usuario a validar</param>
        /// <returns>UsuarioDto si la contraseña es válida, null en caso contrario</returns>
        public async Task<UsuarioDto?> ValidarCredencialesPassword(string password, int? usuarioId = null)
        {
            try
            {
                if (!usuarioId.HasValue)
                {
                    return null;
                }

                var parametersPassword = new Dictionary<string, object>
                {
                    { "@UsuarioId", usuarioId.Value }
                };

                // Primero intentar buscar el hash en col.persona_password
                var queryPasswordHash = @"
                    SELECT TOP 1 pp.password
                    FROM col.persona_password AS pp
                    WHERE pp.id_persona = @UsuarioId";

                var resultPasswordHash = await _databaseService.ExecuteQueryAsync(queryPasswordHash, parametersPassword);
                
                Console.WriteLine($"🔍 Buscando hash para usuario ID: {usuarioId}");
                Console.WriteLine($"🔍 Filas encontradas: {resultPasswordHash.Rows.Count}");
                
                if (resultPasswordHash.Rows.Count > 0)
                {
                    // Usuario tiene contraseña personalizada hasheada
                    var passwordHash = resultPasswordHash.Rows[0]["password"]?.ToString();
                    
                    // Limpiar espacios en blanco al inicio y final del hash
                    if (!string.IsNullOrEmpty(passwordHash))
                    {
                        passwordHash = passwordHash.Trim();
                    }
                    
                    Console.WriteLine($"🔍 Hash encontrado. Longitud: {passwordHash?.Length ?? 0}");
                    Console.WriteLine($"🔍 Hash (primeros 20 chars): {(passwordHash?.Length > 20 ? passwordHash.Substring(0, 20) + "..." : passwordHash)}");
                    Console.WriteLine($"🔍 Contraseña ingresada (longitud): {password?.Length ?? 0}");
                    
                    if (string.IsNullOrEmpty(passwordHash))
                    {
                        Console.WriteLine("⚠️ Hash de contraseña encontrado pero está vacío");
                        return null;
                    }

                    // Verificar la contraseña usando BCrypt
                    try
                    {
                        Console.WriteLine($"🔐 Intentando verificar contraseña con BCrypt...");
                        var esValida = BCrypt.Net.BCrypt.Verify(password, passwordHash);
                        
                        if (esValida)
                        {
                            Console.WriteLine($"✅ Contraseña verificada correctamente con BCrypt para usuario ID: {usuarioId}");
                        }
                        else
                        {
                            Console.WriteLine($"❌ Contraseña incorrecta para usuario ID: {usuarioId}");
                            Console.WriteLine($"❌ La contraseña ingresada no coincide con el hash almacenado");
                            return null;
                        }
                    }
                    catch (Exception exBcrypt)
                    {
                        Console.WriteLine($"❌ Error al verificar contraseña con BCrypt: {exBcrypt.Message}");
                        Console.WriteLine($"❌ Stack trace: {exBcrypt.StackTrace}");
                        return null;
                    }
                }
                else
                {
                    // No hay hash, usar comportamiento legacy: validar que el password sea el nro_documento
                    Console.WriteLine($"⚠️ No se encontró hash en persona_password para usuario ID: {usuarioId}, usando validación por número de documento");
                    
                    var queryDocumento = @"
                        SELECT TOP 1 p.nro_documento
                        FROM col.persona AS p
                        WHERE p.id = @UsuarioId";

                    var resultDocumento = await _databaseService.ExecuteQueryAsync(queryDocumento, parametersPassword);
                    
                    if (resultDocumento.Rows.Count > 0)
                    {
                        var nroDocumento = resultDocumento.Rows[0]["nro_documento"]?.ToString();
                        // Validar que el password coincida con el número de documento
                        if (nroDocumento != password)
                        {
                            Console.WriteLine($"❌ Contraseña (nro_documento) incorrecta para usuario ID: {usuarioId}");
                            return null;
                        }
                    }
                    else
                    {
                        Console.WriteLine($"❌ Usuario ID: {usuarioId} no encontrado");
                        return null;
                    }
                }

                // Si la contraseña es válida (ya sea por BCrypt o por nro_documento), obtener los datos completos del usuario
                var queryUsuario = @"
                    SELECT TOP 1
                        p.id,
                        p.nro_documento,
                        p.e_mail,
                        p.celular,
                        p.primer_nombre,
                        p.primer_apellido,
                        p.segundo_apellido,
                        p.otros_nombres,
                        p.url_foto,
                        r.rol AS perfil
                    FROM col.persona AS p
                    LEFT OUTER JOIN seg.rol_persona AS rp ON p.id = rp.id_persona 
                        AND rp.plataforma = 1 
                        AND rp.eliminado = 0
                        AND (rp.fec_terminacion IS NULL OR rp.fec_terminacion >= CAST(GETDATE() AS DATE))
                    LEFT OUTER JOIN seg.rol AS r ON rp.id_rol = r.id
                    WHERE p.id = @UsuarioId
                    ORDER BY rp.id_anio DESC";

                var resultUsuario = await _databaseService.ExecuteQueryAsync(queryUsuario, parametersPassword);
                
                if (resultUsuario.Rows.Count > 0)
                {
                    var row = resultUsuario.Rows[0];
                    
                    // Construir nombre completo desde los campos de persona
                    var nombreCompleto = $"{row["primer_apellido"]?.ToString()?.Trim() ?? ""} " +
                                        $"{row["segundo_apellido"]?.ToString()?.Trim() ?? ""} " +
                                        $"{row["primer_nombre"]?.ToString()?.Trim() ?? ""} " +
                                        $"{row["otros_nombres"]?.ToString()?.Trim() ?? ""}";
                    nombreCompleto = nombreCompleto.Trim();
                    
                    return new UsuarioDto
                    {
                        Id = Convert.ToInt32(row["id"]),
                        NroDocumento = row["nro_documento"]?.ToString(),
                        Email = row["e_mail"]?.ToString(),
                        Celular = row["celular"]?.ToString(),
                        NombreCompleto = nombreCompleto,
                        Perfil = row["perfil"]?.ToString() ?? "Usuario",
                        FotoUrl = row["url_foto"]?.ToString(),
                        Activo = true
                    };
                }

                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al validar contraseña: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Valida credenciales completas (usuario y contraseña) en un solo paso.
        /// 
        /// CONSULTA A BASE DE DATOS:
        /// 1. Busca la persona en col.persona por correo/cédula/celular
        /// 2. Valida la contraseña:
        ///    - Si existe hash en col.persona_password, verifica con BCrypt
        ///    - Si no existe hash, valida que la contraseña sea el nro_documento (comportamiento legacy)
        /// 3. Obtiene el rol del usuario desde seg.rol_persona y seg.rol
        /// 
        /// Utiliza DatabaseService para todas las consultas a SQL Server.
        /// </summary>
        /// <param name="usuario">Correo, cédula o número de celular del usuario</param>
        /// <param name="password">Contraseña ingresada por el usuario</param>
        /// <returns>LoginResponse con el resultado de la validación</returns>
        public async Task<LoginResponse> ValidarLoginCompleto(string usuario, string password)
        {
            try
            {
                Console.WriteLine($"🔐 Iniciando validación de login para usuario: {usuario}");
                
                // Primero validar que el usuario existe (por correo, cédula o celular)
                var usuarioEncontrado = await ValidarCredencialesUsuario(usuario, usuario, usuario);
                
                if (usuarioEncontrado == null)
                {
                    Console.WriteLine($"❌ Usuario no encontrado: {usuario}");
                    return new LoginResponse
                    {
                        Success = false,
                        Message = "Usuario no encontrado"
                    };
                }

                Console.WriteLine($"✅ Usuario encontrado. ID: {usuarioEncontrado.Id}, Nombre: {usuarioEncontrado.NombreCompleto}");

                // Luego validar que la contraseña coincida
                Console.WriteLine($"🔐 Validando contraseña para usuario ID: {usuarioEncontrado.Id}");
                var passwordValida = await ValidarCredencialesPassword(password, usuarioEncontrado.Id);
                
                if (passwordValida == null || passwordValida.Id != usuarioEncontrado.Id)
                {
                    Console.WriteLine($"❌ Contraseña inválida para usuario ID: {usuarioEncontrado.Id}");
                    return new LoginResponse
                    {
                        Success = false,
                        Message = "Contraseña incorrecta"
                    };
                }
                
                Console.WriteLine($"✅ Contraseña válida para usuario ID: {usuarioEncontrado.Id}");

                // Generar token (por ahora un GUID, luego se puede implementar JWT)
                var token = Guid.NewGuid().ToString();

                // Retornar respuesta exitosa con usuario y token
                return new LoginResponse
                {
                    Success = true,
                    Message = "Login exitoso",
                    Usuario = new SesionUsuarioResponse
                    {
                        UsuarioId = passwordValida.Id,
                        NombreCompleto = passwordValida.NombreCompleto,
                        Email = passwordValida.Email,
                        Perfil = passwordValida.Perfil,
                        FotoUrl = passwordValida.FotoUrl,
                        Token = token
                    },
                    Token = token
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al validar login completo: {ex.Message}");
                return new LoginResponse
                {
                    Success = false,
                    Message = $"Error al validar credenciales: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Valida la información del usuario para recuperación de contraseña.
        /// Verifica que el correo, celular y número de documento coincidan con un usuario en col.persona
        /// </summary>
        public async Task<bool> ValidarInformacionRecuperacion(ValidarInformacionRecuperacionRequest request)
        {
            try
            {
                // Limpiar y normalizar los datos de entrada
                var email = (request.Email ?? string.Empty).Trim();
                var celular = (request.Celular ?? string.Empty).Trim();
                var nroDocumento = (request.NroDocumento ?? string.Empty).Trim();

                Console.WriteLine($"🔍 Validando información de recuperación:");
                Console.WriteLine($"   Email: '{email}'");
                Console.WriteLine($"   Celular: '{celular}'");
                Console.WriteLine($"   NroDocumento: '{nroDocumento}'");

                // Usar comparaciones case-insensitive y trim para mayor robustez
                // El celular es opcional: si está vacío/NULL en la BD, no se valida el celular ingresado
                // Si el celular tiene valor en la BD, entonces sí debe coincidir
                var query = @"
                    SELECT TOP 1 p.id, p.e_mail, p.celular, p.nro_documento
                    FROM col.persona AS p
                    WHERE LTRIM(RTRIM(LOWER(p.e_mail))) = LTRIM(RTRIM(LOWER(@Email)))
                      AND LTRIM(RTRIM(p.nro_documento)) = LTRIM(RTRIM(@NroDocumento))
                      AND (
                          LTRIM(RTRIM(ISNULL(p.celular, ''))) = '' 
                          OR LTRIM(RTRIM(p.celular)) = LTRIM(RTRIM(@Celular))
                      )";

                var parameters = new Dictionary<string, object>
                {
                    { "@Email", email },
                    { "@Celular", celular },
                    { "@NroDocumento", nroDocumento }
                };

                var result = await _databaseService.ExecuteQueryAsync(query, parameters);
                
                Console.WriteLine($"🔍 Resultado de validación: {(result.Rows.Count > 0 ? "Usuario encontrado" : "Usuario NO encontrado")}");
                
                if (result.Rows.Count > 0)
                {
                    var row = result.Rows[0];
                    Console.WriteLine($"   Usuario ID: {row["id"]}");
                    Console.WriteLine($"   Email en BD: '{row["e_mail"]}'");
                    Console.WriteLine($"   Celular en BD: '{row["celular"]}'");
                    Console.WriteLine($"   NroDocumento en BD: '{row["nro_documento"]}'");
                }
                else
                {
                    // Consulta de diagnóstico: buscar usuarios que coincidan parcialmente
                    var queryDiagnostico = @"
                        SELECT TOP 5 p.id, p.e_mail, p.celular, p.nro_documento
                        FROM col.persona AS p
                        WHERE LTRIM(RTRIM(LOWER(p.e_mail))) LIKE '%' + LTRIM(RTRIM(LOWER(@Email))) + '%'
                           OR LTRIM(RTRIM(p.celular)) LIKE '%' + LTRIM(RTRIM(@Celular)) + '%'
                           OR LTRIM(RTRIM(p.nro_documento)) = LTRIM(RTRIM(@NroDocumento))
                        ORDER BY 
                            CASE 
                                WHEN LTRIM(RTRIM(LOWER(p.e_mail))) = LTRIM(RTRIM(LOWER(@Email))) THEN 1
                                WHEN LTRIM(RTRIM(p.celular)) = LTRIM(RTRIM(@Celular)) THEN 2
                                WHEN LTRIM(RTRIM(p.nro_documento)) = LTRIM(RTRIM(@NroDocumento)) THEN 3
                                ELSE 4
                            END";
                    
                    var resultDiagnostico = await _databaseService.ExecuteQueryAsync(queryDiagnostico, parameters);
                    Console.WriteLine($"🔍 Diagnóstico - Usuarios parcialmente coincidentes: {resultDiagnostico.Rows.Count}");
                    
                    foreach (System.Data.DataRow row in resultDiagnostico.Rows)
                    {
                        Console.WriteLine($"   - ID: {row["id"]}, Email: '{row["e_mail"]}', Celular: '{row["celular"]}', Doc: '{row["nro_documento"]}'");
                    }
                    
                    // Intentar buscar con comparación exacta pero sin case-sensitive para email
                    var queryAlternativa = @"
                        SELECT TOP 1 p.id, p.e_mail, p.celular, p.nro_documento
                        FROM col.persona AS p
                        WHERE LTRIM(RTRIM(p.e_mail)) = LTRIM(RTRIM(@Email))
                          AND LTRIM(RTRIM(p.celular)) = LTRIM(RTRIM(@Celular))
                          AND LTRIM(RTRIM(p.nro_documento)) = LTRIM(RTRIM(@NroDocumento))";
                    
                    var resultAlternativa = await _databaseService.ExecuteQueryAsync(queryAlternativa, parameters);
                    Console.WriteLine($"🔍 Resultado con consulta alternativa: {(resultAlternativa.Rows.Count > 0 ? "Usuario encontrado" : "Usuario NO encontrado")}");
                    
                    if (resultAlternativa.Rows.Count > 0)
                    {
                        return true;
                    }
                    
                    // Último intento: buscar solo por número de documento (más común que coincida)
                    var queryPorDocumento = @"
                        SELECT TOP 1 p.id, p.e_mail, p.celular, p.nro_documento
                        FROM col.persona AS p
                        WHERE LTRIM(RTRIM(p.nro_documento)) = LTRIM(RTRIM(@NroDocumento))";
                    
                    var parametersDoc = new Dictionary<string, object>
                    {
                        { "@NroDocumento", nroDocumento }
                    };
                    
                    var resultPorDocumento = await _databaseService.ExecuteQueryAsync(queryPorDocumento, parametersDoc);
                    Console.WriteLine($"🔍 Búsqueda solo por documento: {(resultPorDocumento.Rows.Count > 0 ? "Usuario encontrado" : "Usuario NO encontrado")}");
                    
                    if (resultPorDocumento.Rows.Count > 0)
                    {
                        var row = resultPorDocumento.Rows[0];
                        Console.WriteLine($"   Email en BD: '{row["e_mail"]}' vs ingresado: '{email}'");
                        Console.WriteLine($"   Celular en BD: '{row["celular"]}' vs ingresado: '{celular}'");
                        Console.WriteLine($"   NroDocumento en BD: '{row["nro_documento"]}' vs ingresado: '{nroDocumento}'");
                    }
                }
                
                return result.Rows.Count > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error al validar información de recuperación: {ex.Message}");
                Console.WriteLine($"❌ Stack trace: {ex.StackTrace}");
                return false;
            }
        }

        /// <summary>
        /// Cambia la contraseña del usuario.
        /// Hashea la contraseña con BCrypt y la almacena en col.persona_password
        /// </summary>
        public async Task<bool> CambiarContrasena(CambiarContrasenaRequest request)
        {
            try
            {
                // Primero validar que la información del usuario es correcta
                var validacionRequest = new ValidarInformacionRecuperacionRequest
                {
                    Email = request.Email,
                    Celular = request.Celular,
                    NroDocumento = request.NroDocumento
                };

                if (!await ValidarInformacionRecuperacion(validacionRequest))
                {
                    Console.WriteLine("❌ Información de usuario no válida para cambiar contraseña");
                    return false;
                }

                // Limpiar y normalizar los datos de entrada
                var email = (request.Email ?? string.Empty).Trim();
                var celular = (request.Celular ?? string.Empty).Trim();
                var nroDocumento = (request.NroDocumento ?? string.Empty).Trim();

                // Obtener el ID del usuario usando la misma lógica robusta que la validación
                // El celular es opcional: si está vacío/NULL en la BD, no se valida el celular ingresado
                var queryUsuario = @"
                    SELECT TOP 1 p.id
                    FROM col.persona AS p
                    WHERE LTRIM(RTRIM(LOWER(p.e_mail))) = LTRIM(RTRIM(LOWER(@Email)))
                      AND LTRIM(RTRIM(p.nro_documento)) = LTRIM(RTRIM(@NroDocumento))
                      AND (
                          LTRIM(RTRIM(ISNULL(p.celular, ''))) = '' 
                          OR LTRIM(RTRIM(p.celular)) = LTRIM(RTRIM(@Celular))
                      )";

                var parametersUsuario = new Dictionary<string, object>
                {
                    { "@Email", email },
                    { "@Celular", celular },
                    { "@NroDocumento", nroDocumento }
                };

                var resultUsuario = await _databaseService.ExecuteQueryAsync(queryUsuario, parametersUsuario);
                
                if (resultUsuario.Rows.Count == 0)
                {
                    Console.WriteLine("❌ Usuario no encontrado");
                    return false;
                }

                var usuarioId = Convert.ToInt32(resultUsuario.Rows[0]["id"]);

                // Hashear la contraseña con BCrypt
                var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.NuevaContrasena);

                Console.WriteLine($"🔐 Cambiando contraseña para usuario ID: {usuarioId}");
                Console.WriteLine($"🔐 Hash generado (longitud): {passwordHash?.Length ?? 0}");
                Console.WriteLine($"🔐 Hash (primeros 20 chars): {(passwordHash?.Length > 20 ? passwordHash.Substring(0, 20) + "..." : passwordHash)}");

                // Verificar si ya existe un registro en col.persona_password
                var queryVerificar = @"
                    SELECT TOP 1 id_persona
                    FROM col.persona_password
                    WHERE id_persona = @UsuarioId";

                var parametersVerificar = new Dictionary<string, object>
                {
                    { "@UsuarioId", usuarioId }
                };

                var existePassword = await _databaseService.ExecuteQueryAsync(queryVerificar, parametersVerificar);

                if (existePassword.Rows.Count > 0)
                {
                    // Actualizar contraseña existente
                    // La tabla tiene: id, id_persona, password
                    // Especificar explícitamente el tipo de dato para evitar truncamiento
                    var queryUpdate = @"
                        UPDATE col.persona_password
                        SET password = CAST(@PasswordHash AS NVARCHAR(MAX))
                        WHERE id_persona = @UsuarioId";

                    var parametersUpdate = new Dictionary<string, object>
                    {
                        { "@PasswordHash", passwordHash ?? string.Empty },
                        { "@UsuarioId", usuarioId }
                    };

                    var rowsAffected = await _databaseService.ExecuteNonQueryAsync(queryUpdate, parametersUpdate);
                    Console.WriteLine($"✅ Contraseña actualizada exitosamente. Filas afectadas: {rowsAffected}");
                    
                    // Verificar que se guardó correctamente leyendo el hash de vuelta
                    var queryVerificarGuardado = @"
                        SELECT TOP 1 password
                        FROM col.persona_password
                        WHERE id_persona = @UsuarioId";
                    
                    var hashVerificado = await _databaseService.ExecuteQueryAsync(queryVerificarGuardado, parametersVerificar);
                    if (hashVerificado.Rows.Count > 0)
                    {
                        var hashGuardado = hashVerificado.Rows[0]["password"]?.ToString()?.Trim();
                        Console.WriteLine($"🔍 Hash guardado (longitud): {hashGuardado?.Length ?? 0}");
                        Console.WriteLine($"🔍 Hash guardado coincide con generado: {hashGuardado == passwordHash}");
                    }
                }
                else
                {
                    // Insertar nueva contraseña
                    // La tabla tiene: id, id_persona, password
                    // Especificar explícitamente el tipo de dato para evitar truncamiento
                    var queryInsert = @"
                        INSERT INTO col.persona_password (id_persona, password)
                        VALUES (@UsuarioId, CAST(@PasswordHash AS NVARCHAR(MAX)))";

                    var parametersInsert = new Dictionary<string, object>
                    {
                        { "@UsuarioId", usuarioId },
                        { "@PasswordHash", passwordHash ?? string.Empty }
                    };

                    var rowsAffected = await _databaseService.ExecuteNonQueryAsync(queryInsert, parametersInsert);
                    Console.WriteLine($"✅ Contraseña creada exitosamente. Filas afectadas: {rowsAffected}");
                    
                    // Verificar que se guardó correctamente leyendo el hash de vuelta
                    var queryVerificarGuardado = @"
                        SELECT TOP 1 password
                        FROM col.persona_password
                        WHERE id_persona = @UsuarioId";
                    
                    var hashVerificado = await _databaseService.ExecuteQueryAsync(queryVerificarGuardado, parametersVerificar);
                    if (hashVerificado.Rows.Count > 0)
                    {
                        var hashGuardado = hashVerificado.Rows[0]["password"]?.ToString()?.Trim();
                        Console.WriteLine($"🔍 Hash guardado (longitud): {hashGuardado?.Length ?? 0}");
                        Console.WriteLine($"🔍 Hash guardado coincide con generado: {hashGuardado == passwordHash}");
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error al cambiar contraseña: {ex.Message}");
                Console.WriteLine($"❌ Stack trace: {ex.StackTrace}");
                return false;
            }
        }
    }
}
