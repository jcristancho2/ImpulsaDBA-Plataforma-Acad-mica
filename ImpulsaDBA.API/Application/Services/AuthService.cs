using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using ImpulsaDBA.API.Domain.Entities;
using ImpulsaDBA.API.Infrastructure.Database;
using ImpulsaDBA.Shared.DTOs;
using ImpulsaDBA.Shared.Responses;

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
        /// - e_mail = @correo, o
        /// - nro_documento = @cedula, o
        /// - celular = @numeroCelular
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
                // Construir consulta SQL para buscar persona por correo, cédula o celular
                // La consulta se ejecuta contra la base de datos configurada en DatabaseService
                // Obtiene el rol activo del usuario desde seg.rol_persona
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
                    WHERE (p.e_mail = @Email OR p.nro_documento = @Cedula OR p.celular = @Celular)
                    ORDER BY rp.id_anio DESC";

                var parameters = new Dictionary<string, object>
                {
                    { "@Email", correo ?? string.Empty },
                    { "@Cedula", cedula ?? string.Empty },
                    { "@Celular", numeroCelular ?? string.Empty }
                };

                // Ejecutar consulta usando DatabaseService (conexión a SQL Server)
                var result = await _databaseService.ExecuteQueryAsync(query, parameters);

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
        /// Valida que la contraseña (número de documento) coincida con el usuario.
        /// 
        /// CONSULTA A BASE DE DATOS:
        /// Siempre valida que el password proporcionado coincida con el nro_documento del usuario.
        /// Ignora las contraseñas personalizadas almacenadas en col.persona_password.
        /// 
        /// La consulta se ejecuta usando DatabaseService que maneja la conexión a SQL Server.
        /// </summary>
        /// <param name="password">Número de documento del usuario (contraseña)</param>
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

                // Siempre validar que el password sea el nro_documento
                var queryDocumento = @"
                    SELECT TOP 1 p.nro_documento
                    FROM col.persona AS p
                    WHERE p.id = @UsuarioId";

                var parametersPassword = new Dictionary<string, object>
                {
                    { "@UsuarioId", usuarioId.Value }
                };

                var resultDocumento = await _databaseService.ExecuteQueryAsync(queryDocumento, parametersPassword);
                
                if (resultDocumento.Rows.Count > 0)
                {
                    var nroDocumento = resultDocumento.Rows[0]["nro_documento"]?.ToString();
                    // Validar que el password coincida con el número de documento
                    if (nroDocumento != password)
                    {
                        return null;
                    }
                }
                else
                {
                    return null;
                }

                // Si la contraseña es válida, obtener los datos completos del usuario
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
        /// 2. Valida que la contraseña coincida con el nro_documento (siempre usa número de documento como contraseña)
        /// 3. Obtiene el rol del usuario desde seg.rol_persona y seg.rol
        /// 
        /// Utiliza DatabaseService para todas las consultas a SQL Server.
        /// </summary>
        /// <param name="usuario">Correo, cédula o número de celular del usuario</param>
        /// <param name="password">Número de documento (siempre se usa como contraseña)</param>
        /// <returns>LoginResponse con el resultado de la validación</returns>
        public async Task<LoginResponse> ValidarLoginCompleto(string usuario, string password)
        {
            try
            {
                // Primero validar que el usuario existe (por correo, cédula o celular)
                var usuarioEncontrado = await ValidarCredencialesUsuario(usuario, usuario, usuario);
                
                if (usuarioEncontrado == null)
                {
                    return new LoginResponse
                    {
                        Success = false,
                        Message = "Usuario no encontrado"
                    };
                }

                // Luego validar que la contraseña coincida
                var passwordValida = await ValidarCredencialesPassword(password, usuarioEncontrado.Id);
                
                if (passwordValida == null || passwordValida.Id != usuarioEncontrado.Id)
                {
                    return new LoginResponse
                    {
                        Success = false,
                        Message = "Contraseña incorrecta"
                    };
                }

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
    }
}
