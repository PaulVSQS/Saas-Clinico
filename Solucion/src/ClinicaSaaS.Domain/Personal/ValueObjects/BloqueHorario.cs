using ClinicaSaaS.SharedKernel.Results;

namespace ClinicaSaaS.Domain.Personal.ValueObjects;

/// <summary>
/// Value Object: rango de hora del día (HoraInicio/HoraFin) de un bloque de horario. Encapsula
/// la única invariante real (fin posterior a inicio) para que ningún HorarioDoctor pueda
/// construirse con un bloque invertido.
/// </summary>
public sealed record BloqueHorario
{
    public TimeOnly HoraInicio { get; }
    public TimeOnly HoraFin { get; }

    private BloqueHorario(TimeOnly horaInicio, TimeOnly horaFin)
    {
        HoraInicio = horaInicio;
        HoraFin = horaFin;
    }

    public static Result<BloqueHorario> Crear(TimeOnly horaInicio, TimeOnly horaFin)
    {
        if (horaFin <= horaInicio)
            return new Error("BloqueHorario.RangoInvalido", "La hora de fin debe ser posterior a la hora de inicio.");

        return new BloqueHorario(horaInicio, horaFin);
    }

    public bool SeSolapaCon(BloqueHorario otro) =>
        HoraInicio < otro.HoraFin && otro.HoraInicio < HoraFin;

    /// <summary>
    /// Nueva instancia con los mismos valores. Imprescindible cuando el mismo bloque de horario
    /// se aplica a varios HorarioDoctor a la vez (ver Doctor.AgregarHorarioEnVariosDias) — Bloque
    /// está mapeado como owned type (OwnsOne) en EF Core, que rastrea owned types por identidad
    /// de REFERENCIA: si dos HorarioDoctor comparten la misma instancia de BloqueHorario, EF Core
    /// solo logra asociarla con uno de los dos dueños y el otro se guarda con columnas NULL.
    /// </summary>
    internal BloqueHorario Clonar() => new(HoraInicio, HoraFin);
}
