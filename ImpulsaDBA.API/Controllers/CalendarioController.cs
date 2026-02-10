using Microsoft.AspNetCore.Mvc;
using ImpulsaDBA.API.Application.Services;
using ImpulsaDBA.Shared.Requests;

namespace ImpulsaDBA.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CalendarioController : ControllerBase
    {
        private readonly ImpulsaDBA.API.Application.Services.CalendarioService _calendarioService;

        public CalendarioController(ImpulsaDBA.API.Application.Services.CalendarioService calendarioService)
        {
            _calendarioService = calendarioService;
        }

        [HttpGet("asignacion/{idAsignacionAcademica}/fecha")]
        public async Task<IActionResult> ObtenerActividadesPorFecha(
            int idAsignacionAcademica, 
            [FromQuery] DateTime fecha)
        {
            try
            {
                var actividades = await _calendarioService.ObtenerActividadesPorFecha(idAsignacionAcademica, fecha);
                return Ok(actividades);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Error al obtener actividades", mensaje = ex.Message });
            }
        }

        [HttpGet("asignacion/{idAsignacionAcademica}/mes")]
        public async Task<IActionResult> ObtenerActividadesPorMes(
            int idAsignacionAcademica, 
            [FromQuery] int año, 
            [FromQuery] int mes)
        {
            try
            {
                var actividades = await _calendarioService.ObtenerActividadesPorMes(idAsignacionAcademica, año, mes);
                return Ok(actividades);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Error al obtener actividades", mensaje = ex.Message });
            }
        }

        [HttpGet("tipos-actividades")]
        public async Task<IActionResult> ObtenerTiposActividades()
        {
            try
            {
                var tipos = await _calendarioService.ObtenerTiposActividades();
                return Ok(tipos);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Error al obtener tipos de actividades", mensaje = ex.Message });
            }
        }

        [HttpGet("dias-festivos/{año}")]
        public async Task<IActionResult> ObtenerDiasFestivos(int año)
        {
            try
            {
                var dias = await _calendarioService.ObtenerDiasFestivos(año);
                return Ok(dias);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Error al obtener días festivos", mensaje = ex.Message });
            }
        }

        [HttpGet("grupo/{grupoId}/fecha")]
        public async Task<IActionResult> ObtenerActividadesPorGrupoYFecha(
            int grupoId,
            [FromQuery] DateTime fecha)
        {
            try
            {
                var actividades = await _calendarioService.ObtenerActividadesPorGrupoYFecha(grupoId, fecha);
                return Ok(actividades);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Error al obtener actividades del grupo", mensaje = ex.Message });
            }
        }

        [HttpPost("crear")]
        public async Task<IActionResult> CrearActividad([FromBody] CrearActividadCompletaRequest request)
        {
            try
            {
                if (request == null)
                    return BadRequest(new { error = "El request no puede ser nulo" });

                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var idRecurso = await _calendarioService.CrearActividad(request);
                return Ok(new { id = idRecurso, mensaje = "Actividad creada exitosamente" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Error al crear la actividad", mensaje = ex.Message });
            }
        }

        [HttpPut("actualizar")]
        public async Task<IActionResult> ActualizarActividad([FromBody] ActualizarActividadRequest request)
        {
            try
            {
                if (request == null)
                    return BadRequest(new { error = "El request no puede ser nulo" });

                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var resultado = await _calendarioService.ActualizarActividad(request);
                return Ok(new { exito = resultado, mensaje = "Actividad actualizada exitosamente" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Error al actualizar la actividad", mensaje = ex.Message });
            }
        }

        [HttpGet("completa/{idAsignacionAcademicaRecurso}")]
        public async Task<IActionResult> ObtenerActividadCompleta(int idAsignacionAcademicaRecurso)
        {
            try
            {
                var actividad = await _calendarioService.ObtenerActividadCompleta(idAsignacionAcademicaRecurso);
                return Ok(actividad);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Error al obtener actividad completa", mensaje = ex.Message });
            }
        }

        /// <summary>
        /// Descarga un archivo guardado por id (tab.archivo).
        /// </summary>
        [HttpGet("archivo/{idArchivo}")]
        public async Task<IActionResult> DescargarArchivo(int idArchivo)
        {
            try
            {
                var (fullPath, contentType, fileName) = await _calendarioService.ObtenerArchivoParaDescargaAsync(idArchivo);
                if (string.IsNullOrEmpty(fullPath) || !System.IO.File.Exists(fullPath))
                    return NotFound(new { error = "Archivo no encontrado" });
                return PhysicalFile(fullPath, contentType, fileName);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Error al obtener archivo", mensaje = ex.Message });
            }
        }

        [HttpDelete("eliminar/{idAsignacionAcademicaRecurso}")]
        public async Task<IActionResult> EliminarActividad(int idAsignacionAcademicaRecurso, [FromQuery] int usuarioId)
        {
            try
            {
                var resultado = await _calendarioService.EliminarActividad(idAsignacionAcademicaRecurso, usuarioId);
                return Ok(new { exito = resultado, mensaje = "Actividad eliminada exitosamente" });
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new { error = "No autorizado", mensaje = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Error al eliminar la actividad", mensaje = ex.Message });
            }
        }

        /// <summary>
        /// Mueve una actividad a otra fecha (arrastrar y soltar en el calendario).
        /// </summary>
        [HttpPut("mover/{idAsignacionAcademicaRecurso}")]
        public async Task<IActionResult> MoverActividad(int idAsignacionAcademicaRecurso, [FromQuery] DateTime nuevaFecha)
        {
            try
            {
                await _calendarioService.MoverActividadAsync(idAsignacionAcademicaRecurso, nuevaFecha);
                return Ok(new { exito = true, mensaje = "Fecha actualizada." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Error al mover la actividad", mensaje = ex.Message });
            }
        }

        /// <summary>
        /// Lista actividades del docente por asignatura para la pantalla de duplicar.
        /// </summary>
        [HttpGet("duplicar/actividades")]
        public async Task<IActionResult> ObtenerActividadesParaDuplicar(
            [FromQuery] int profesorId,
            [FromQuery] int idAsignatura)
        {
            try
            {
                var list = await _calendarioService.ObtenerActividadesParaDuplicarAsync(profesorId, idAsignatura);
                return Ok(list);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Error al listar actividades", mensaje = ex.Message });
            }
        }

        /// <summary>
        /// Grupos del mismo grado que ven la misma asignatura (para elegir destinos al duplicar).
        /// </summary>
        [HttpGet("duplicar/grupos-mismo-grado")]
        public async Task<IActionResult> ObtenerGruposMismoGrado([FromQuery] int idAsignacionAcademica, [FromQuery] int profesorId)
        {
            try
            {
                var list = await _calendarioService.ObtenerGruposMismoGradoAsync(idAsignacionAcademica, profesorId);
                return Ok(list);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Error al listar grupos", mensaje = ex.Message });
            }
        }

        /// <summary>
        /// Duplica una actividad a los grupos seleccionados con fecha y hora por grupo.
        /// </summary>
        [HttpPost("duplicar")]
        public async Task<IActionResult> DuplicarActividad([FromBody] DuplicarActividadRequest request)
        {
            try
            {
                if (request == null)
                    return BadRequest(new { error = "El request no puede ser nulo" });
                var idRecurso = await _calendarioService.DuplicarActividadAsync(request);
                return Ok(new { id = idRecurso, mensaje = "Actividad duplicada correctamente." });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Error al duplicar la actividad", mensaje = ex.Message });
            }
        }
    }
}
