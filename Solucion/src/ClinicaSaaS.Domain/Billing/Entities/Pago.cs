using ClinicaSaaS.Domain.Billing.Enums;
using ClinicaSaaS.Domain.Common.ValueObjects;
using ClinicaSaaS.SharedKernel.Common;
using ClinicaSaaS.SharedKernel.Results;

namespace ClinicaSaaS.Domain.Billing.Entities;

/// <summary>
/// Entity dentro del agregado Factura. Un abono mal registrado se corrige anulándolo
/// (EstaAnulado + motivo) y registrando uno nuevo — nunca editando el monto ni eliminándolo
/// físicamente, para que un contador/auditor pueda reconstruir exactamente qué pasó y quién lo hizo.
/// </summary>
public sealed class Pago : EntityBase
{
    public Dinero Monto { get; private set; }
    public DateTime FechaHora { get; private set; }
    public MetodoPago MetodoPago { get; private set; }
    public string? Referencia { get; private set; }
    public Guid RegistradoPorUsuarioId { get; private set; }
    public bool EstaAnulado { get; private set; }
    public string? MotivoAnulacion { get; private set; }

    private Pago() { } // EF Core

    internal Pago(Guid id, Dinero monto, DateTime fechaUtc, MetodoPago metodoPago, string? referencia, Guid registradoPorUsuarioId) : base(id)
    {
        Monto = monto;
        FechaHora = fechaUtc;
        MetodoPago = metodoPago;
        Referencia = referencia;
        RegistradoPorUsuarioId = registradoPorUsuarioId;
    }

    internal Result Anular(string motivo)
    {
        if (EstaAnulado)
            return new Error("Pago.YaAnulado", "El pago ya está anulado.");

        if (string.IsNullOrWhiteSpace(motivo))
            return new Error("Pago.MotivoAnulacionRequerido", "El motivo de anulación es requerido.");

        EstaAnulado = true;
        MotivoAnulacion = motivo.Trim();
        return Result.Exitoso();
    }
}
