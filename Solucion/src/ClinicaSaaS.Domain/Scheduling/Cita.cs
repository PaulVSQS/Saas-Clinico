using ClinicaSaaS.Domain.Scheduling.Enums;
using ClinicaSaaS.Domain.Scheduling.Events;
using ClinicaSaaS.SharedKernel.Abstractions;
using ClinicaSaaS.SharedKernel.Common;
using ClinicaSaaS.SharedKernel.Results;

namespace ClinicaSaaS.Domain.Scheduling;

/// <summary>
/// Aggregate Root. Referencia a Paciente, Doctor y Consultorio solo por Id (regla DDD estándar:
/// ningún agregado navega objetos completos de otro agregado). Hace cumplir la máquina de
/// estados del ciclo de vida de una cita — las transiciones inválidas (ej. completar una cita
/// ya cancelada) se rechazan aquí, no confiando en que la UI nunca ofrezca el botón equivocado.
/// La validación de "el doctor no tiene otra cita en este horario" NO vive en este agregado:
/// requiere consultar otras Citas fuera de este límite de agregado, por eso se delega a
/// IValidadorDisponibilidadDoctor (Domain Service) desde la capa de Application antes de invocar
/// el factory Programar.
/// </summary>
public sealed class Cita : SoftDeleteEntity, ITenantEntity
{
    private static readonly IReadOnlyDictionary<EstadoCita, EstadoCita[]> TransicionesValidas = new Dictionary<EstadoCita, EstadoCita[]>
    {
        [EstadoCita.Programada] = [EstadoCita.Confirmada, EstadoCita.Cancelada, EstadoCita.NoAsistio],
        [EstadoCita.Confirmada] = [EstadoCita.EnProceso, EstadoCita.Cancelada, EstadoCita.NoAsistio],
        [EstadoCita.EnProceso] = [EstadoCita.Completada, EstadoCita.Cancelada],
        [EstadoCita.Completada] = [],
        [EstadoCita.Cancelada] = [],
        [EstadoCita.NoAsistio] = []
    };

    public Guid ClinicaId { get; private set; }
    public Guid PacienteId { get; private set; }
    public Guid DoctorId { get; private set; }
    public Guid? ConsultorioId { get; private set; }
    public DateTime FechaHoraInicio { get; private set; }
    public DateTime FechaHoraFin { get; private set; }
    public EstadoCita Estado { get; private set; }
    public string? MotivoConsulta { get; private set; }
    public Guid CreadoPorUsuarioId { get; private set; }

    private Cita() { } // EF Core

    private Cita(
        Guid id, Guid clinicaId, Guid pacienteId, Guid doctorId, Guid? consultorioId,
        DateTime fechaHoraInicio, DateTime fechaHoraFin, string? motivoConsulta, Guid creadoPorUsuarioId) : base(id)
    {
        ClinicaId = clinicaId;
        PacienteId = pacienteId;
        DoctorId = doctorId;
        ConsultorioId = consultorioId;
        FechaHoraInicio = fechaHoraInicio;
        FechaHoraFin = fechaHoraFin;
        MotivoConsulta = motivoConsulta;
        CreadoPorUsuarioId = creadoPorUsuarioId;
        Estado = EstadoCita.Programada;
    }

    /// <summary>
    /// La disponibilidad del doctor ya debe haberse verificado (IValidadorDisponibilidadDoctor)
    /// ANTES de llamar a este factory — el agregado solo valida sus propias invariantes internas
    /// (fechas coherentes), no invariantes que requieren consultar otros agregados.
    /// </summary>
    public static Result<Cita> Programar(
        Guid id, Guid clinicaId, Guid pacienteId, Guid doctorId, Guid? consultorioId,
        DateTime fechaHoraInicio, DateTime fechaHoraFin, string? motivoConsulta, Guid creadoPorUsuarioId)
    {
        Guard.ContraGuidVacio(id, nameof(id));
        Guard.ContraGuidVacio(clinicaId, nameof(clinicaId));
        Guard.ContraGuidVacio(pacienteId, nameof(pacienteId));
        Guard.ContraGuidVacio(doctorId, nameof(doctorId));

        if (fechaHoraFin <= fechaHoraInicio)
            return new Error("Cita.RangoInvalido", "La hora de fin debe ser posterior a la hora de inicio.");

        return new Cita(id, clinicaId, pacienteId, doctorId, consultorioId, fechaHoraInicio, fechaHoraFin, motivoConsulta, creadoPorUsuarioId);
    }

    public Result Confirmar() => TransicionarA(EstadoCita.Confirmada);

    public Result IniciarAtencion() => TransicionarA(EstadoCita.EnProceso);

    public Result Completar(DateTime fechaUtc)
    {
        var resultado = TransicionarA(EstadoCita.Completada);
        if (resultado.EsFallido) return resultado;

        RaiseEvent(new CitaCompletadaDomainEvent(Id, PacienteId, DoctorId, ClinicaId, fechaUtc));
        return Result.Exitoso();
    }

    public Result Cancelar() => TransicionarA(EstadoCita.Cancelada);

    public Result MarcarNoAsistio() => TransicionarA(EstadoCita.NoAsistio);

    public Result Reprogramar(DateTime nuevaFechaHoraInicio, DateTime nuevaFechaHoraFin)
    {
        if (Estado is not (EstadoCita.Programada or EstadoCita.Confirmada))
            return new Error("Cita.NoReprogramable", "Solo una cita programada o confirmada puede reprogramarse.");

        if (nuevaFechaHoraFin <= nuevaFechaHoraInicio)
            return new Error("Cita.RangoInvalido", "La hora de fin debe ser posterior a la hora de inicio.");

        FechaHoraInicio = nuevaFechaHoraInicio;
        FechaHoraFin = nuevaFechaHoraFin;
        Estado = EstadoCita.Programada;
        return Result.Exitoso();
    }

    private Result TransicionarA(EstadoCita nuevoEstado)
    {
        if (!TransicionesValidas[Estado].Contains(nuevoEstado))
            return new Error("Cita.TransicionInvalida", $"No se puede pasar una cita de '{Estado}' a '{nuevoEstado}'.");

        Estado = nuevoEstado;
        return Result.Exitoso();
    }

    public new Result Eliminar(Guid usuarioId, DateTime fechaUtc)
    {
        if (Estado is not (EstadoCita.Cancelada or EstadoCita.NoAsistio))
            return new Error("Cita.NoEliminable", "Solo una cita cancelada o marcada como no-asistió puede eliminarse.");

        base.Eliminar(usuarioId, fechaUtc);
        return Result.Exitoso();
    }
}
