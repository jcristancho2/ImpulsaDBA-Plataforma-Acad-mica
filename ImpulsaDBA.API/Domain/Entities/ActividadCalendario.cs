namespace ImpulsaDBA.API.Domain.Entities
{
    public class ActividadCalendario
    {
        public int Id { get; set; }
        public int IdTipoActividad { get; set; }
        public string TipoActividad { get; set; } = string.Empty;
        public DateTime FechaInicio { get; set; }
        public DateTime? FechaFinal { get; set; }
        public int IdAsignacionAcademica { get; set; }
        public int IdPeriodo { get; set; }
        public int IdAnio { get; set; }
        public bool Eliminado { get; set; }
    }
}
