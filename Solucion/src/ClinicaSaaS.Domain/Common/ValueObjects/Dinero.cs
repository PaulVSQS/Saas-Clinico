using ClinicaSaaS.SharedKernel.Results;

namespace ClinicaSaaS.Domain.Common.ValueObjects;

/// <summary>
/// Value Object: monto monetario. Se usa en ProcedimientosCatalogo, ProcedimientosRealizados,
/// Facturas y Pagos — centralizarlo evita que la regla "nunca negativo, siempre 2 decimales"
/// se repita (o se olvide) en cuatro lugares distintos. Vive en Domain.Common porque no
/// pertenece a un solo bounded context; el diseño de BD ya fijó DECIMAL(18,2) en todos los
/// montos, así que el dominio simplemente hace cumplir esa misma precisión en memoria.
/// La moneda no se modela explícitamente (RD$ implícito en todo el sistema, según el alcance
/// actual); si el SaaS se expande a multi-moneda, este es el punto único de extensión.
/// </summary>
public sealed record Dinero
{
    public decimal Monto { get; }

    private Dinero(decimal monto) => Monto = monto;

    public static Result<Dinero> Crear(decimal monto)
    {
        if (monto < 0)
            return new Error("Dinero.Negativo", "El monto no puede ser negativo.");

        return new Dinero(Math.Round(monto, 2, MidpointRounding.AwayFromZero));
    }

    public static Dinero Cero => new(0m);

    public Dinero Sumar(Dinero otro) => new(Monto + otro.Monto);
    public Dinero Restar(Dinero otro) => new(Math.Max(0, Monto - otro.Monto));

    public static bool operator >(Dinero a, Dinero b) => a.Monto > b.Monto;
    public static bool operator <(Dinero a, Dinero b) => a.Monto < b.Monto;
    public static bool operator >=(Dinero a, Dinero b) => a.Monto >= b.Monto;
    public static bool operator <=(Dinero a, Dinero b) => a.Monto <= b.Monto;

    public override string ToString() => Monto.ToString("N2");
}
