namespace ClinicaSaaS.Domain.Scheduling.Events;

/// <summary>
/// Se dispara al completar una cita. Justificación: es el disparador natural para que la
/// Application layer sugiera/abra el registro de una nueva HistorialClinicoEntrada o inicie
/// el flujo de facturación del procedimiento — sin acoplar el agregado Cita a esos otros
/// bounded contexts (Clinical, Billing).
/// </summary>
public sealed record CitaCompletadaDomainEvent(Guid CitaId, Guid PacienteId, Guid DoctorId, Guid ClinicaId, DateTime FechaUtc);
