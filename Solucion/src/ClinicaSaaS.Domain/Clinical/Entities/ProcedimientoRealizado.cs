using ClinicaSaaS.Domain.Common.ValueObjects;
using ClinicaSaaS.SharedKernel.Common;

namespace ClinicaSaaS.Domain.Clinical.Entities;

/// <summary>
/// Entity dentro del agregado HistorialClinico: evidencia clínica de un procedimiento ejecutado
/// en una consulta específica. PrecioAplicado es un snapshot deliberado del precio del catálogo
/// al momento de ejecutarse — si el catálogo sube de precio después, este registro histórico
/// no debe recalcularse retroactivamente (mismo patrón que Billing.FacturaDetalles).
/// </summary>
public sealed class ProcedimientoRealizado : EntityBase
{
    public Guid ProcedimientoCatalogoId { get; private set; }
    public Guid DoctorId { get; private set; }
    public string? PiezaDental { get; private set; } // notación FDI — solo aplica a odontología
    public Dinero PrecioAplicado { get; private set; }
    public DateTime Fecha { get; private set; }
    public string? Notas { get; private set; }

    private ProcedimientoRealizado() { } // EF Core

    internal ProcedimientoRealizado(
        Guid id, Guid procedimientoCatalogoId, Guid doctorId, string? piezaDental,
        Dinero precioAplicado, DateTime fechaUtc, string? notas) : base(id)
    {
        ProcedimientoCatalogoId = procedimientoCatalogoId;
        DoctorId = doctorId;
        PiezaDental = piezaDental;
        PrecioAplicado = precioAplicado;
        Fecha = fechaUtc;
        Notas = notas;
    }
}
