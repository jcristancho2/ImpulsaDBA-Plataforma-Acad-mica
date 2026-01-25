using Microsoft.AspNetCore.Mvc;
using ImpulsaDBA.API.Application.Services;

namespace ImpulsaDBA.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AyudasController : ControllerBase
    {
        private readonly ImpulsaDBA.API.Application.Services.AyudaService _ayudaService;

        public AyudasController(ImpulsaDBA.API.Application.Services.AyudaService ayudaService)
        {
            _ayudaService = ayudaService;
        }

        [HttpGet("componente/{idComponente}")]
        public async Task<IActionResult> ObtenerAyudasPorComponente(int idComponente)
        {
            try
            {
                var (pdf, video) = await _ayudaService.ObtenerAyudasPorComponente(idComponente);
                return Ok(new { pdf, video });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Error al obtener ayudas", mensaje = ex.Message });
            }
        }
    }
}
