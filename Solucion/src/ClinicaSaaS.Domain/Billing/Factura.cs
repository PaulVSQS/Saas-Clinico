using ClinicaSaaS.Domain.Billing.Entities;
using ClinicaSaaS.Domain.Billing.Enums;
using ClinicaSaaS.Domain.Billing.Events;
using ClinicaSaaS.Domain.Billing.ValueObjects;
using ClinicaSaaS.Domain.Common.ValueObjects;
using ClinicaSaaS.SharedKernel.Abstractions;
using ClinicaSaaS.SharedKernel.Common;
using ClinicaSaaS.SharedKernel.Results;

namespace ClinicaSaaS.Domain.Billing;

/// <summary>
/// Aggregate Root. Contiene FacturaDetalles y Pagos como Entities internas porque el total y el
/// estado de la factura (Pendiente/Parcial/Pagada) son invariantes que solo pueden garantizarse
/// si todos los detalles y pagos se modifican en la misma transacción de agregado — dos
/// recepcionistas registrando un abono al mismo tiempo deben chocar por concurrencia optimista
/// (RowVersion en Persistence), no producir un total silenciosamente incorrecto.
/// Referencia a Paciente solo por Id, nunca por navegación (regla DDD estándar de agregados).
/// </summary>
public sealed class Factura : SoftDeleteEntity, ITenantEntity
{
    private readonly List<FacturaDetalle> _detalles = [];
    private readonly List<Pago> _pagos = [];

    public Guid ClinicaId { get; private set; }
    public Guid PacienteId { get; private set; }
    public TipoFactura TipoFactura { get; private set; }
    public string? NumeroComprobante { get; private set; }
    public Enums.EstadoDGII? EstadoDGII { get; private set; }
    public DateTime FechaEmision { get; private set; }
    public Dinero Descuento { get; private set; }
    public Dinero Impuestos { get; private set; }
    public EstadoFactura Estado { get; private set; }
    public bool EstaAnulada { get; private set; }
    public string? MotivoAnulacion { get; private set; }
    public Guid CreadoPorUsuarioId { get; private set; }

    public IReadOnlyCollection<FacturaDetalle> Detalles => _detalles.AsReadOnly();
    public IReadOnlyCollection<Pago> Pagos => _pagos.AsReadOnly();

    public Dinero Subtotal => Dinero.Crear(_detalles.Sum(d => d.Subtotal.Monto)).Value;
    public Dinero Total => Dinero.Crear(Subtotal.Monto - Descuento.Monto + Impuestos.Monto).Value;
    public Dinero TotalPagado => Dinero.Crear(_pagos.Where(p => !p.EstaAnulado).Sum(p => p.Monto.Monto)).Value;
    public Dinero SaldoPendiente => Total.Restar(TotalPagado);

    private Factura() { } // EF Core

    private Factura(Guid id, Guid clinicaId, Guid pacienteId, TipoFactura tipoFactura, Guid creadoPorUsuarioId, DateTime fechaUtc) : base(id)
    {
        ClinicaId = clinicaId;
        PacienteId = pacienteId;
        TipoFactura = tipoFactura;
        FechaEmision = fechaUtc;
        Descuento = Dinero.Cero;
        Impuestos = Dinero.Cero;
        Estado = EstadoFactura.Pendiente;
        CreadoPorUsuarioId = creadoPorUsuarioId;
        EstadoDGII = tipoFactura == Enums.TipoFactura.Fiscal ? Enums.EstadoDGII.Pendiente : null;
    }

    public static Result<Factura> Emitir(Guid id, Guid clinicaId, Guid pacienteId, TipoFactura tipoFactura, Guid creadoPorUsuarioId, DateTime fechaUtc)
    {
        Guard.ContraGuidVacio(id, nameof(id));
        Guard.ContraGuidVacio(clinicaId, nameof(clinicaId));
        Guard.ContraGuidVacio(pacienteId, nameof(pacienteId));

        return new Factura(id, clinicaId, pacienteId, tipoFactura, creadoPorUsuarioId, fechaUtc);
    }

    public Result<FacturaDetalle> AgregarDetalle(Guid idDetalle, Guid? procedimientoRealizadoId, string descripcion, int cantidad, Dinero precioUnitario)
    {
        if (EstaAnulada)
            return new Error("Factura.Anulada", "No se pueden agregar detalles a una factura anulada.");

        if (_pagos.Any(p => !p.EstaAnulado))
            return new Error("Factura.TienePagos", "No se pueden agregar detalles a una factura que ya tiene pagos registrados.");

        var resultadoDetalle = FacturaDetalle.Crear(idDetalle, procedimientoRealizadoId, descripcion, cantidad, precioUnitario);
        if (resultadoDetalle.EsFallido)
            return resultadoDetalle.Error;

        _detalles.Add(resultadoDetalle.Value);
        return resultadoDetalle.Value;
    }

