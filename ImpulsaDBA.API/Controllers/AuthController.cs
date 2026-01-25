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
