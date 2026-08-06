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
}
