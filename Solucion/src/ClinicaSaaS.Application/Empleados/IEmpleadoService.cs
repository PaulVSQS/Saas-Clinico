using ClinicaSaaS.SharedKernel.Results;

namespace ClinicaSaaS.Application.Empleados;

/// <summary>
/// Vista de un Empleado hacia afuera de Application. Incluye datos del Usuario asociado
/// (nombre, email) resueltos vía join en memoria con IUsuarioService — igual criterio que
/// UsuarioClinicaRolDto en Fase 6/Módulo 2 resolvía el nombre de la clínica con IClinicaService.
/// </summary>
public sealed record EmpleadoDto(
    Guid Id,
    Guid UsuarioId,
    string NombreUsuario,
    string EmailUsuario,
    Guid ClinicaId,
    string NombreClinica,
    string Cargo,
    DateOnly? FechaIngreso,
    bool Activo);

public sealed record RegistrarEmpleadoRequest(Guid UsuarioId, Guid ClinicaId, string Cargo, DateOnly? FechaIngreso);

public sealed record ActualizarDatosEmpleadoRequest(string Cargo, DateOnly? FechaIngreso);

/// <summary>
/// Módulo 3 de Fase 6 — Empleados: personal administrativo/operativo de UNA clínica concreta.
/// A diferencia de Clínica (Módulo 1) y Usuarios (Módulo 2), que son entidades de plataforma
/// protegidas con la Policy SuperAdminSaaS, Empleado SÍ tiene ClinicaId como columna real
/// (implementa ITenantEntity de forma normal) — así que en Web se protege con la Policy de rol
/// "AdminClinica" (Fase 5), que un SuperAdminSaaS también satisface automáticamente.
///
/// Registrar un Empleado requiere un Usuario YA EXISTENTE (Módulo 2) — este módulo no crea
/// usuarios nuevos, solo vincula uno existente a una clínica con un cargo. Combinar "crear
/// usuario + empleado en un solo paso" queda fuera de este módulo a propósito, para no mezclar
/// la responsabilidad de dos módulos distintos.
/// </summary>
public interface IEmpleadoService
{
    Task<IReadOnlyList<EmpleadoDto>> ListarAsync(CancellationToken cancellationToken = default);

    Task<EmpleadoDto?> ObtenerAsync(Guid empleadoId, CancellationToken cancellationToken = default);

    Task<Result<Guid>> RegistrarAsync(RegistrarEmpleadoRequest request, CancellationToken cancellationToken = default);

    Task<Result> ActualizarDatosAsync(Guid empleadoId, ActualizarDatosEmpleadoRequest request, CancellationToken cancellationToken = default);

    Task<Result> DesactivarAsync(Guid empleadoId, CancellationToken cancellationToken = default);

    Task<Result> ReactivarAsync(Guid empleadoId, CancellationToken cancellationToken = default);
}
