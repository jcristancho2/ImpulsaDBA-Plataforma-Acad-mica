using Microsoft.AspNetCore.Mvc;
using ImpulsaDBA.API.Application.Services;

namespace ImpulsaDBA.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AsignaturasController : ControllerBase
    {
        private readonly ImpulsaDBA.API.Application.Services.AsignaturaService _asignaturaService;

        public AsignaturasController(ImpulsaDBA.API.Application.Services.AsignaturaService asignaturaService)
        {
            _asignaturaService = asignaturaService;
        }

        [HttpGet("profesor/{profesorId}")]
        public async Task<IActionResult> ObtenerAsignaturasPorProfesor(int profesorId)
        {
            try
            {
                var asignaturas = await _asignaturaService.ObtenerAsignaturasPorProfesor(profesorId);
                return Ok(asignaturas);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Error al obtener asignaturas", mensaje = ex.Message });
            }
        }

        [HttpGet("grupo/{grupoId}/estudiantes")]
        public async Task<IActionResult> ObtenerCantidadEstudiantes(int grupoId)
        {
            try
            {
                var cantidad = await _asignaturaService.ObtenerCantidadEstudiantesPorGrupo(grupoId);
                return Ok(new { cantidad });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Error al obtener cantidad de estudiantes", mensaje = ex.Message });
            }
        }

        [HttpGet("asignacion/{idAsignacionAcademica}/estadisticas")]
        public async Task<IActionResult> ObtenerEstadisticasActividades(int idAsignacionAcademica)
        {
            try
            {
                var estadisticas = await _asignaturaService.ObtenerEstadisticasActividades(idAsignacionAcademica);
                return Ok(estadisticas);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Error al obtener estadísticas", mensaje = ex.Message });
            }
        }

        [HttpGet("periodo-actual")]
        public async Task<IActionResult> ObtenerPeriodoActual()
        {
            try
            {
                var periodo = await _asignaturaService.ObtenerPeriodoActual();
                return Ok(new 
                { 
                    periodoId = periodo.PeriodoId, 
                    periodo = periodo.Periodo,
                    anioId = periodo.AnioId,
                    anio = periodo.Anio
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Error al obtener periodo actual", mensaje = ex.Message });
            }
        }

        [HttpGet("colegio/profesor/{profesorId}")]
        public async Task<IActionResult> ObtenerColegioIdPorProfesor(int profesorId)
        {
            try
            {
                var colegioId = await _asignaturaService.ObtenerColegioIdPorProfesor(profesorId);
                return Ok(new { colegioId });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Error al obtener colegio", mensaje = ex.Message });
            }
        }

        [HttpGet("grupo/{grupoId}/primera-asignacion")]
        public async Task<IActionResult> ObtenerPrimeraAsignacionAcademicaPorGrupo(int grupoId)
        {
            try
            {
                var idAsignacion = await _asignaturaService.ObtenerPrimeraAsignacionAcademicaPorGrupo(grupoId);
                return Ok(new { idAsignacionAcademica = idAsignacion });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Error al obtener asignación académica del grupo", mensaje = ex.Message });
            }
        }
    }
}
