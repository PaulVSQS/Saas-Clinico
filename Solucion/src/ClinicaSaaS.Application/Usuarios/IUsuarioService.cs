using ClinicaSaaS.Domain.Security.Enums;
using ClinicaSaaS.SharedKernel.Results;

namespace ClinicaSaaS.Application.Usuarios;

/// <summary>
/// Vista de un Usuario hacia afuera de Application — deliberadamente NO se expone el
/// Aggregate Root ni sus Value Objects (Email) a Web: este DTO es lo único que Blazor conoce.
/// Se usa el mismo DTO para lista y detalle porque los campos relevantes no difieren
/// significativamente entre ambos casos.
/// </summary>
public sealed record UsuarioDto(
    Guid Id,
    string Email,
    string NombreCompleto,
    string? Telefono,
    bool EsSuperAdminSaaS,
    bool Activo);

/// <summary>
/// Vista plana de una membresía de clínica-rol para un usuario.
/// ClinicaNombre se obtiene haciendo join en memoria (número de clínicas es pequeño),
/// para no introducir una dependencia de Application en la proyección de EF Core.
/// </summary>
public sealed record UsuarioClinicaRolDto(
    Guid Id,
    Guid ClinicaId,
    string ClinicaNombre,
    RolClinica Rol,
    DateTime FechaAsignacion);

public sealed record RegistrarUsuarioRequest(string Email, string NombreCompleto, string Password);

public sealed record ActualizarDatosPersonalesRequest(string NombreCompleto, string? Telefono);

public sealed record AsignarRolRequest(Guid UsuarioId, Guid ClinicaId, RolClinica Rol);

/// <summary>
/// Módulo 2 de Fase 6 (Módulos del Negocio) — gestión de usuarios de la plataforma.
/// Opera a nivel SaaS (no de una clínica individual), por eso todas las páginas de este
/// módulo se protegen con la Policy SuperAdminSaaS (igual que Clínicas en el Módulo 1).
/// Usuario es identidad global: no tiene filtro de tenant, a diferencia de UsuarioClinicaRol
/// que sí tiene filtro de tenant para el aislamiento de membresías por clínica.
/// </summary>
public interface IUsuarioService
{
    Task<IReadOnlyList<UsuarioDto>> ListarAsync(CancellationToken cancellationToken = default);

    Task<UsuarioDto?> ObtenerAsync(Guid usuarioId, CancellationToken cancellationToken = default);

    Task<Result<Guid>> RegistrarAsync(RegistrarUsuarioRequest request, CancellationToken cancellationToken = default);

    Task<Result> ActualizarDatosPersonalesAsync(Guid usuarioId, ActualizarDatosPersonalesRequest request, CancellationToken cancellationToken = default);

    Task<Result> DesactivarAsync(Guid usuarioId, CancellationToken cancellationToken = default);

    Task<Result> ReactivarAsync(Guid usuarioId, CancellationToken cancellationToken = default);

    /// <summary>Exige que el usuario esté inactivo primero — mismo criterio que Usuario.Eliminar (Domain).</summary>
    Task<Result> EliminarAsync(Guid usuarioId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Devuelve las membresías activas del usuario (UsuarioClinicaRol no eliminados).
    /// El filtro de tenant del DbContext cubre el aislamiento cuando hay un ClinicaId activo;
    /// como SuperAdmin SaaS no tiene tenant, el filtro pasa sin restricción (correcto: ve todo).
    /// </summary>
    Task<IReadOnlyList<UsuarioClinicaRolDto>> ListarMembresiasAsync(Guid usuarioId, CancellationToken cancellationToken = default);

    Task<Result> AsignarRolAsync(AsignarRolRequest request, CancellationToken cancellationToken = default);

    Task<Result> RevocarRolAsync(Guid usuarioClinicaRolId, CancellationToken cancellationToken = default);
}