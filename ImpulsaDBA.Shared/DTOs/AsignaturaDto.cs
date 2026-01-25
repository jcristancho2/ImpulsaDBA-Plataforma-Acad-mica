namespace ImpulsaDBA.Shared.DTOs;

public class AsignaturaDto
{
    public int Id { get; set; }
    public int IdAsignacionAcademica { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Institucion { get; set; } = string.Empty;
    public int GradoId { get; set; }
    public string Grado { get; set; } = string.Empty;
    public int GrupoId { get; set; }
    public string Grupo { get; set; } = string.Empty;
    public string ColorHex { get; set; } = string.Empty;
    public int ProfesorId { get; set; }
    public string ProfesorNombre { get; set; } = string.Empty;
    public int CantidadEstudiantes { get; set; }
    public EstadisticasActividadesDto? Estadisticas { get; set; }
}
