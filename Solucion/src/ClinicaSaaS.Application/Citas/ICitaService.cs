using ClinicaSaaS.Domain.Scheduling.Enums;
using ClinicaSaaS.SharedKernel.Results;

namespace ClinicaSaaS.Application.Citas;

/// <summary>
/// Vista de una Cita hacia afuera de Application — join en memoria con Clínica, Paciente, Doctor
/// y Consultorio, mismo criterio que los módulos anteriores. Consultorio es el único opcional
/// (una cita puede agendarse sin consultorio fijo todavía, igual que un HorarioDoctor rotativo).
/// </summary>
public sealed record CitaDto(
    Guid Id,
    Guid ClinicaId,
    string NombreClinica,
    Guid PacienteId,
    string NombrePaciente,
    Guid DoctorId,
    string NombreDoctor,
    Guid? ConsultorioId,
    string? NombreConsultorio,
    DateTime FechaHoraInicio,
    DateTime FechaHoraFin,
    EstadoCita Estado,
    string? MotivoConsulta);

public sealed record ProgramarCitaRequest(
    Guid ClinicaId,
    Guid PacienteId,
    Guid DoctorId,
    Guid? ConsultorioId,
    DateTime FechaHoraInicio,
    DateTime FechaHoraFin,
    string? MotivoConsulta);

public sealed record ReprogramarCitaRequest(DateTime NuevaFechaHoraInicio, DateTime NuevaFechaHoraFin);

/// <summary>
/// Módulo 8 de Fase 6 — Citas: agenda un paciente con un doctor (y opcionalmente un consultorio)
/// en un bloque de tiempo, y hace cumplir el ciclo de vida completo que ya define el agregado
/// Cita (Programada → Confirmada → EnProceso → Completada, con Cancelada/NoAsistio como salidas
/// alternas — ver Domain.Scheduling.Cita.TransicionesValidas). Cada método de transición aquí es
/// un espejo directo de un método del agregado; este servicio no decide reglas de negocio nuevas,
/// solo orquesta: valida cruces entre agregados (disponibilidad del doctor, consultorio de la
/// clínica correcta) antes de invocar el dominio.
///
/// Mismo criterio de protección que los módulos anteriores: Policy "AdminClinica", con
/// aislamiento de tenant en escritura vía ITenantContext.
///
/// DELIBERADAMENTE FUERA de este módulo: validar la cita contra los bloques de HorarioDoctor
/// (Módulo 6) del doctor — IValidadorDisponibilidadDoctor solo protege contra choque con OTRA
/// cita, no contra agendar fuera del horario de trabajo declarado. Historia clínica, facturación
/// y archivos médicos que referencian una Cita son módulos aparte (9, 10, 12).
/// </summary>
public interface ICitaService
{
    /// <summary>Agenda de un doctor para un día específico — el flujo real de una clínica.</summary>
    Task<IReadOnlyList<CitaDto>> ListarPorDoctorYFechaAsync(Guid doctorId, DateOnly fecha, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CitaDto>> ListarPorPacienteAsync(Guid pacienteId, CancellationToken cancellationToken = default);

    Task<CitaDto?> ObtenerAsync(Guid citaId, CancellationToken cancellationToken = default);

    Task<Result<Guid>> ProgramarAsync(ProgramarCitaRequest request, CancellationToken cancellationToken = default);

    Task<Result> ConfirmarAsync(Guid citaId, CancellationToken cancellationToken = default);

    Task<Result> IniciarAtencionAsync(Guid citaId, CancellationToken cancellationToken = default);

    Task<Result> CompletarAsync(Guid citaId, CancellationToken cancellationToken = default);

    Task<Result> CancelarAsync(Guid citaId, CancellationToken cancellationToken = default);

    Task<Result> MarcarNoAsistioAsync(Guid citaId, CancellationToken cancellationToken = default);

    Task<Result> ReprogramarAsync(Guid citaId, ReprogramarCitaRequest request, CancellationToken cancellationToken = default);

    Task<Result> EliminarAsync(Guid citaId, CancellationToken cancellationToken = default);
}