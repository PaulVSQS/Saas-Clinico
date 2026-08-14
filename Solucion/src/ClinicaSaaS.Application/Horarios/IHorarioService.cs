using ClinicaSaaS.Domain.Personal.Enums;
using ClinicaSaaS.SharedKernel.Results;

namespace ClinicaSaaS.Application.Horarios;

/// <summary>
/// Vista de un bloque de horario hacia afuera de Application. Siempre se consulta en el contexto
/// de un doctor específico (ver IHorarioService), así que no incluye datos del doctor — la
/// página que lo use ya tiene el DoctorDto por separado.
/// </summary>
public sealed record HorarioDto(
    Guid Id,
    Guid DoctorId,
    Guid? ConsultorioId,
    string? NombreConsultorio,
    TipoHorario TipoHorario,
    DayOfWeek? DiaSemana,
    TimeOnly HoraInicio,
    TimeOnly HoraFin,
    DateOnly FechaInicioVigencia,
    DateOnly? FechaFinVigencia,
    bool Activo);

/// <summary>Para TipoHorario.PorRango (nunca lleva día) o para un único bloque de un solo día.</summary>
public sealed record AgregarHorarioRequest(
    Guid DoctorId,
    Guid? ConsultorioId,
    TipoHorario TipoHorario,
    DayOfWeek? DiaSemana,
    TimeOnly HoraInicio,
    TimeOnly HoraFin,
    DateOnly FechaInicioVigencia,
    DateOnly? FechaFinVigencia);

/// <summary>
/// Para Fijo/Rotativo con uno o varios días marcados — crea un bloque idéntico por cada día en
/// <paramref name="Dias"/>. Ver Doctor.AgregarHorarioEnVariosDias: todo o nada.
/// </summary>
public sealed record AgregarHorarioEnDiasRequest(
    Guid DoctorId,
    Guid? ConsultorioId,
    TipoHorario TipoHorario,
    IReadOnlyList<DayOfWeek> Dias,
    TimeOnly HoraInicio,
    TimeOnly HoraFin,
    DateOnly FechaInicioVigencia,
    DateOnly? FechaFinVigencia);

public sealed record ActualizarHorarioRequest(
    Guid? ConsultorioId,
    TipoHorario TipoHorario,
    DayOfWeek? DiaSemana,
    TimeOnly HoraInicio,
    TimeOnly HoraFin,
    DateOnly FechaInicioVigencia,
    DateOnly? FechaFinVigencia);

/// <summary>
/// Módulo 6 de Fase 6 — Horarios: bloques de disponibilidad de un doctor (día/hora, consultorio
/// opcional, vigencia). Este servicio NO opera sobre su propio Aggregate Root — HorarioDoctor es
/// una Entity interna del agregado Doctor, así que toda operación pasa por
/// IRepositorio&lt;Doctor&gt; y por los métodos de dominio de Doctor, que validan solapamiento
/// entre bloques del mismo doctor en cada escritura (alta, edición y reactivación).
///
/// DELIBERADAMENTE FUERA de este módulo: la validación de disponibilidad de un doctor contra
/// Citas (Scheduling.Cita) — eso corresponde al Módulo 8 (Citas), que consultará estos horarios
/// pero no los administra.
/// </summary>
public interface IHorarioService
{
    Task<IReadOnlyList<HorarioDto>> ListarPorDoctorAsync(Guid doctorId, CancellationToken cancellationToken = default);

    Task<Result<Guid>> AgregarAsync(AgregarHorarioRequest request, CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<Guid>>> AgregarEnDiasAsync(AgregarHorarioEnDiasRequest request, CancellationToken cancellationToken = default);

    Task<Result> ActualizarAsync(Guid doctorId, Guid horarioId, ActualizarHorarioRequest request, CancellationToken cancellationToken = default);

    Task<Result> DesactivarAsync(Guid doctorId, Guid horarioId, CancellationToken cancellationToken = default);

    Task<Result> ReactivarAsync(Guid doctorId, Guid horarioId, CancellationToken cancellationToken = default);

    Task<Result> EliminarAsync(Guid doctorId, Guid horarioId, CancellationToken cancellationToken = default);
}
