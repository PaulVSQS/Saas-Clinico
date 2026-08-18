using ClinicaSaaS.Domain.Billing.Enums;
using ClinicaSaaS.SharedKernel.Results;

namespace ClinicaSaaS.Application.Facturacion;

public sealed record PagoDto(
    Guid Id,
    Guid FacturaId,
    decimal Monto,
    DateTime FechaHora,
    MetodoPago MetodoPago,
    string? Referencia,
    Guid RegistradoPorUsuarioId,
    string NombreRegistradoPor,
    bool EstaAnulado,
    string? MotivoAnulacion);

public sealed record RegistrarPagoRequest(Guid FacturaId, decimal Monto, MetodoPago MetodoPago, string? Referencia);

/// <summary>
/// Módulo 13 de Fase 6 — Pagos: abonos registrados contra una factura. El agregado que los
/// contiene (Factura) ya existía completo desde Fase 2 — este servicio solo expone lo que el
/// Módulo 12 (Facturación) dejó deliberadamente fuera: RegistrarPago/AnularPago.
///
/// NO HAY ActualizarAsync NI EliminarAsync EN ESTE SERVICIO — a propósito. Un pago mal
/// registrado se corrige anulándolo (con motivo) y registrando uno nuevo, nunca editando el
/// monto ni eliminándolo físicamente — así lo exige el propio dominio (ver Pago.cs), para
/// mantener un rastro auditable completo de cada corrección.
///
/// Mismo criterio de protección que los módulos anteriores: Policy "AdminClinica".
/// </summary>
public interface IPagoService
{
    Task<IReadOnlyList<PagoDto>> ListarPorFacturaAsync(Guid facturaId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PagoDto>> ListarPorPacienteAsync(Guid pacienteId, CancellationToken cancellationToken = default);

    Task<Result<Guid>> RegistrarAsync(RegistrarPagoRequest request, CancellationToken cancellationToken = default);

    Task<Result> AnularAsync(Guid facturaId, Guid pagoId, string motivo, CancellationToken cancellationToken = default);
}