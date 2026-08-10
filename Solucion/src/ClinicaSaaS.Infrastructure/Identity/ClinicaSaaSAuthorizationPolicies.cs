using ClinicaSaaS.Domain.Security.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace ClinicaSaaS.Infrastructure.Identity;

/// <summary>
/// Registra las Policies de autorización (Fase 5) de forma DATA-DRIVEN a partir del enum
/// RolClinica (Fase 2) — una Policy "Rol:{valor}" por cada rol del catálogo, en vez de
/// escribirlas una por una a mano. Así, si algún día se agrega un valor al enum RolClinica,
/// su Policy correspondiente aparece sola, sin tener que acordarse de venir a este archivo.
/// Un SuperAdminSaaS siempre pasa cualquier Policy de rol — es un control de plataforma por
/// encima de los roles de clínica (ver Usuario.EsSuperAdminSaaS).
/// </summary>
public static class ClinicaSaaSAuthorizationPolicies
{
    /// <summary>Nombre de la Policy que exige ser SuperAdmin SaaS.</summary>
    public const string SuperAdminSaaS = "SuperAdminSaaS";

    /// <summary>Tipo de claim (no estándar) que marca a un usuario como SuperAdmin SaaS.</summary>
    public const string ClaimSuperAdminSaaS = "superadmin_saas";

    /// <summary>Nombre de la Policy asociada a un RolClinica — ej. NombrePolicyDeRol(RolClinica.Doctor) == "Rol:Doctor".</summary>
    public static string NombrePolicyDeRol(RolClinica rol) => $"Rol:{rol}";

    public static IServiceCollection AddClinicaSaaSAuthorizationPolicies(this IServiceCollection services)
    {
        var builder = services.AddAuthorizationBuilder()
            .AddPolicy(SuperAdminSaaS, policy => policy.RequireClaim(ClaimSuperAdminSaaS, "true"));

        foreach (var rol in Enum.GetValues<RolClinica>())
        {
            var nombreRol = rol.ToString();
            builder.AddPolicy(NombrePolicyDeRol(rol), policy => policy.RequireAssertion(contexto =>
                contexto.User.HasClaim(ClaimSuperAdminSaaS, "true") ||
                contexto.User.IsInRole(nombreRol)));
        }

        return services;
    }
}
