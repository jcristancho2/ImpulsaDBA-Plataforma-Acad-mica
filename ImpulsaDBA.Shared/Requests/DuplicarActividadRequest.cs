namespace ImpulsaDBA.Shared.Requests;

/// <summary>
/// Request para duplicar una actividad a varios grupos del mismo grado.
/// </summary>
public class DuplicarActividadRequest
{
    /// <summary>ID de tab.asignacion_academica_recurso de la actividad origen.</summary>
    public int IdAsignacionAcademicaRecursoOrigen { get; set; }

    /// <summary>Destinos: cada grupo con su fecha y hora asignadas. Solo se puede duplicar una vez por grupo.</summary>
    public List<DestinoDuplicadoDto> Destinos { get; set; } = new();
}

/// <summary>
/// Un grupo destino con fecha y hora para la actividad duplicada.
/// </summary>
public class DestinoDuplicadoDto
{
    public int IdAsignacionAcademica { get; set; }
    /// <summary>Fecha (solo día).</summary>
    public DateTime Fecha { get; set; }
    /// <summary>Hora (solo hora, minutos opcionales). Ej: "14:30".</summary>
    public string Hora { get; set; } = "00:00";
}
