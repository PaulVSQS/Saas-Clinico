using ClinicaSaaS.Domain.Billing.ValueObjects;
using ClinicaSaaS.SharedKernel.Results;

namespace ClinicaSaaS.Application.Facturacion;

public sealed record SecuenciaComprobanteDto(
    Guid Id,
    Guid ClinicaId,
    string NombreClinica,
    string TipoComprobante,
    bool EsElectronico,
    long SecuenciaActual,
    long SecuenciaDesde,
    long SecuenciaHasta,
    DateOnly? FechaVencimiento,
    bool Activo);

public sealed record AutorizarSecuenciaComprobanteRequest(
    Guid ClinicaId, string TipoComprobante, long SecuenciaDesde, long SecuenciaHasta, DateOnly? FechaVencimiento);

/// <summary>
/// Administra los rangos de comprobantes fiscales (NCF/e-CF) que la DGII autorizó a cada
/// clínica — sin una secuencia activa, ninguna factura Fiscal puede numerarse
/// (ver Factura.AsignarNumeroComprobante). Es su propio módulo pequeño porque
/// SecuenciaComprobante es un Aggregate Root independiente (Fase 2) — distintas facturas
/// concurrentes compiten por el mismo contador, así que nunca podría vivir dentro de Factura.
///
/// Mismo criterio de protección que los módulos anteriores: Policy "AdminClinica", con
/// aislamiento de tenant en escritura vía ITenantContext.
/// </summary>
public interface ISecuenciaComprobanteService
{
    Task<IReadOnlyList<SecuenciaComprobanteDto>> ListarAsync(CancellationToken cancellationToken = default);

    Task<Result<Guid>> AutorizarAsync(AutorizarSecuenciaComprobanteRequest request, CancellationToken cancellationToken = default);

    Task<Result> DesactivarAsync(Guid secuenciaComprobanteId, CancellationToken cancellationToken = default);

    /// <summary>Consume el siguiente número de esta secuencia. Devuelve el número formateado y el tipo de comprobante, listos para Factura.AsignarNumeroComprobante.</summary>
    Task<Result<(string Numero, CodigoComprobanteFiscal TipoComprobante)>> TomarSiguienteNumeroAsync(Guid secuenciaComprobanteId, CancellationToken cancellationToken = default);
}