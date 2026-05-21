namespace TurnosMedicos.Models;

public class Turno
{
    public int Id { get; set; }
    // ToDo: tiene sentido un turno sin PacienteId? debería ser Required
    public int? PacienteId { get; set; }
    public Paciente? Paciente { get; set; }
    public int MedicoId { get; set; }
    public Medico? Medico { get; set; }
    public DateTime FechaHora { get; set; }
    public EstadoTurno Estado { get; set; }
    public DateTime FechaCreacion { get; set; }
    public string Motivo { get; set; } = string.Empty;
}
