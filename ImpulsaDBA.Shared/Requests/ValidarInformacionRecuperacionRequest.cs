using System.ComponentModel.DataAnnotations;

namespace ImpulsaDBA.Shared.Requests;

public class ValidarInformacionRecuperacionRequest
{
    [Required(ErrorMessage = "El correo electrónico es requerido")]
    public string Email { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "El número de celular es requerido")]
    public string Celular { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "El número de documento es requerido")]
    public string NroDocumento { get; set; } = string.Empty;
}
