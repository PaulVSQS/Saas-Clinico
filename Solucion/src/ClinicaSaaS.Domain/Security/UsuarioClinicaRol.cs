using ClinicaSaaS.Domain.Security.Enums;
using ClinicaSaaS.SharedKernel.Abstractions;
using ClinicaSaaS.SharedKernel.Common;
using ClinicaSaaS.SharedKernel.Results;

namespace ClinicaSaaS.Domain.Security;

/// <summary>
/// Aggregate Root (no Entity dentro de Usuario ni de Clinica). Es la clásica entidad de
/// asociación DDD entre dos raíces: referencia a Usuario y Clinica solo por Id (nunca por
/// navegación de objeto), tiene su propia identidad, su propio ciclo de vida (asignar/revocar)
/// y su propia regla de invariancia (UsuarioId+ClinicaId+RolId único). Modelarla dentro de
/// Usuario obligaría a cargar TODAS las asignaciones de un usuario (potencialmente muchas
/// clínicas) solo para autenticar en una; modelarla dentro de Clinica tendría el problema
/// simétrico con miles de usuarios por clínica grande.
/// </summary>
public sealed class UsuarioClinicaRol : EntityBase, ITenantEntity, ISoftDeletable
{
    public Guid UsuarioId { get; private set; }
    public Guid ClinicaId { get; private set; }
    public RolClinica Rol { get; private set; }
    public DateTime FechaAsignacion { get; private set; }
    public Guid? AsignadoPorUsuarioId { get; private set; }
    public bool Activo { get; private set; }

    public bool EstaEliminado { get; private set; }
    public DateTime? FechaEliminacion { get; private set; }
    public Guid? EliminadoPorUsuarioId { get; private set; }

    private UsuarioClinicaRol() { } // EF Core

    private UsuarioClinicaRol(Guid id, Guid usuarioId, Guid clinicaId, RolClinica rol,
        Guid? asignadoPorUsuarioId, DateTime fechaUtc) : base(id)
    {
        UsuarioId = usuarioId;
        ClinicaId = clinicaId;
        Rol = rol;
        AsignadoPorUsuarioId = asignadoPorUsuarioId;
        FechaAsignacion = fechaUtc;
        Activo = true;
    }

    public static Result<UsuarioClinicaRol> Asignar(
        Guid id, Guid usuarioId, Guid clinicaId, RolClinica rol, Guid? asignadoPorUsuarioId, DateTime fechaUtc)
    {
        Guard.ContraGuidVacio(id, nameof(id));
        Guard.ContraGuidVacio(usuarioId, nameof(usuarioId));
        Guard.ContraGuidVacio(clinicaId, nameof(clinicaId));

        return new UsuarioClinicaRol(id, usuarioId, clinicaId, rol, asignadoPorUsuarioId, fechaUtc);
    }

    public Result Revocar(Guid usuarioId, DateTime fechaUtc)
    {
        if (!Activo)
            return new Error("UsuarioClinicaRol.YaRevocado", "Esta asignación de rol ya está revocada.");

        Activo = false;
        Eliminar(usuarioId, fechaUtc);
        return Result.Exitoso();
    }

    private void Eliminar(Guid usuarioId, DateTime fechaUtc)
    {
        if (EstaEliminado) return;
        EstaEliminado = true;
        FechaEliminacion = fechaUtc;
        EliminadoPorUsuarioId = usuarioId;
    }
}
