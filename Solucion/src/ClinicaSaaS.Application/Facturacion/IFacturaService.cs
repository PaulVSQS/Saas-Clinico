using ClinicaSaaS.Domain.Billing.Enums;
using ClinicaSaaS.SharedKernel.Results;

namespace ClinicaSaaS.Application.Facturacion;

public sealed record FacturaDetalleDto(
    Guid Id,
    Guid? ProcedimientoRealizadoId,
    string Descripcion,
    int Cantidad,
    decimal PrecioUnitario,
    decimal Subtotal);

public sealed record FacturaDto(
    Guid Id,
    Guid ClinicaId,
    string NombreClinica,
    Guid PacienteId,
    string NombrePaciente,
    TipoFactura TipoFactura,
    string? NumeroComprobante,
    EstadoDGII? EstadoDGII,
    DateTime FechaEmision,
    decimal Subtotal,
    decimal Descuento,
    decimal Impuestos,
    decimal Total,
    decimal TotalPagado,
    decimal SaldoPendiente,
    EstadoFactura Estado,
    bool EstaAnulada,
    string? MotivoAnulacion,
    IReadOnlyList<FacturaDetalleDto> Detalles);

public sealed record EmitirFacturaRequest(Guid PacienteId, TipoFactura TipoFactura);

/// <summary>
/// Un detalle facturado desde un procedimiento realizado toma Descripcion/PrecioUnitario como
/// snapshot de ese procedimiento (ProcedimientoRealizadoId no nulo) — DescripcionManual y
/// PrecioUnitarioManual se ignoran en ese caso. Sin ProcedimientoRealizadoId, ambos campos
/// manuales son requeridos (ej. un cargo administrativo sin procedimiento clínico asociado).
/// </summary>
public sealed record AgregarDetalleFacturaRequest(
    Guid? ProcedimientoRealizadoId, string? DescripcionManual, int Cantidad, decimal? PrecioUnitarioManual);

public sealed record AplicarDescuentoFacturaRequest(decimal Monto);

public sealed record AplicarImpuestosFacturaRequest(decimal Monto);

/// <summary>
/// Módulo 12 de Fase 6 — Facturación: emitir facturas (Fiscales o Internas), agregar detalles,
/// aplicar descuento/impuestos, asignar número de comprobante fiscal (consumiendo una
/// SecuenciaComprobante activa, ver ISecuenciaComprobanteService), actualizar su estado DGII, y
/// anularlas.
///
/// DELIBERADAMENTE FUERA de este servicio: RegistrarPago/AnularPago — aunque YA EXISTEN en el
/// mismo agregado Factura (Fase 2), corresponden al Módulo 13 (Pagos) del roadmap. También fuera
/// de alcance: una integración real con el webservice de la DGII — ActualizarEstadoDGIIAsync es
/// una transición manual (quien opera el sistema la actualiza a mano según lo que reporte el
/// portal de la DGII), no una llamada automática a ningún servicio externo.
///
/// Mismo criterio de protección que los módulos anteriores: Policy "AdminClinica", con
/// aislamiento de tenant en escritura vía ITenantContext al emitir.
/// </summary>
public interface IFacturaService
{
    Task<IReadOnlyList<FacturaDto>> ListarPorPacienteAsync(Guid pacienteId, CancellationToken cancellationToken = default);

    Task<FacturaDto?> ObtenerAsync(Guid facturaId, CancellationToken cancellationToken = default);

    Task<Result<Guid>> EmitirAsync(EmitirFacturaRequest request, CancellationToken cancellationToken = default);

    Task<Result<Guid>> AgregarDetalleAsync(Guid facturaId, AgregarDetalleFacturaRequest request, CancellationToken cancellationToken = default);

    Task<Result> AplicarDescuentoAsync(Guid facturaId, AplicarDescuentoFacturaRequest request, CancellationToken cancellationToken = default);

    Task<Result> AplicarImpuestosAsync(Guid facturaId, AplicarImpuestosFacturaRequest request, CancellationToken cancellationToken = default);

    /// <summary>Consume el siguiente número de la SecuenciaComprobante indicada y lo asigna a la factura. Solo aplica a facturas Fiscales.</summary>
    Task<Result> AsignarNumeroComprobanteAsync(Guid facturaId, Guid secuenciaComprobanteId, CancellationToken cancellationToken = default);

    Task<Result> ActualizarEstadoDGIIAsync(Guid facturaId, EstadoDGII nuevoEstado, CancellationToken cancellationToken = default);

    Task<Result> AnularAsync(Guid facturaId, string motivo, CancellationToken cancellationToken = default);

    Task<Result> EliminarAsync(Guid facturaId, CancellationToken cancellationToken = default);
}