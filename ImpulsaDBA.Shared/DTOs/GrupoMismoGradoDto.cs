namespace ImpulsaDBA.Shared.DTOs;

/// <summary>
/// Grupo del mismo grado que ve la misma asignatura (para duplicar actividad).
/// </summary>
public class GrupoMismoGradoDto
{
    public int IdAsignacionAcademica { get; set; }
    public int IdGrupo { get; set; }
    public string NombreGrupo { get; set; } = string.Empty;
}
