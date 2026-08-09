using System.Security.Claims;
using ClinicaSaaS.Application.Common.Interfaces;
using Microsoft.AspNetCore.Http;

namespace ClinicaSaaS.Infrastructure.Identity;

/// <summary>
/// Implementación real de <see cref="ICurrentUserContext"/> y <see cref="ITenantContext"/>,
/// reemplazando los stubs de diseño de Fase 3 (DesignTimeCurrentUserContext/
/// DesignTimeTenantContext en Persistence, que ahora quedan reservados exclusivamente para
/// las herramientas `dotnet ef`). Una sola clase implementa las dos interfaces a propósito:
/// ambas leen del mismo <see cref="ClaimsPrincipal"/>, así que separarlas en dos servicios
/// solo duplicaría la lectura de HttpContext sin ningún beneficio.
///
/// NOTA CRÍTICA SOBRE BLAZOR SERVER: <see cref="IHttpContextAccessor.HttpContext"/> solo está
/// garantizado durante la conexión HTTP inicial (negociación de SignalR / primer render) — una
/// vez que el circuito queda establecido sobre WebSockets, HttpContext puede volverse null en
/// eventos posteriores (timers, callbacks tardíos). Por eso esta clase captura los valores UNA
/// SOLA VEZ en el constructor (se registra Scoped = una instancia por circuito/usuario, ver
/// InfrastructureServiceCollectionExtensions) y los cachea en campos de solo lectura — nunca
/// vuelve a tocar HttpContext después de construirse.
///
/// NOTA SOBRE FASE 5: hoy (Fase 4) no existe todavía ASP.NET Core Identity ni el middleware de
/// autenticación, así que en la práctica el usuario nunca estará autenticado y esta clase
/// siempre devolverá UsuarioId/ClinicaId nulos — eso es correcto y esperado. Lo que deja lista
/// esta clase es la LECTURA de claims; la Fase 5 solo tiene que asegurarse de emitir los claims
/// con los nombres que esta clase ya espera (ClaimTypes.NameIdentifier y "clinica_id") al
/// iniciar sesión, sin tener que volver a tocar Persistence, Application ni los interceptores
/// de auditoría que dependen de estas dos interfaces.
/// </summary>
public sealed class HttpUserContext : ICurrentUserContext, ITenantContext
{
    private const string ClaimClinicaId = "clinica_id";

    public Guid? UsuarioId { get; }
    public string? DireccionIp { get; }
    public string? Dispositivo { get; }
    public Guid? ClinicaId { get; }

    public HttpUserContext(IHttpContextAccessor httpContextAccessor)
    {
        var httpContext = httpContextAccessor.HttpContext;
        var usuario = httpContext?.User;

        UsuarioId = ObtenerClaimComoGuid(usuario, ClaimTypes.NameIdentifier);
        ClinicaId = ObtenerClaimComoGuid(usuario, ClaimClinicaId);
        DireccionIp = httpContext?.Connection.RemoteIpAddress?.ToString();

        var userAgent = httpContext?.Request.Headers.UserAgent.ToString();
        Dispositivo = string.IsNullOrEmpty(userAgent) ? null : userAgent;
    }

    private static Guid? ObtenerClaimComoGuid(ClaimsPrincipal? usuario, string tipoClaim)
    {
        var valor = usuario?.FindFirstValue(tipoClaim);
        return Guid.TryParse(valor, out var id) ? id : null;
    }
}
