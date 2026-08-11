using ClinicaSaaS.Application.Clinicas;
using ClinicaSaaS.Application.Common.Interfaces;
using ClinicaSaaS.Application.Security;
using ClinicaSaaS.Application.Usuarios;
using Microsoft.Extensions.DependencyInjection;

namespace ClinicaSaaS.Application.DependencyInjection;

/// <summary>
/// Punto único de registro de los servicios de Application (Fase 5: IAutenticacionService;
/// Fase 6: un servicio por módulo de negocio, empezando por IClinicaService). Mismo patrón que
/// AddInfrastructure()/AddPersistence() — Program.cs no debe conocer las clases concretas de
/// este proyecto, solo llamar builder.Services.AddApplication().
/// </summary>
public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IAutenticacionService, AutenticacionService>();
        services.AddScoped<IClinicaService, ClinicaService>();
        // Módulo 2 de Fase 6: Usuarios
        services.AddScoped<IUsuarioService, UsuarioService>();
        return services;
    }
}
