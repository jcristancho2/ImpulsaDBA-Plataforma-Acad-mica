namespace ImpulsaDBA.Shared.DTOs;

/// <summary>
/// Actividad listada para la pantalla de duplicar (misma asignatura del docente).
/// </summary>
public class ActividadParaDuplicarDto
{
    /// <summary>ID de tab.asignacion_academica_recurso.</summary>
    public int Id { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public DateTime? FechaPublicacion { get; set; }
    public int IdAsignacionAcademica { get; set; }
    public int IdGrado { get; set; }
    public string NombreGrupo { get; set; } = string.Empty;
    public string NombreAsignatura { get; set; } = string.Empty;
}
