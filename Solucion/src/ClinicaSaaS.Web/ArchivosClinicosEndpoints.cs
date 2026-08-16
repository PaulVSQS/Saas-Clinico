using ClinicaSaaS.Application.HistorialesClinicos;
using ClinicaSaaS.Infrastructure.Identity;

namespace ClinicaSaaS.Web;

/// <summary>
/// Endpoint de descarga de archivos clínicos, deliberadamente FUERA de Blazor — mismo motivo
/// documentado en AuthEndpoints.cs: Blazor Server no puede empujarle al navegador una descarga
/// de archivo a través del circuito SignalR, hace falta una respuesta HTTP real. Nunca acepta
/// una ruta de disco arbitraria del cliente: todo pasa por
/// IArchivoClinicoService.DescargarAsync, que valida que el archivo pertenezca a ese paciente
/// (y por lo tanto a la clínica del usuario autenticado, vía el filtro global de tenant).
/// </summary>
internal static class ArchivosClinicosEndpoints
{
    public static IEndpointRouteBuilder MapArchivosClinicosEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/archivos-clinicos/{pacienteId:guid}/{archivoId:guid}", DescargarAsync)
            .RequireAuthorization(ClinicaSaaSAuthorizationPolicies.AdminClinica);

        return endpoints;
    }

    private static async Task<IResult> DescargarAsync(
        Guid pacienteId, Guid archivoId, IArchivoClinicoService archivoClinicoService, CancellationToken cancellationToken)
    {
        var resultado = await archivoClinicoService.DescargarAsync(pacienteId, archivoId, cancellationToken);
        if (resultado.EsFallido)
            return Results.NotFound();

        var archivo = resultado.Value;
        return Results.File(archivo.Contenido, archivo.TipoContenido, archivo.NombreOriginal);
    }
}