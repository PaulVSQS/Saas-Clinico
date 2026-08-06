namespace ClinicaSaaS.Domain.Billing.Enums;

/// <summary>
/// Solo aplica a facturas Fiscales (e-CF) — una factura Interna nunca tiene EstadoDGII, lo cual
/// se hace cumplir en el propio agregado Factura (constructor no permite asignarlo a Interna),
/// reflejando el mismo CHECK constraint que ya existe en BD.
/// </summary>
public enum EstadoDGII
{
    Pendiente = 1,
    Enviado = 2,
    Aceptado = 3,
    Rechazado = 4,
    Contingencia = 5
}
