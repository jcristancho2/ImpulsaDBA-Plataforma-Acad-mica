using Microsoft.AspNetCore.Mvc;
using ImpulsaDBA.API.Application.Services;
using ImpulsaDBA.Shared.Requests;
using ImpulsaDBA.Shared.Responses;
using ImpulsaDBA.Shared.DTOs;

namespace ImpulsaDBA.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly ImpulsaDBA.API.Application.Services.AuthService _authService;

        public AuthController(ImpulsaDBA.API.Application.Services.AuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("validar-usuario")]
        public async Task<IActionResult> ValidarUsuario([FromBody] ValidarUsuarioRequest request)
        {
            try
            {
                var usuario = await _authService.ValidarCredencialesUsuario(
                    request.Correo, 
                    request.Cedula, 
                    request.NumeroCelular);
                
                if (usuario == null)
                {
                    return NotFound(new { mensaje = "Usuario no encontrado" });
                }
                
                return Ok(usuario);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Error al validar usuario", mensaje = ex.Message });
            }
        }

        [HttpPost("validar-password")]
        public async Task<IActionResult> ValidarPassword([FromBody] ValidarPasswordRequest request)
        {
            try
            {
                var usuario = await _authService.ValidarCredencialesPassword(
                    request.Password, 
                    request.UsuarioId);
                
                if (usuario == null)
                {
                    return Unauthorized(new { mensaje = "Contraseña incorrecta" });
                }
                
                return Ok(usuario); // Retorna UsuarioDto
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Error al validar contraseña", mensaje = ex.Message });
            }
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            try
            {
                var response = await _authService.ValidarLoginCompleto(request.Usuario ?? string.Empty, request.Password ?? string.Empty);
                
                if (!response.Success)
                {
                    return Unauthorized(response);
                }
                
                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new LoginResponse 
                { 
                    Success = false, 
                    Message = $"Error al realizar login: {ex.Message}" 
                });
            }
        }

        [HttpPost("validar-informacion-recuperacion")]
        public async Task<IActionResult> ValidarInformacionRecuperacion([FromBody] ValidarInformacionRecuperacionRequest request)
        {
            try
            {
                if (request == null)
                    return BadRequest(new { error = "El request no puede ser nulo" });

                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var esValido = await _authService.ValidarInformacionRecuperacion(request);
                
                return Ok(new { esValido, mensaje = esValido ? "Información validada correctamente" : "La información proporcionada no coincide con ningún usuario" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Error al validar información", mensaje = ex.Message });
            }
        }

        [HttpPost("cambiar-contrasena")]
        public async Task<IActionResult> CambiarContrasena([FromBody] CambiarContrasenaRequest request)
        {
            try
            {
                if (request == null)
                    return BadRequest(new { error = "El request no puede ser nulo" });

                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                // Validar que las contraseñas coincidan
                if (request.NuevaContrasena != request.ConfirmarContrasena)
                {
                    return BadRequest(new { error = "Las contraseñas no coinciden" });
                }

                var resultado = await _authService.CambiarContrasena(request);
                
                if (resultado)
                {
                    return Ok(new { exito = true, mensaje = "Contraseña cambiada exitosamente" });
                }
                else
                {
                    return BadRequest(new { exito = false, mensaje = "No se pudo cambiar la contraseña. Verifique que la información proporcionada sea correcta." });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Error al cambiar contraseña", mensaje = ex.Message });
            }
        }
    }

    // Request classes locales (pueden moverse a Shared si se reutilizan)
    public class ValidarUsuarioRequest
    {
        public string? Correo { get; set; }
        public string? Cedula { get; set; }
        public string? NumeroCelular { get; set; }
    }

    public class ValidarPasswordRequest
    {
        public string Password { get; set; } = string.Empty;
        public int? UsuarioId { get; set; }
    }
}
