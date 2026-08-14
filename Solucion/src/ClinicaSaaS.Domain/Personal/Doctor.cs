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

    /// <summary>
    /// Igual que <see cref="AgregarHorario"/> pero crea un bloque IDÉNTICO (mismo consultorio/
    /// hora/vigencia) para cada día en <paramref name="idsPorDia"/> — pensado para cargar una
    /// semana de un tirón en vez de una llamada por día. Nunca aplica a TipoHorario.PorRango
    /// (ese no lleva día de la semana, ver AgregarHorario con diaSemana: null).
    ///
    /// IMPORTANTE: cada HorarioDoctor del lote recibe su PROPIA instancia clonada de
    /// bloque/vigencia (nunca la misma referencia) — Bloque y Vigencia están mapeados como owned
    /// types (OwnsOne) en EF Core, que los rastrea por identidad de referencia; compartir la
    /// misma instancia entre dos HorarioDoctor hace que EF Core solo guarde los datos en uno de
    /// los dos y deje el otro con columnas NULL al insertar.
    ///
    /// TODO O NADA: si el bloque de CUALQUIER día se solapa con un horario existente del doctor
    /// (o con otro día del mismo lote, por si el llamador repitió un día), no se agrega NINGUNO
    /// — evita dejar el horario a medio cargar por un solo día conflictivo.
    /// </summary>
    public Result<IReadOnlyList<HorarioDoctor>> AgregarHorarioEnVariosDias(
        IReadOnlyDictionary<DayOfWeek, Guid> idsPorDia, Guid? consultorioId, TipoHorario tipoHorario,
        BloqueHorario bloque, PeriodoVigencia vigencia)
    {
        if (tipoHorario == TipoHorario.PorRango)
            return new Error("HorarioDoctor.DiaSemanaNoAplica", "Un horario PorRango no usa selección de días — use AgregarHorario con día nulo.");

        if (idsPorDia.Count == 0)
            return new Error("HorarioDoctor.DiasRequeridos", "Selecciona al menos un día de la semana.");

        var nuevos = new List<HorarioDoctor>();
        foreach (var (dia, id) in idsPorDia)
        {
            var nuevo = new HorarioDoctor(id, consultorioId, tipoHorario, dia, bloque.Clonar(), vigencia.Clonar());

            if (_horarios.Any(h => h.SeSolapaCon(nuevo)) || nuevos.Any(h => h.SeSolapaCon(nuevo)))
                return new Error("HorarioDoctor.Solapado", $"El bloque del {dia} se solapa con un horario existente del doctor.");

            nuevos.Add(nuevo);
        }

        _horarios.AddRange(nuevos);
        return nuevos;
    }

    /// <summary>
    /// Cambia los datos de un bloque existente, revalidando la invariante de solapamiento contra
    /// los DEMÁS bloques del doctor (excluyendo el que se está editando). No existe una forma de
    /// "editar solo un campo" — se reemplazan todos los datos editables juntos, igual que
    /// ActualizarDatos en Doctor/Empleado/Consultorio.
    /// </summary>
    public Result ActualizarHorario(
        Guid horarioId, Guid? consultorioId, TipoHorario tipoHorario, DayOfWeek? diaSemana,
        BloqueHorario bloque, PeriodoVigencia vigencia)
    {
        var horario = _horarios.FirstOrDefault(h => h.Id == horarioId);
        if (horario is null)
            return new Error("HorarioDoctor.NoEncontrado", "El bloque de horario no pertenece a este doctor.");

        if (tipoHorario != TipoHorario.PorRango && diaSemana is null)
            return new Error("HorarioDoctor.DiaSemanaRequerido", "El día de la semana es requerido salvo en horarios PorRango.");

        if (_horarios.Any(h => h.Id != horarioId && h.SeSolapaConPropuesta(diaSemana, bloque)))
            return new Error("HorarioDoctor.Solapado", "Los nuevos datos del bloque se solapan con otro horario existente del doctor.");

        horario.ActualizarDatos(consultorioId, tipoHorario, diaSemana, bloque, vigencia);
        return Result.Exitoso();
    }

    public Result DesactivarHorario(Guid horarioId)
    {
        var horario = _horarios.FirstOrDefault(h => h.Id == horarioId);
        if (horario is null)
            return new Error("HorarioDoctor.NoEncontrado", "El bloque de horario no pertenece a este doctor.");

        horario.Desactivar();
        return Result.Exitoso();
    }

    /// <summary>
    /// Revalida solapamiento antes de reactivar: mientras este bloque estuvo inactivo pudo
    /// haberse agregado otro que ahora chocaría con él — reactivar no debe saltarse la invariante
    /// que AgregarHorario/ActualizarHorario sí exigen.
    /// </summary>
    public Result ReactivarHorario(Guid horarioId)
    {
        var horario = _horarios.FirstOrDefault(h => h.Id == horarioId);
        if (horario is null)
            return new Error("HorarioDoctor.NoEncontrado", "El bloque de horario no pertenece a este doctor.");

        if (horario.Activo)
            return new Error("HorarioDoctor.YaActivo", "El bloque de horario ya está activo.");

        if (_horarios.Any(h => h.Id != horarioId && h.SeSolapaConPropuesta(horario.DiaSemana, horario.Bloque)))
            return new Error("HorarioDoctor.Solapado", "No se puede reactivar: se solapa con otro horario activo del doctor.");

        horario.Reactivar();
        return Result.Exitoso();
    }

    /// <summary>
    /// Elimina un bloque del agregado (DELETE físico en BD — HorarioDoctor no tiene soft-delete
    /// propio, ver nota en HorarioDoctorConfiguration de Fase 3). Exige que esté inactivo primero,
    /// mismo criterio que Doctor/Consultorio.Eliminar: nunca se borra algo que sigue en uso sin
    /// pasar antes por desactivarlo explícitamente.
    /// </summary>
    public Result EliminarHorario(Guid horarioId)
    {
        var horario = _horarios.FirstOrDefault(h => h.Id == horarioId);
        if (horario is null)
            return new Error("HorarioDoctor.NoEncontrado", "El bloque de horario no pertenece a este doctor.");

        if (horario.Activo)
            return new Error("HorarioDoctor.DebeDesactivarsePrimero", "Un bloque de horario activo no puede eliminarse; desactívelo primero.");

        _horarios.Remove(horario);
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
