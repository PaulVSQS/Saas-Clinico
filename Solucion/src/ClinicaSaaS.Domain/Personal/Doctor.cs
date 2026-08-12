using ClinicaSaaS.Domain.Personal.Entities;
using ClinicaSaaS.Domain.Personal.Enums;
using ClinicaSaaS.Domain.Personal.ValueObjects;
using ClinicaSaaS.SharedKernel.Abstractions;
using ClinicaSaaS.SharedKernel.Common;
using ClinicaSaaS.SharedKernel.Results;

namespace ClinicaSaaS.Domain.Personal;

/// <summary>
/// Aggregate Root. Perfil profesional del doctor DENTRO de una clínica (un mismo UsuarioId
/// puede tener un Doctor distinto por cada clínica donde trabaja — ver Usuario). Contiene sus
/// HorariosDoctor como Entities internas porque el horario NUNCA existe fuera del contexto de
/// "el horario de este doctor" y las invariantes de solapamiento solo pueden garantizarse si
/// todos los bloques del mismo doctor se modifican dentro de una sola transacción de agregado.
/// </summary>
public sealed class Doctor : SoftDeleteEntity, ITenantEntity
{
    private readonly List<HorarioDoctor> _horarios = [];

    public Guid UsuarioId { get; private set; }
    public Guid ClinicaId { get; private set; }
    public string Especialidad { get; private set; }
    public string? NumeroExequatur { get; private set; }
    public bool Activo { get; private set; }

    public IReadOnlyCollection<HorarioDoctor> Horarios => _horarios.AsReadOnly();

    private Doctor() { } // EF Core

    private Doctor(Guid id, Guid usuarioId, Guid clinicaId, string especialidad) : base(id)
    {
        UsuarioId = usuarioId;
        ClinicaId = clinicaId;
        Especialidad = especialidad;
        Activo = true;
    }

    public static Result<Doctor> Registrar(Guid id, Guid usuarioId, Guid clinicaId, string especialidad, string? numeroExequatur)
    {
        Guard.ContraGuidVacio(id, nameof(id));
        Guard.ContraGuidVacio(usuarioId, nameof(usuarioId));
        Guard.ContraGuidVacio(clinicaId, nameof(clinicaId));

        if (string.IsNullOrWhiteSpace(especialidad))
            return new Error("Doctor.EspecialidadRequerida", "La especialidad es requerida.");

        return new Doctor(id, usuarioId, clinicaId, especialidad.Trim()) { NumeroExequatur = numeroExequatur?.Trim() };
    }

    public Result ActualizarDatos(string especialidad, string? numeroExequatur)
    {
        if (string.IsNullOrWhiteSpace(especialidad))
            return new Error("Doctor.EspecialidadRequerida", "La especialidad es requerida.");

        Especialidad = especialidad.Trim();
        NumeroExequatur = numeroExequatur?.Trim();
        return Result.Exitoso();
    }

    /// <summary>
    /// Agrega un bloque de horario validando que no se solape con ninguno de los bloques
    /// activos existentes de este mismo doctor. La validación contra citas/horarios de OTROS
    /// doctores no aplica aquí (no es invariante de este agregado) — eso corresponde al
    /// dominio Scheduling.Cita a través de IValidadorDisponibilidadDoctor.
    /// </summary>
    public Result<HorarioDoctor> AgregarHorario(
        Guid idNuevoHorario, Guid? consultorioId, TipoHorario tipoHorario, DayOfWeek? diaSemana,
        BloqueHorario bloque, PeriodoVigencia vigencia)
    {
        if (tipoHorario != TipoHorario.PorRango && diaSemana is null)
            return new Error("HorarioDoctor.DiaSemanaRequerido", "El día de la semana es requerido salvo en horarios PorRango.");

        var nuevo = new HorarioDoctor(idNuevoHorario, consultorioId, tipoHorario, diaSemana, bloque, vigencia);

        if (_horarios.Any(h => h.SeSolapaCon(nuevo)))
            return new Error("HorarioDoctor.Solapado", "El nuevo bloque se solapa con un horario existente del doctor.");

        _horarios.Add(nuevo);
        return nuevo;
    }

    public Result DesactivarHorario(Guid horarioId)
    {
        var horario = _horarios.FirstOrDefault(h => h.Id == horarioId);
        if (horario is null)
            return new Error("HorarioDoctor.NoEncontrado", "El bloque de horario no pertenece a este doctor.");

        horario.Desactivar();
        return Result.Exitoso();
    }

    public Result Desactivar()
    {
        if (!Activo)
            return new Error("Doctor.YaInactivo", "El doctor ya está inactivo.");

        Activo = false;
        return Result.Exitoso();
    }

    public Result Reactivar()
    {
        if (Activo)
            return new Error("Doctor.YaActivo", "El doctor ya está activo.");

        Activo = true;
        return Result.Exitoso();
    }

    public new Result Eliminar(Guid usuarioId, DateTime fechaUtc)
    {
        if (Activo)
            return new Error("Doctor.DebeDesactivarsePrimero", "Un doctor activo no puede eliminarse; desactívelo primero.");

        base.Eliminar(usuarioId, fechaUtc);
        return Result.Exitoso();
    }
}
