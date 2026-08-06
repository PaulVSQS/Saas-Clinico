namespace ClinicaSaaS.Domain.Billing.Events;

/// <summary>
/// Se dispara cuando la suma de pagos activos alcanza el Total de la factura. Justificación:
/// dispara notificaciones al paciente/recepción y, para facturas Fiscales, puede ser el gatillo
/// para iniciar el envío a la DGII si esa integración lo requiere en ese momento del flujo.
/// </summary>
public sealed record FacturaPagadaDomainEvent(Guid FacturaId, Guid ClinicaId, Guid PacienteId, DateTime FechaUtc);
