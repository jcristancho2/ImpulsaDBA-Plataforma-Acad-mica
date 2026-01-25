using Microsoft.AspNetCore.Mvc;
using ImpulsaDBA.API.Application.Services;
using ImpulsaDBA.API.Infrastructure.Repositories;

namespace ImpulsaDBA.API.Controllers
{

    [ApiController]
    [Route("api/[controller]")]
    public class UsuariosController : ControllerBase
    {
        private readonly ImpulsaDBA.API.Infrastructure.Repositories.UsuarioRepository _repo;

        public UsuariosController(ImpulsaDBA.API.Infrastructure.Repositories.UsuarioRepository repo)
        {
            _repo = repo;
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            try
            {
                var usuarios = await _repo.ObtenerUsuariosAsync();
                return Ok(usuarios);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Error al obtener usuarios", mensaje = ex.Message });
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> Get(int id)
        {
            try
            {
                var usuario = await _repo.ObtenerUsuarioPorIdAsync(id);
                if (usuario == null)
                {
                    return NotFound(new { mensaje = $"Usuario con ID {id} no encontrado" });
                }
                return Ok(usuario);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Error al obtener usuario", mensaje = ex.Message });
            }
        }
    }
}
