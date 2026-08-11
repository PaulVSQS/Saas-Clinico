using ClinicaSaaS.SharedKernel.Abstractions;
using ClinicaSaaS.SharedKernel.Common;
using ClinicaSaaS.SharedKernel.Results;

namespace ClinicaSaaS.Domain.Personal;

/// <summary>
/// Aggregate Root. Perfil de personal administrativo/operativo dentro de una clínica, simétrico
/// a Doctor pero sin horarios estructurados (recepción/contabilidad no requieren la misma
/// modelación de bloques que la agenda médica en este alcance del dominio).
/// </summary>
public sealed class Empleado : SoftDeleteEntity, ITenantEntity
{
    public Guid UsuarioId { get; private set; }
    public Guid ClinicaId { get; private set; }
    public string Cargo { get; private set; }
    public DateOnly? FechaIngreso { get; private set; }
    public bool Activo { get; private set; }

    private Empleado() { } // EF Core

    private Empleado(Guid id, Guid usuarioId, Guid clinicaId, string cargo) : base(id)
    {
        UsuarioId = usuarioId;
        ClinicaId = clinicaId;
        Cargo = cargo;
        Activo = true;
    }

    public static Result<Empleado> Registrar(Guid id, Guid usuarioId, Guid clinicaId, string cargo, DateOnly? fechaIngreso)
    {
        Guard.ContraGuidVacio(id, nameof(id));
        Guard.ContraGuidVacio(usuarioId, nameof(usuarioId));
        Guard.ContraGuidVacio(clinicaId, nameof(clinicaId));

        if (string.IsNullOrWhiteSpace(cargo))
            return new Error("Empleado.CargoRequerido", "El cargo es requerido.");

        return new Empleado(id, usuarioId, clinicaId, cargo.Trim()) { FechaIngreso = fechaIngreso };
    }

    public Result ActualizarDatos(string cargo, DateOnly? fechaIngreso)
    {
        if (string.IsNullOrWhiteSpace(cargo))
            return new Error("Empleado.CargoRequerido", "El cargo es requerido.");

        Cargo = cargo.Trim();
        FechaIngreso = fechaIngreso;
        return Result.Exitoso();
    }

    public Result Desactivar()
    {
        if (!Activo)
            return new Error("Empleado.YaInactivo", "El empleado ya está inactivo.");

        Activo = false;
        return Result.Exitoso();
    }

    public Result Reactivar()
    {
        if (Activo)
            return new Error("Empleado.YaActivo", "El empleado ya está activo.");

        Activo = true;
        return Result.Exitoso();
    }

    public new Result Eliminar(Guid usuarioId, DateTime fechaUtc)
    {
        if (Activo)
            return new Error("Empleado.DebeDesactivarsePrimero", "Un empleado activo no puede eliminarse; desactívelo primero.");

        base.Eliminar(usuarioId, fechaUtc);
        return Result.Exitoso();
    }
}
