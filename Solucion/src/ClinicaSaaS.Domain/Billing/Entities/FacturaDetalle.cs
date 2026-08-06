using ClinicaSaaS.Domain.Common.ValueObjects;
using ClinicaSaaS.SharedKernel.Common;
using ClinicaSaaS.SharedKernel.Results;

namespace ClinicaSaaS.Domain.Billing.Entities;

/// <summary>
/// Entity dentro del agregado Factura. Descripcion es un snapshot textual deliberado (no solo
/// el FK al procedimiento) para que, si el catálogo renombra el procedimiento el año próximo,
/// una factura fiscal ya emitida y reportada a la DGII no "cambie" retroactivamente de texto.
/// </summary>
public sealed class FacturaDetalle : EntityBase
{
    public Guid? ProcedimientoRealizadoId { get; private set; }
    public string Descripcion { get; private set; }
    public int Cantidad { get; private set; }
    public Dinero PrecioUnitario { get; private set; }

    public Dinero Subtotal => Dinero.Crear(PrecioUnitario.Monto * Cantidad).Value;

    private FacturaDetalle() { } // EF Core

    internal FacturaDetalle(Guid id, Guid? procedimientoRealizadoId, string descripcion, int cantidad, Dinero precioUnitario) : base(id)
    {
        ProcedimientoRealizadoId = procedimientoRealizadoId;
        Descripcion = descripcion;
        Cantidad = cantidad;
        PrecioUnitario = precioUnitario;
    }

    internal static Result<FacturaDetalle> Crear(Guid id, Guid? procedimientoRealizadoId, string descripcion, int cantidad, Dinero precioUnitario)
    {
        if (string.IsNullOrWhiteSpace(descripcion))
            return new Error("FacturaDetalle.DescripcionRequerida", "La descripción del detalle es requerida.");

        if (cantidad <= 0)
            return new Error("FacturaDetalle.CantidadInvalida", "La cantidad debe ser mayor a cero.");

        return new FacturaDetalle(id, procedimientoRealizadoId, descripcion.Trim(), cantidad, precioUnitario);
    }
}
