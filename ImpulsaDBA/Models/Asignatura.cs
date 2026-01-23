namespace ImpulsaDBA.Models
{
    public class Asignatura
    {
        public int Id { get; set; }
        public int IdAsignacionAcademica { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Institucion { get; set; } = string.Empty;
        public int GradoId { get; set; }
        public string Grado { get; set; } = string.Empty;
        public int GrupoId { get; set; }
        public string Grupo { get; set; } = string.Empty;
        public string ColorHex { get; set; } = string.Empty;
        public int ProfesorId { get; set; }
        public string ProfesorNombre { get; set; } = string.Empty;
        public int CantidadEstudiantes { get; set; }
        public EstadisticasActividades? Estadisticas { get; set; }
    }
}
