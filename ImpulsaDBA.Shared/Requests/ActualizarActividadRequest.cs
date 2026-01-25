using System.ComponentModel.DataAnnotations;

namespace ImpulsaDBA.Shared.Requests;

public class ActualizarActividadRequest
{
    public int Id { get; set; } // ID de tab.asignacion_academica_recurso
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
    
    // Para Video de Enganche
    public List<VideoEngancheRequest>? Videos { get; set; }
    
    // Para Preguntas Problematizadoras
    public List<PreguntaProblematizadoraRequest>? Preguntas { get; set; }
    
    // Para archivos (MaterialApoyo, Asignaciones)
    public List<ArchivoRequest>? Archivos { get; set; }
    
    // Hipertexto (descripción enriquecida para Asignaciones)
    public string? Hipertexto { get; set; }
    
    // ID del usuario/profesor que está intentando actualizar (para validación)
    public int UsuarioId { get; set; }
}