    public Result AplicarDescuento(Dinero descuento)
    {
        if (EstaAnulada)
            return new Error("Factura.Anulada", "No se puede modificar una factura anulada.");

        if (descuento.Monto > Subtotal.Monto)
            return new Error("Factura.DescuentoExcesivo", "El descuento no puede ser mayor al subtotal.");

        Descuento = descuento;
        return Result.Exitoso();
    }

    public Result AplicarImpuestos(Dinero impuestos)
    {
        if (EstaAnulada)
            return new Error("Factura.Anulada", "No se puede modificar una factura anulada.");

        Impuestos = impuestos;
        return Result.Exitoso();
    }

    /// <summary>
    /// Solo aplica a facturas Fiscales — hace cumplir en código el mismo
    /// CK_Factura_NumeroComprobante que ya existe en BD.
    /// </summary>
    public Result AsignarNumeroComprobante(CodigoComprobanteFiscal tipoComprobante, string numeroComprobante)
    {
        if (TipoFactura != Enums.TipoFactura.Fiscal)
            return new Error("Factura.NoEsFiscal", "Solo una factura Fiscal puede recibir número de comprobante.");

        if (!string.IsNullOrEmpty(NumeroComprobante))
            return new Error("Factura.ComprobanteYaAsignado", "Esta factura ya tiene un número de comprobante asignado.");

        if (string.IsNullOrWhiteSpace(numeroComprobante))
            return new Error("Factura.NumeroComprobanteRequerido", "El número de comprobante es requerido.");

        NumeroComprobante = numeroComprobante.Trim();
        return Result.Exitoso();
    }

    public Result ActualizarEstadoDGII(Enums.EstadoDGII nuevoEstado)
    {
        if (TipoFactura != Enums.TipoFactura.Fiscal)
            return new Error("Factura.NoEsFiscal", "Solo una factura Fiscal tiene estado DGII.");

        EstadoDGII = nuevoEstado;
        return Result.Exitoso();
    }

    public Result<Pago> RegistrarPago(Guid idPago, Dinero monto, DateTime fechaUtc, MetodoPago metodoPago, string? referencia, Guid registradoPorUsuarioId)
    {
        if (EstaAnulada)
            return new Error("Factura.Anulada", "No se pueden registrar pagos sobre una factura anulada.");

        if (Estado == EstadoFactura.Pagada)
            return new Error("Factura.YaPagada", "La factura ya está completamente pagada.");

        if (monto.Monto <= 0)
            return new Error("Pago.MontoInvalido", "El monto del pago debe ser mayor a cero.");

        if (monto.Monto > SaldoPendiente.Monto)
            return new Error("Pago.ExcedeSaldo", "El monto del pago no puede exceder el saldo pendiente de la factura.");

        var pago = new Pago(idPago, monto, fechaUtc, metodoPago, referencia, registradoPorUsuarioId);
        _pagos.Add(pago);

        RecalcularEstadoPorPagos(fechaUtc);
        return pago;
    }

    public Result AnularPago(Guid pagoId, string motivo, DateTime fechaUtc)
    {
        var pago = _pagos.FirstOrDefault(p => p.Id == pagoId);
        if (pago is null)
            return new Error("Pago.NoEncontrado", "El pago no pertenece a esta factura.");

        var resultado = pago.Anular(motivo);
        if (resultado.EsFallido)
            return resultado;

        RecalcularEstadoPorPagos(fechaUtc);
        return Result.Exitoso();
    }

    public Result Anular(string motivo)
    {
        if (EstaAnulada)
            return new Error("Factura.YaAnulada", "La factura ya está anulada.");

        if (_pagos.Any(p => !p.EstaAnulado))
            return new Error("Factura.TienePagosActivos", "No se puede anular una factura con pagos activos; anule los pagos primero.");

        if (string.IsNullOrWhiteSpace(motivo))
            return new Error("Factura.MotivoAnulacionRequerido", "El motivo de anulación es requerido.");

        EstaAnulada = true;
        MotivoAnulacion = motivo.Trim();
        Estado = EstadoFactura.Anulada;
        return Result.Exitoso();
    }

    private void RecalcularEstadoPorPagos(DateTime fechaUtc)
    {
        if (TotalPagado.Monto <= 0)
        {
            Estado = EstadoFactura.Pendiente;
        }
        else if (TotalPagado.Monto >= Total.Monto)
        {
            var eraPagada = Estado == EstadoFactura.Pagada;
            Estado = EstadoFactura.Pagada;
            if (!eraPagada)
                RaiseEvent(new FacturaPagadaDomainEvent(Id, ClinicaId, PacienteId, fechaUtc));
        }
        else
        {
            Estado = EstadoFactura.Parcial;
        }
    }

    public new Result Eliminar(Guid usuarioId, DateTime fechaUtc)
    {
        if (!EstaAnulada)
            return new Error("Factura.DebeAnularsePrimero", "Una factura no anulada no puede eliminarse; anúlela primero.");

        base.Eliminar(usuarioId, fechaUtc);
        return Result.Exitoso();
    }
}
