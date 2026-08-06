using ClinicaSaaS.Domain.Common.ValueObjects;
using ClinicaSaaS.SharedKernel.Abstractions;
using ClinicaSaaS.SharedKernel.Common;
using ClinicaSaaS.SharedKernel.Results;

namespace ClinicaSaaS.Domain.Clinical;

/// <summary>
/// Aggregate Root independiente (no vive dentro de HistorialClinico ni de Factura). Es un
/// catálogo de precios administrado por la clínica con su propio ciclo de vida (activar/
/// desactivar/repriciar) que decenas de ProcedimientosRealizados y FacturaDetalles referencian
/// solo por Id — nunca por navegación— precisamente para que cambiar un precio hoy no reescriba
/// snapshots históricos ya tomados.
/// </summary>
public sealed class ProcedimientoCatalogo : SoftDeleteEntity, ITenantEntity
{
    public Guid ClinicaId { get; private set; }
    public string Codigo { get; private set; }
    public string Nombre { get; private set; }
    public string? Descripcion { get; private set; }
    public Dinero PrecioBase { get; private set; }
    public bool Activo { get; private set; }

    private ProcedimientoCatalogo() { } // EF Core

    private ProcedimientoCatalogo(Guid id, Guid clinicaId, string codigo, string nombre, Dinero precioBase) : base(id)
    {
        ClinicaId = clinicaId;
        Codigo = codigo;
        Nombre = nombre;
        PrecioBase = precioBase;
        Activo = true;
    }

    public static Result<ProcedimientoCatalogo> Crear(
        Guid id, Guid clinicaId, string codigo, string nombre, string? descripcion, Dinero precioBase)
    {
        Guard.ContraGuidVacio(id, nameof(id));
        Guard.ContraGuidVacio(clinicaId, nameof(clinicaId));

        if (string.IsNullOrWhiteSpace(codigo))
            return new Error("ProcedimientoCatalogo.CodigoRequerido", "El código es requerido.");

        if (string.IsNullOrWhiteSpace(nombre))
            return new Error("ProcedimientoCatalogo.NombreRequerido", "El nombre es requerido.");

        return new ProcedimientoCatalogo(id, clinicaId, codigo.Trim(), nombre.Trim(), precioBase) { Descripcion = descripcion?.Trim() };
    }

    public Result CambiarPrecio(Dinero nuevoPrecio)
    {
        if (!Activo)
            return new Error("ProcedimientoCatalogo.Inactivo", "No se puede repreciar un procedimiento inactivo.");

        PrecioBase = nuevoPrecio;
        return Result.Exitoso();
    }

    public Result Desactivar()
    {
        if (!Activo)
            return new Error("ProcedimientoCatalogo.YaInactivo", "El procedimiento ya está inactivo.");

        Activo = false;
        return Result.Exitoso();
    }

    public Result Reactivar()
    {
        if (Activo)
            return new Error("ProcedimientoCatalogo.YaActivo", "El procedimiento ya está activo.");

        Activo = true;
        return Result.Exitoso();
    }

    public new Result Eliminar(Guid usuarioId, DateTime fechaUtc)
    {
        if (Activo)
            return new Error("ProcedimientoCatalogo.DebeDesactivarsePrimero", "Un procedimiento activo no puede eliminarse; desactívelo primero.");

        base.Eliminar(usuarioId, fechaUtc);
        return Result.Exitoso();
    }
}
