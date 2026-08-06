namespace ClinicaSaaS.Domain.Security.Enums;

/// <summary>
/// Catálogo cerrado de roles asignables dentro de una clínica (Security.Roles en BD).
/// Es un enum y no una entidad porque el catálogo es fijo y conocido en tiempo de compilación —
/// agregar un rol nuevo es un cambio de código, no un dato que un Admin de clínica pueda crear.
/// EsSuperAdminSaaS NO está aquí: vive como flag en Usuario porque no es un rol ligado a una
/// clínica, sino un control de plataforma completo (ver justificación en el documento de BD).
/// </summary>
public enum RolClinica
{
    AdminClinica = 1,
    Recepcion = 2,
    Doctor = 3,
    Empleado = 4,
    Auditor = 5
}
