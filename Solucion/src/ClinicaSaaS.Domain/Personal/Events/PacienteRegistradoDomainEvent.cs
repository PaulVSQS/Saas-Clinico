namespace ClinicaSaaS.Domain.Personal.Events;

/// <summary>
/// Se dispara al registrar un paciente nuevo. Justificación: la creación de un Paciente debe
/// disparar, en otros bounded contexts, la creación automática de su HistorialClinico
/// (agregado separado en Clinical) — sin este evento, la Application layer tendría que
/// "adivinar" que ambas operaciones van juntas en vez de reaccionar a un hecho de dominio.
/// </summary>
public sealed record PacienteRegistradoDomainEvent(Guid PacienteId, Guid ClinicaId, DateTime FechaUtc);
