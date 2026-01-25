using System.ComponentModel.DataAnnotations;

namespace ImpulsaDBA.Shared.Requests;

public class CambiarContrasenaRequest
{
    [Required(ErrorMessage = "El correo electrónico es requerido")]
    public string Email { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "El número de celular es requerido")]
    public string Celular { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "El número de documento es requerido")]
    public string NroDocumento { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "La nueva contraseña es requerida")]
    [MinLength(6, ErrorMessage = "La contraseña debe tener al menos 6 caracteres")]
    public string NuevaContrasena { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "La confirmación de contraseña es requerida")]
    [Compare("NuevaContrasena", ErrorMessage = "Las contraseñas no coinciden")]
    public string ConfirmarContrasena { get; set; } = string.Empty;
}
