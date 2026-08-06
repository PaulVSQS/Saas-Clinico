using ClinicaSaaS.SharedKernel.Abstractions;
using ClinicaSaaS.SharedKernel.Common;
using ClinicaSaaS.SharedKernel.Results;

namespace ClinicaSaaS.Domain.Scheduling;

/// <summary>
/// Aggregate Root separado de Consultorio (ver justificación en Consultorio.cs). ConsultorioId
/// nulo representa equipo móvil o compartido entre consultorios — estado de negocio válido,
/// no un dato faltante.
/// </summary>
public sealed class Equipamiento : SoftDeleteEntity, ITenantEntity
{
    public Guid ClinicaId { get; private set; }
    public Guid? ConsultorioId { get; private set; }
    public string Nombre { get; private set; }
    public string? NumeroSerie { get; private set; }
    public DateOnly? FechaAdquisicion { get; private set; }
    public DateOnly? ProximoMantenimiento { get; private set; }
    public bool Activo { get; private set; }

    private Equipamiento() { } // EF Core

    private Equipamiento(Guid id, Guid clinicaId, string nombre) : base(id)
    {
        ClinicaId = clinicaId;
        Nombre = nombre;
        Activo = true;
    }

    public static Result<Equipamiento> Registrar(
        Guid id, Guid clinicaId, Guid? consultorioId, string nombre, string? numeroSerie, DateOnly? fechaAdquisicion)
    {
        Guard.ContraGuidVacio(id, nameof(id));
        Guard.ContraGuidVacio(clinicaId, nameof(clinicaId));

        if (string.IsNullOrWhiteSpace(nombre))
            return new Error("Equipamiento.NombreRequerido", "El nombre del equipo es requerido.");

        return new Equipamiento(id, clinicaId, nombre.Trim())
        {
            ConsultorioId = consultorioId,
            NumeroSerie = numeroSerie?.Trim(),
            FechaAdquisicion = fechaAdquisicion
        };
    }

    public Result Reubicar(Guid? nuevoConsultorioId)
    {
        ConsultorioId = nuevoConsultorioId;
        return Result.Exitoso();
    }

    public Result ProgramarMantenimiento(DateOnly fecha)
    {
        ProximoMantenimiento = fecha;
        return Result.Exitoso();
    }

    public Result Desactivar()
    {
        if (!Activo)
            return new Error("Equipamiento.YaInactivo", "El equipo ya está inactivo.");

        Activo = false;
        return Result.Exitoso();
    }

    public new Result Eliminar(Guid usuarioId, DateTime fechaUtc)
    {
        if (Activo)
            return new Error("Equipamiento.DebeDesactivarsePrimero", "Un equipo activo no puede eliminarse; desactívelo primero.");

        base.Eliminar(usuarioId, fechaUtc);
        return Result.Exitoso();
    }
}
