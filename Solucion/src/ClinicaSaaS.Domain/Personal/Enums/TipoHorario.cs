namespace ClinicaSaaS.Domain.Personal.Enums;

/// <summary>
/// Modalidad de un bloque de horario de doctor. Fijo = mismo día/consultorio cada semana;
/// Rotativo = mismo día/hora pero el consultorio puede variar (ConsultorioId nulo permitido);
/// PorRango = vigencia acotada a un rango de fechas específico (cobertura temporal, suplencia).
/// </summary>
public enum TipoHorario
{
    Fijo = 1,
    Rotativo = 2,
    PorRango = 3
}
