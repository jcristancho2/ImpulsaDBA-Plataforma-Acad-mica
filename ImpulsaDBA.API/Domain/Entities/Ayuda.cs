using System.ComponentModel.DataAnnotations;

namespace ImpulsaDBA.API.Domain.Entities
{
    public class Ayuda
    {
        public int Id { get; set; }
        
        [Required(ErrorMessage = "El código de aplicación es requerido")]
        public string CodigoAplicacion { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "El nombre de ayuda es requerido")]
        public string NombreAyuda { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "La URL de ayuda es requerida")]
        [Url(ErrorMessage = "Debe ser una URL válida")]
        public string UrlAyuda { get; set; } = string.Empty;
    }
}
