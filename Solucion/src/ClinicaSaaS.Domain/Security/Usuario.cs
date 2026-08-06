using ClinicaSaaS.Domain.Security.ValueObjects;
using ClinicaSaaS.SharedKernel.Common;
using ClinicaSaaS.SharedKernel.Results;

namespace ClinicaSaaS.Domain.Security;

/// <summary>
/// Aggregate Root. Identidad global de acceso — deliberadamente independiente de Clinica:
/// un mismo Usuario puede tener roles en varias clínicas (doctor que atiende en dos consultorios
/// de clínicas distintas), por eso NO implementa ITenantEntity. La asignación de roles por
/// clínica vive en el agregado separado UsuarioClinicaRol (ver justificación allí) — Usuario no
/// mantiene una colección de sus roles para no forzar una carga de todas las clínicas donde
/// trabaja cada vez que se autentica.
/// </summary>
public sealed class Usuario : SoftDeleteEntity
{
    public Email Email { get; private set; }
    public byte[] PasswordHash { get; private set; }
    public string NombreCompleto { get; private set; }
    public string? Telefono { get; private set; }
    public bool EsSuperAdminSaaS { get; private set; }
    public bool Activo { get; private set; }

    private Usuario() { } // EF Core

    private Usuario(Guid id, Email email, byte[] passwordHash, string nombreCompleto) : base(id)
    {
        Email = email;
        PasswordHash = passwordHash;
        NombreCompleto = nombreCompleto;
        Activo = true;
    }

    public static Result<Usuario> Registrar(Guid id, Email email, byte[] passwordHash, string nombreCompleto)
    {
        Guard.ContraGuidVacio(id, nameof(id));
        Guard.ContraNulo(email, nameof(email));

        if (passwordHash is null || passwordHash.Length == 0)
            return new Error("Usuario.PasswordHashRequerido", "El hash de contraseña es requerido.");

        if (string.IsNullOrWhiteSpace(nombreCompleto))
            return new Error("Usuario.NombreRequerido", "El nombre completo es requerido.");

        return new Usuario(id, email, passwordHash, nombreCompleto.Trim());
    }

    public Result CambiarPassword(byte[] nuevoPasswordHash)
    {
        if (nuevoPasswordHash is null || nuevoPasswordHash.Length == 0)
            return new Error("Usuario.PasswordHashRequerido", "El hash de contraseña es requerido.");

        PasswordHash = nuevoPasswordHash;
        return Result.Exitoso();
    }

    public Result ActualizarDatosPersonales(string nombreCompleto, string? telefono)
    {
        if (string.IsNullOrWhiteSpace(nombreCompleto))
            return new Error("Usuario.NombreRequerido", "El nombre completo es requerido.");

        NombreCompleto = nombreCompleto.Trim();
        Telefono = telefono?.Trim();
        return Result.Exitoso();
    }

    /// <summary>
    /// Otorgar/revocar superadmin es una operación de plataforma de altísimo privilegio;
    /// se modela como método explícito (nunca un setter) para que cualquier interceptor de
    /// auditoría pueda distinguirla de una actualización de datos personales corriente.
    /// </summary>
    public Result OtorgarSuperAdminSaaS()
    {
        if (EsSuperAdminSaaS)
            return new Error("Usuario.YaEsSuperAdmin", "El usuario ya es SuperAdmin SaaS.");

        EsSuperAdminSaaS = true;
        return Result.Exitoso();
    }

    public Result RevocarSuperAdminSaaS()
    {
        if (!EsSuperAdminSaaS)
            return new Error("Usuario.NoEsSuperAdmin", "El usuario no es SuperAdmin SaaS.");

        EsSuperAdminSaaS = false;
        return Result.Exitoso();
    }

    public Result Desactivar()
    {
        if (!Activo)
            return new Error("Usuario.YaInactivo", "El usuario ya está inactivo.");

        Activo = false;
        return Result.Exitoso();
    }

    public Result Reactivar()
    {
        if (Activo)
            return new Error("Usuario.YaActivo", "El usuario ya está activo.");

        Activo = true;
        return Result.Exitoso();
    }

    public new Result Eliminar(Guid usuarioId, DateTime fechaUtc)
    {
        if (Activo)
            return new Error("Usuario.DebeDesactivarsePrimero", "Un usuario activo no puede eliminarse; desactívelo primero.");

        base.Eliminar(usuarioId, fechaUtc);
        return Result.Exitoso();
    }
}
