namespace ImpulsaDBA.Shared.DTOs;

public class UsuarioDto
{
    public int Id { get; set; }
    public string? NroDocumento { get; set; }
    public string? Email { get; set; }
    public string? Celular { get; set; }
    public string? NombreCompleto { get; set; }
    public string? Perfil { get; set; } // "Profesor", "Estudiante", "Padre"
    public string? FotoUrl { get; set; }
    public bool Activo { get; set; }
}
