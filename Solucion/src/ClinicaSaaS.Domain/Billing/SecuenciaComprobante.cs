using ClinicaSaaS.Domain.Billing.ValueObjects;
using ClinicaSaaS.SharedKernel.Abstractions;
using ClinicaSaaS.SharedKernel.Common;
using ClinicaSaaS.SharedKernel.Results;

namespace ClinicaSaaS.Domain.Billing;

/// <summary>
/// Aggregate Root. Rango de comprobantes fiscales (e-CF/NCF) autorizado por la DGII para una
/// clínica y tipo de comprobante. Es su propio agregado —no vive dentro de Factura— porque
/// distintas Facturas concurrentes compiten por el mismo contador (SecuenciaActual): forzar el
/// número por dentro de Factura no protegería contra dos facturas tomando el mismo número. El
/// método TomarSiguienteNumero() es el único punto de entrada para consumir un número, pensado
/// para usarse bajo concurrencia optimista/pesimista en Persistence.
/// </summary>
public sealed class SecuenciaComprobante : EntityBase, ITenantEntity
{
    public Guid ClinicaId { get; private set; }
    public CodigoComprobanteFiscal TipoComprobante { get; private set; }
    public bool EsElectronico { get; private set; }
    public long SecuenciaActual { get; private set; }
    public long SecuenciaDesde { get; private set; }
    public long SecuenciaHasta { get; private set; }
    public DateOnly? FechaVencimiento { get; private set; }
    public bool Activo { get; private set; }

    private SecuenciaComprobante() { } // EF Core

    private SecuenciaComprobante(
        Guid id, Guid clinicaId, CodigoComprobanteFiscal tipoComprobante, long secuenciaDesde, long secuenciaHasta,
        DateOnly? fechaVencimiento) : base(id)
    {
        ClinicaId = clinicaId;
        TipoComprobante = tipoComprobante;
        EsElectronico = tipoComprobante.EsElectronico;
        SecuenciaActual = secuenciaDesde - 1;
        SecuenciaDesde = secuenciaDesde;
        SecuenciaHasta = secuenciaHasta;
        FechaVencimiento = fechaVencimiento;
        Activo = true;
    }

    public static Result<SecuenciaComprobante> Autorizar(
        Guid id, Guid clinicaId, CodigoComprobanteFiscal tipoComprobante, long secuenciaDesde, long secuenciaHasta, DateOnly? fechaVencimiento)
    {
        Guard.ContraGuidVacio(id, nameof(id));
        Guard.ContraGuidVacio(clinicaId, nameof(clinicaId));
        Guard.ContraNulo(tipoComprobante, nameof(tipoComprobante));

        if (secuenciaHasta <= secuenciaDesde)
            return new Error("SecuenciaComprobante.RangoInvalido", "La secuencia 'hasta' debe ser mayor que la secuencia 'desde'.");

        return new SecuenciaComprobante(id, clinicaId, tipoComprobante, secuenciaDesde, secuenciaHasta, fechaVencimiento);
    }

    /// <summary>
    /// Consume el siguiente número de la secuencia autorizada. Devuelve el número completo
    /// formateado (prefijo + correlativo con ceros a la izquierda) listo para asignarse a una
    /// Factura vía Factura.AsignarNumeroComprobante.
    /// </summary>
    public Result<string> TomarSiguienteNumero(DateOnly fechaActual)
    {
        if (!Activo)
            return new Error("SecuenciaComprobante.Inactiva", "Esta secuencia de comprobantes no está activa.");

        if (FechaVencimiento is not null && fechaActual > FechaVencimiento)
            return new Error("SecuenciaComprobante.Vencida", "Esta secuencia de comprobantes está vencida.");

        if (SecuenciaActual + 1 > SecuenciaHasta)
            return new Error("SecuenciaComprobante.Agotada", "Esta secuencia de comprobantes se agotó; debe solicitarse un nuevo rango a la DGII.");

        SecuenciaActual++;
        return $"{TipoComprobante.Valor}{SecuenciaActual:D10}";
    }

    public Result Desactivar()
    {
        if (!Activo)
            return new Error("SecuenciaComprobante.YaInactiva", "La secuencia ya está inactiva.");

        Activo = false;
        return Result.Exitoso();
    }
}
