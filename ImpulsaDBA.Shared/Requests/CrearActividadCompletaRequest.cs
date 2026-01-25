using System.ComponentModel.DataAnnotations;

namespace ImpulsaDBA.Shared.Requests;

public class CrearActividadCompletaRequest
{
    public bool ActividadActiva { get; set; } = true;
    
    [Required(ErrorMessage = "La fecha de publicación es requerida")]
    public DateTime? FechaPublicacion { get; set; }
    
    [Required(ErrorMessage = "El título es requerido")]
    [StringLength(255, ErrorMessage = "El título no puede exceder 255 caracteres")]
    public string Titulo { get; set; } = string.Empty;
    
    [StringLength(1000, ErrorMessage = "La descripción no puede exceder 1000 caracteres")]
    public string Descripcion { get; set; } = string.Empty;
    
    public string? Link { get; set; }
    public string? Tiempo { get; set; }
    
    public int TipoActividadId { get; set; }
    public int AsignacionAcademicaId { get; set; }
    public int PeriodoId { get; set; }
    public int AnioId { get; set; }
    
    // Para Video de Enganche
    public List<VideoEngancheRequest>? Videos { get; set; }
    
    // Para Preguntas Problematizadoras
    public List<PreguntaProblematizadoraRequest>? Preguntas { get; set; }
    
    // Para archivos (MaterialApoyo, Asignaciones)
    public List<ArchivoRequest>? Archivos { get; set; }
    
    // Hipertexto (descripción enriquecida para Asignaciones)
    public string? Hipertexto { get; set; }
}

public class VideoEngancheRequest
{
    public string Url { get; set; } = string.Empty;
    public int Orden { get; set; }
}

public class PreguntaProblematizadoraRequest
{
    public string Enunciado { get; set; } = string.Empty;
    public int Orden { get; set; }
}

public class ArchivoRequest
{
    public string NombreOriginal { get; set; } = string.Empty;
    public string NombreUnico { get; set; } = string.Empty;
    public int TipoArchivoId { get; set; }
    public string Ruta { get; set; } = string.Empty;
    public bool Renderizable { get; set; } = false;
    public byte[]? Datos { get; set; }
}
