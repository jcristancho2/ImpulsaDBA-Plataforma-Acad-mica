namespace ImpulsaDBA.Shared.DTOs;

public class ActividadDto
{
    public int Id { get; set; }
    public string Tipo { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
    public DateTime FechaCreacion { get; set; }
    public DateTime? FechaInicio { get; set; }
    public DateTime? FechaFinal { get; set; }
    public int AsignaturaId { get; set; }
    public int PeriodoId { get; set; }
}
