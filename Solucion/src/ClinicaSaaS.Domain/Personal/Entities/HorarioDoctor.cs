using ClinicaSaaS.Domain.Personal.Enums;
using ClinicaSaaS.Domain.Personal.ValueObjects;
using ClinicaSaaS.SharedKernel.Common;
using ClinicaSaaS.SharedKernel.Results;

namespace ClinicaSaaS.Domain.Personal.Entities;

/// <summary>
/// Entity (no Aggregate Root) dentro del agregado Doctor. Tiene identidad propia (Id) porque
/// un doctor puede tener varios bloques de horario que se editan/eliminan individualmente,
/// pero no tiene sentido de negocio fuera de su Doctor — nunca se consulta ni se modifica un
/// HorarioDoctor sin pasar por el agregado Doctor, por eso el constructor y las operaciones de
/// escritura son internal al agregado (ver Doctor.cs).
/// </summary>
public sealed class HorarioDoctor : EntityBase
{
    public Guid? ConsultorioId { get; private set; } // null = rotativo aún no asignado a un consultorio fijo
    public TipoHorario TipoHorario { get; private set; }
    public DayOfWeek? DiaSemana { get; private set; } // null solo si TipoHorario == PorRango
    public BloqueHorario Bloque { get; private set; }
    public PeriodoVigencia Vigencia { get; private set; }
    public bool Activo { get; private set; }

    private HorarioDoctor() { } // EF Core

    internal HorarioDoctor(
        Guid id, Guid? consultorioId, TipoHorario tipoHorario, DayOfWeek? diaSemana,
        BloqueHorario bloque, PeriodoVigencia vigencia) : base(id)
    {
        ConsultorioId = consultorioId;
        TipoHorario = tipoHorario;
        DiaSemana = diaSemana;
        Bloque = bloque;
        Vigencia = vigencia;
        Activo = true;
    }

    internal void Desactivar() => Activo = false;

    internal void Reactivar() => Activo = true;

    /// <summary>
    /// Muta los datos editables del bloque. Sin validación propia — Doctor.ActualizarHorario ya
    /// valida (día requerido salvo PorRango, solapamiento contra los demás bloques) ANTES de
    /// llamar aquí, mismo criterio que AgregarHorario valida antes de invocar el constructor.
    /// </summary>
    internal void ActualizarDatos(
        Guid? consultorioId, TipoHorario tipoHorario, DayOfWeek? diaSemana,
        BloqueHorario bloque, PeriodoVigencia vigencia)
    {
        ConsultorioId = consultorioId;
        TipoHorario = tipoHorario;
        DiaSemana = diaSemana;
        Bloque = bloque;
        Vigencia = vigencia;
    }

    public bool SeSolapaCon(HorarioDoctor otro)
    {
        if (!Activo || !otro.Activo) return false;
        if (DiaSemana != otro.DiaSemana) return false;
        return Bloque.SeSolapaCon(otro.Bloque);
    }

    /// <summary>
    /// Igual que <see cref="SeSolapaCon"/> pero contra datos propuestos (no contra otra instancia
    /// ya construida) — lo usa Doctor.ActualizarHorario/ReactivarHorario para revalidar la
    /// invariante de solapamiento sin tener que instanciar un HorarioDoctor temporal.
    /// </summary>
    internal bool SeSolapaConPropuesta(DayOfWeek? diaSemanaPropuesto, BloqueHorario bloquePropuesto)
    {
        if (!Activo) return false;
        if (DiaSemana != diaSemanaPropuesto) return false;
        return Bloque.SeSolapaCon(bloquePropuesto);
    }
}
