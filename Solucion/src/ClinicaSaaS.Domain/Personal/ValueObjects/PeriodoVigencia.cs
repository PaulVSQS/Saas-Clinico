using ClinicaSaaS.SharedKernel.Results;

namespace ClinicaSaaS.Domain.Personal.ValueObjects;

/// <summary>
/// Value Object: rango de fechas en que un HorarioDoctor está vigente. FechaFin nula significa
/// vigencia indefinida (caso normal de un horario fijo permanente); el requisito de negocio de
/// que Fin sea posterior a Inicio solo aplica cuando Fin está presente.
/// </summary>
public sealed record PeriodoVigencia
{
    public DateOnly FechaInicio { get; }
    public DateOnly? FechaFin { get; }

    private PeriodoVigencia(DateOnly fechaInicio, DateOnly? fechaFin)
    {
        FechaInicio = fechaInicio;
        FechaFin = fechaFin;
    }

    public static Result<PeriodoVigencia> Crear(DateOnly fechaInicio, DateOnly? fechaFin)
    {
        if (fechaFin is not null && fechaFin < fechaInicio)
            return new Error("PeriodoVigencia.RangoInvalido", "La fecha de fin no puede ser anterior a la fecha de inicio.");

        return new PeriodoVigencia(fechaInicio, fechaFin);
    }

    public bool VigenteEn(DateOnly fecha) => fecha >= FechaInicio && (FechaFin is null || fecha <= FechaFin);
}
