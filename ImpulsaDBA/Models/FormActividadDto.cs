using System.ComponentModel.DataAnnotations;

namespace ImpulsaDBA.Models
{
    public class FormActividadDto
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
    }
}
