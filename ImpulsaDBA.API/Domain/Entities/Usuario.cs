namespace ImpulsaDBA.API.Domain.Entities;

/// <summary>
/// Entidad de dominio Usuario. Representa un usuario en el sistema.
/// </summary>
public class Usuario
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
