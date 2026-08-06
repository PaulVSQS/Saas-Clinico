using ClinicaSaaS.SharedKernel.Results;

namespace ClinicaSaaS.Domain.Clinical.ValueObjects;

/// <summary>
/// Value Object: signos vitales de una entrada de historial clínico. La BD lo modela como
/// SignosVitalesJson (NVARCHAR(MAX) flexible) para no bloquear el esquema ante nuevos campos
/// médicos futuros; el dominio, en cambio, sí modela los campos comunes de forma explícita
/// porque son los que participan en reglas de negocio y presentación clínica (alertas de
/// presión/temperatura anómala). La serialización a JSON es responsabilidad de Persistence,
/// no del dominio. Todos los campos son opcionales: no todas las consultas registran todos.
/// </summary>
public sealed record SignosVitales
{
    public decimal? PresionSistolica { get; }
    public decimal? PresionDiastolica { get; }
    public decimal? TemperaturaCelsius { get; }
    public int? FrecuenciaCardiaca { get; }
    public int? FrecuenciaRespiratoria { get; }
    public decimal? SaturacionOxigeno { get; }
    public decimal? PesoKg { get; }
    public decimal? TallaCm { get; }

    private SignosVitales(
        decimal? presionSistolica, decimal? presionDiastolica, decimal? temperaturaCelsius,
        int? frecuenciaCardiaca, int? frecuenciaRespiratoria, decimal? saturacionOxigeno,
        decimal? pesoKg, decimal? tallaCm)
    {
        PresionSistolica = presionSistolica;
        PresionDiastolica = presionDiastolica;
        TemperaturaCelsius = temperaturaCelsius;
        FrecuenciaCardiaca = frecuenciaCardiaca;
        FrecuenciaRespiratoria = frecuenciaRespiratoria;
        SaturacionOxigeno = saturacionOxigeno;
        PesoKg = pesoKg;
        TallaCm = tallaCm;
    }

    public static Result<SignosVitales> Crear(
        decimal? presionSistolica = null, decimal? presionDiastolica = null, decimal? temperaturaCelsius = null,
        int? frecuenciaCardiaca = null, int? frecuenciaRespiratoria = null, decimal? saturacionOxigeno = null,
        decimal? pesoKg = null, decimal? tallaCm = null)
    {
        if (saturacionOxigeno is > 100 or < 0)
            return new Error("SignosVitales.SaturacionInvalida", "La saturación de oxígeno debe estar entre 0 y 100.");

        if (pesoKg is < 0 || tallaCm is < 0)
            return new Error("SignosVitales.ValorNegativo", "Peso y talla no pueden ser negativos.");

        return new SignosVitales(
            presionSistolica, presionDiastolica, temperaturaCelsius,
            frecuenciaCardiaca, frecuenciaRespiratoria, saturacionOxigeno, pesoKg, tallaCm);
    }
}
