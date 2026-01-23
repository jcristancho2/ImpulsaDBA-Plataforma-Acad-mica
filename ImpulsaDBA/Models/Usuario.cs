using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ImpulsaDBA.Models
{
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

    public class LoginRequest
    {
        public string? Usuario { get; set; } // Puede ser nro_documento, email o celular
        public string? Password { get; set; } // nro_documento
    }

    public class SesionUsuario
    {
        public int UsuarioId { get; set; }
        public string? NombreCompleto { get; set; }
        public string? Email { get; set; }
        public string? Perfil { get; set; }
        public string? FotoUrl { get; set; }
        public string? Token { get; set; }
    }
}
