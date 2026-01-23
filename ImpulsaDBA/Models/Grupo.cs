namespace ImpulsaDBA.Models
{
    public class Grupo
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public int CantidadEstudiantes { get; set; }
        public int GradoId { get; set; }
        public string Grado { get; set; } = string.Empty;
        public int SedeId { get; set; }
        public string Sede { get; set; } = string.Empty;
        public int JornadaId { get; set; }
        public string Jornada { get; set; } = string.Empty;
    }
}
