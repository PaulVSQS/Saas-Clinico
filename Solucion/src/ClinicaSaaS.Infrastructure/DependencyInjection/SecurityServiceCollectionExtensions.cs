using ClinicaSaaS.Infrastructure.Identity;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace ClinicaSaaS.Infrastructure.DependencyInjection;

/// <summary>
/// Punto único de registro de la seguridad (Fase 5): autenticación por cookie + Policies de
/// autorización + el estado de autenticación en cascada que necesitan &lt;AuthorizeView&gt;/
/// &lt;AuthorizeRouteView&gt; en los componentes de Blazor. Separado de
/// InfrastructureServiceCollectionExtensions.AddInfrastructure() (Fase 4) a propósito: ese
/// método registra "capacidades técnicas" que no cambian el pipeline HTTP de la app; este
/// método sí lo hace (agrega esquemas de autenticación y políticas), así que se llama aparte
/// en Program.cs para que quede claro qué hace cada uno.
/// </summary>
public static class SecurityServiceCollectionExtensions
{
    public static IServiceCollection AddClinicaSaaSSecurity(this IServiceCollection services)
    {
        // Necesario para que los componentes de Blazor (AuthorizeView, [Authorize] en páginas)
        // reciban el usuario autenticado vía [CascadingParameter] Task<AuthenticationState>,
        // sin tener que envolver manualmente <CascadingAuthenticationState> en App.razor.
        services.AddCascadingAuthenticationState();

        // Autenticación por cookie: la elección correcta para Blazor Server (que ya mantiene
        // estado de sesión vía SignalR) frente a JWT, que está pensado para clientes stateless
        // (SPAs separadas, APIs). El login/logout real ocurre en AuthEndpoints (Web) — nunca
        // dentro de un circuito interactivo de Blazor (ver comentario en ese archivo).
        services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(options =>
            {
                options.LoginPath = "/login";
                options.AccessDeniedPath = "/acceso-denegado";
                options.ExpireTimeSpan = TimeSpan.FromHours(8);
                options.SlidingExpiration = true;
                options.Cookie.HttpOnly = true;
                options.Cookie.SameSite = SameSiteMode.Lax;
                options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
            });

        services.AddClinicaSaaSAuthorizationPolicies();

        return services;
    }
}
