using System.Security.Claims;
using ClinicaSaaS.Application.Common.Interfaces;
using ClinicaSaaS.Infrastructure.Identity;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;

namespace ClinicaSaaS.Web;

/// <summary>
/// Endpoints de autenticación (login/logout) como Minimal API, deliberadamente FUERA de Blazor.
/// <c>HttpContext.SignInAsync</c>/<c>SignOutAsync</c> escriben la cookie en la respuesta HTTP —
/// algo que no se puede hacer de forma confiable desde dentro de un circuito interactivo de
/// Blazor Server, porque para cuando el circuito SignalR está activo la respuesta HTTP original
/// ya se envió. El patrón oficial de .NET 8+ para esto es exactamente este: la página de login
/// se renderiza estática (Components/Pages/Login.razor, sin <c>@rendermode</c>) con un
/// &lt;form&gt; HTML normal que hace POST directo a estos endpoints — no un componente
/// interactivo con @onclick.
/// </summary>
internal static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/Account/Login", LoginAsync);
        endpoints.MapPost("/Account/Logout", LogoutAsync);
        return endpoints;
    }

    private static async Task<IResult> LoginAsync(
        HttpContext context,
        IAntiforgery antiforgery,
        IAutenticacionService autenticacionService,
        IUnitOfWork unitOfWork,
        [FromForm] string email,
        [FromForm] string password,
        [FromForm] string? returnUrl)
    {
        try
        {
            await antiforgery.ValidateRequestAsync(context);
        }
        catch (AntiforgeryValidationException)
        {
            return Results.Redirect("/login?error=token");
        }

        var resultado = await autenticacionService.AutenticarAsync(email, password, context.RequestAborted);

        if (resultado.EsFallido)
            return Results.Redirect("/login?error=credenciales");

        // Persiste cualquier cambio hecho durante la autenticación (ej. un rehash de password
        // si el algoritmo hubiera cambiado de versión, ver AutenticacionService) — es un no-op
        // barato cuando no hubo cambios.
        await unitOfWork.GuardarCambiosAsync(context.RequestAborted);

        var usuario = resultado.Value;
        var principal = ConstruirClaimsPrincipal(usuario);

        await context.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal,
            new AuthenticationProperties { IsPersistent = true });

        var destino = string.IsNullOrWhiteSpace(returnUrl) || !returnUrl.StartsWith('/')
            ? "/"
            : returnUrl;

        return Results.Redirect(destino);
    }

    private static async Task<IResult> LogoutAsync(HttpContext context, IAntiforgery antiforgery)
    {
        try
        {
            await antiforgery.ValidateRequestAsync(context);
        }
        catch (AntiforgeryValidationException)
        {
            return Results.Redirect("/");
        }

        await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return Results.Redirect("/");
    }

    private static ClaimsPrincipal ConstruirClaimsPrincipal(UsuarioAutenticado usuario)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, usuario.UsuarioId.ToString()),
            new(ClaimTypes.Email, usuario.Email),
            new(ClaimTypes.Name, usuario.NombreCompleto)
        };

        if (usuario.EsSuperAdminSaaS)
            claims.Add(new Claim(ClinicaSaaSAuthorizationPolicies.ClaimSuperAdminSaaS, "true"));

        // La "clínica activa" de la sesión es, por ahora, la primera membresía del usuario —
        // suficiente para el caso común (un usuario, una clínica). Un selector de clínica para
        // usuarios con membresías en varias clínicas queda fuera del alcance de Fase 5
        // (seguridad) y se resuelve más adelante como una pantalla que vuelve a llamar a este
        // mismo endpoint con la clínica elegida — la sesión no se rompe mientras tanto, solo
        // usa la primera por defecto.
        var membresiaActiva = usuario.Membresias.FirstOrDefault();
        if (membresiaActiva is not null)
        {
            claims.Add(new Claim("clinica_id", membresiaActiva.ClinicaId.ToString()));
            claims.Add(new Claim(ClaimTypes.Role, membresiaActiva.Rol.ToString()));
        }

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        return new ClaimsPrincipal(identity);
    }
}
