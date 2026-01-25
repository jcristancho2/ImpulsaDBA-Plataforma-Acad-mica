namespace ImpulsaDBA.Shared.Requests;

public class LoginRequest
{
    public string? Usuario { get; set; } // Puede ser nro_documento, email o celular
    public string? Password { get; set; } // nro_documento
}
