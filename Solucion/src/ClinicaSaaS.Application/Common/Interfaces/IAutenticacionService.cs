using ClinicaSaaS.Domain.Security.Enums;
using ClinicaSaaS.SharedKernel.Results;

namespace ClinicaSaaS.Application.Common.Interfaces;

/// <summary>Una asignación de rol activa del usuario en una clínica concreta.</summary>
public sealed record MembresiaClinica(Guid ClinicaId, RolClinica Rol);

/// <summary>
/// Resultado de una autenticación exitosa: exactamente lo que el llamador (el endpoint de
/// login, en Web) necesita para construir el <c>ClaimsPrincipal</c> de la cookie de sesión —
/// deliberadamente SIN ningún tipo de ASP.NET Core (ClaimsPrincipal, Claim) en esta capa,
/// Application no depende de ASP.NET Core.
/// </summary>
public sealed record UsuarioAutenticado(
    Guid UsuarioId,
    string Email,
    string NombreCompleto,
    bool EsSuperAdminSaaS,
    IReadOnlyList<MembresiaClinica> Membresias);

/// <summary>
/// Caso de uso de autenticación: valida credenciales contra el Usuario del dominio y devuelve
/// sus membresías activas. NO decide nada sobre cookies, claims ni sesiones HTTP — eso es
/// responsabilidad exclusiva de Web (ver AuthEndpoints), que es quien conoce ASP.NET Core.
/// </summary>
public interface IAutenticacionService
{
    Task<Result<UsuarioAutenticado>> AutenticarAsync(string email, string passwordPlano, CancellationToken cancellationToken = default);
}
