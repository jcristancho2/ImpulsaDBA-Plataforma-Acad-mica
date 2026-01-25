namespace ImpulsaDBA.Shared.Responses;

public class LoginResponse
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public SesionUsuarioResponse? Usuario { get; set; }
    public string? Token { get; set; }
}

public class SesionUsuarioResponse
{
    public int UsuarioId { get; set; }
    public string? NombreCompleto { get; set; }
    public string? Email { get; set; }
    public string? Perfil { get; set; }
    public string? FotoUrl { get; set; }
    public string? Token { get; set; }
}
