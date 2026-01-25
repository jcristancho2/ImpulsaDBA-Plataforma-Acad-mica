namespace ImpulsaDBA.Shared.DTOs;

public class ActividadCompletaDto
{
    public int Id { get; set; } // ID de tab.asignacion_academica_recurso
    public string Titulo { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public DateTime? FechaPublicacion { get; set; }
    public bool ActividadActiva { get; set; }
    
    // Para Video de Enganche
    public List<VideoEngancheDto>? Videos { get; set; }
    
    // Para Preguntas Problematizadoras
    public List<PreguntaProblematizadoraDto>? Preguntas { get; set; }
    
    // Para archivos
    public List<ArchivoDto>? Archivos { get; set; }
    
    // Hipertexto (descripción enriquecida para Asignaciones)
    public string? Hipertexto { get; set; }
}

public class VideoEngancheDto
{
    public string Url { get; set; } = string.Empty;
    public int Orden { get; set; }
}

public class PreguntaProblematizadoraDto
{
    public string Enunciado { get; set; } = string.Empty;
    public int Orden { get; set; }
}

public class ArchivoDto
{
    public int Id { get; set; }
    public string NombreOriginal { get; set; } = string.Empty;
    public string NombreUnico { get; set; } = string.Empty;
    public int TipoArchivoId { get; set; }
    public string Ruta { get; set; } = string.Empty;
    public bool Renderizable { get; set; }
}
