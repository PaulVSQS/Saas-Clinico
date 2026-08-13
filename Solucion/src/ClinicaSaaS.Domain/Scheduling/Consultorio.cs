using ClinicaSaaS.SharedKernel.Abstractions;
using ClinicaSaaS.SharedKernel.Common;
using ClinicaSaaS.SharedKernel.Results;

namespace ClinicaSaaS.Domain.Scheduling;

/// <summary>
/// Aggregate Root. Espacio físico de la clínica. No contiene Equipamiento como colección
/// interna porque el equipamiento tiene ciclo de vida propio e independiente (puede ser móvil,
/// sin consultorio asignado — ver Equipamiento) — modelarlo dentro de Consultorio impediría
/// representar el equipo compartido/no asignado sin un Consultorio "ficticio".
/// </summary>
public sealed class Consultorio : SoftDeleteEntity, ITenantEntity
{
    public Guid ClinicaId { get; private set; }
    public string Nombre { get; private set; }
    public string? Piso { get; private set; }
    public bool Activo { get; private set; }

    private Consultorio() { } // EF Core

    private Consultorio(Guid id, Guid clinicaId, string nombre) : base(id)
    {
        ClinicaId = clinicaId;
        Nombre = nombre;
        Activo = true;
    }

    public static Result<Consultorio> Crear(Guid id, Guid clinicaId, string nombre, string? piso)
    {
        Guard.ContraGuidVacio(id, nameof(id));
        Guard.ContraGuidVacio(clinicaId, nameof(clinicaId));

        if (string.IsNullOrWhiteSpace(nombre))
            return new Error("Consultorio.NombreRequerido", "El nombre del consultorio es requerido.");

        return new Consultorio(id, clinicaId, nombre.Trim()) { Piso = piso?.Trim() };
    }

    /// <summary>
    /// Módulo 5 de Fase 6 — mismo patrón que Doctor.ActualizarDatos/Empleado.ActualizarDatos:
    /// solo los datos editables del perfil (aquí: nombre y piso); ClinicaId nunca se reasigna
    /// desde este método — para "mover" un consultorio a otra clínica habría que dar de baja
    /// este y crear uno nuevo, igual que con Doctor/Empleado y su Usuario/Clínica.
    /// </summary>
    public Result ActualizarDatos(string nombre, string? piso)
    {
        if (string.IsNullOrWhiteSpace(nombre))
            return new Error("Consultorio.NombreRequerido", "El nombre del consultorio es requerido.");

        Nombre = nombre.Trim();
        Piso = piso?.Trim();
        return Result.Exitoso();
    }

    public Result Desactivar()
    {
        if (!Activo)
            return new Error("Consultorio.YaInactivo", "El consultorio ya está inactivo.");

        Activo = false;
        return Result.Exitoso();
    }

    public Result Reactivar()
    {
        if (Activo)
            return new Error("Consultorio.YaActivo", "El consultorio ya está activo.");

        Activo = true;
        return Result.Exitoso();
    }

    public new Result Eliminar(Guid usuarioId, DateTime fechaUtc)
    {
        if (Activo)
            return new Error("Consultorio.DebeDesactivarsePrimero", "Un consultorio activo no puede eliminarse; desactívelo primero.");

        base.Eliminar(usuarioId, fechaUtc);
        return Result.Exitoso();
    }
}
