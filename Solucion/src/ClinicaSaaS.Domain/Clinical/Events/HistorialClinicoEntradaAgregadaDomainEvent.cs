namespace ClinicaSaaS.Domain.Clinical.Events;

/// <summary>
/// Se dispara cada vez que se agrega una nueva entrada versionada al historial. Justificación:
/// notificar a otros contextos (ej. enviar recordatorio al paciente, disparar reportería médica,
/// alertar signos vitales anómalos) sin acoplar el agregado HistorialClinico a esos flujos.
/// </summary>
public sealed record HistorialClinicoEntradaAgregadaDomainEvent(
    Guid HistorialClinicoId, Guid EntradaId, Guid PacienteId, Guid DoctorId, int Version, DateTime FechaUtc);
