using ClinicaSaaS.Application.Common.Interfaces;
using ClinicaSaaS.Application.Security;
using Microsoft.Extensions.DependencyInjection;

namespace ClinicaSaaS.Application.DependencyInjection;

/// <summary>
/// Punto único de registro de los servicios de Application (Fase 5: IAutenticacionService).
/// Mismo patrón que AddInfrastructure()/AddPersistence() — Program.cs no debe conocer las
/// clases concretas de este proyecto, solo llamar builder.Services.AddApplication().
/// </summary>
public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IAutenticacionService, AutenticacionService>();
        return services;
    }
}
