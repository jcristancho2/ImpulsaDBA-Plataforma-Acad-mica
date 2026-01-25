namespace ImpulsaDBA.Shared.DTOs;

public class ActividadCalendarioDto
{
    public int Id { get; set; }
    public int IdTipoActividad { get; set; }
    public string TipoActividad { get; set; } = string.Empty;
    public string? Titulo { get; set; }
    public DateTime FechaInicio { get; set; }
    public DateTime? FechaFinal { get; set; }
    public DateTime? FechaCreacion { get; set; }
    public int IdAsignacionAcademica { get; set; }
    public int IdPeriodo { get; set; }
    public int IdAnio { get; set; }
    public bool Eliminado { get; set; }
    public int? IdProfesor { get; set; }
    public string? NombreProfesor { get; set; }
    public string? NombreAsignatura { get; set; }
}
