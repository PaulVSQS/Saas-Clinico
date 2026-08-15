using ClinicaSaaS.Application.Common.Interfaces;
using ClinicaSaaS.Domain.Clinical;
using ClinicaSaaS.Domain.Personal;
using ClinicaSaaS.Domain.Scheduling.Interfaces;
using ClinicaSaaS.Persistence.Interceptors;
using ClinicaSaaS.Persistence.Repositories;
using ClinicaSaaS.Persistence.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

// ClinicaSaaSDbContext y UnitOfWork viven en el namespace padre ClinicaSaaS.Persistence — un
// namespace hijo (DependencyInjection) no los ve automáticamente, hace falta el using explícito.
using ClinicaSaaS.Persistence;

namespace ClinicaSaaS.Persistence.DependencyInjection;

/// <summary>
/// Punto único de registro de todo lo que ofrece Persistence: el DbContext (con sus 4
/// interceptores de Fase 3), el repositorio genérico (con las especializaciones de Doctor y
/// HistorialClinico, que necesitan Include de colecciones hijas), el Unit of Work de Fase 4, y
/// los Domain Services cuya implementación requiere acceso a datos
/// (ValidadorDisponibilidadDoctor, Fase 6 Módulo 8). Mismo patrón que
/// InfrastructureServiceCollectionExtensions.AddInfrastructure() en el otro proyecto.
/// </summary>
public static class PersistenceServiceCollectionExtensions
{
    public static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<AuditableEntitySaveChangesInterceptor>();
        services.AddScoped<AuditoriaSaveChangesInterceptor>();
        services.AddScoped<FacturaTotalesSaveChangesInterceptor>();
        services.AddScoped<TenantSessionContextConnectionInterceptor>();

        services.AddDbContext<ClinicaSaaSDbContext>((serviceProvider, options) =>
        {
            options.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection"),
                sql => sql.MigrationsAssembly(typeof(ClinicaSaaSDbContext).Assembly.FullName));

            options.AddInterceptors(
                serviceProvider.GetRequiredService<AuditableEntitySaveChangesInterceptor>(),
                serviceProvider.GetRequiredService<AuditoriaSaveChangesInterceptor>(),
                serviceProvider.GetRequiredService<FacturaTotalesSaveChangesInterceptor>(),
                serviceProvider.GetRequiredService<TenantSessionContextConnectionInterceptor>());
        });

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped(typeof(IRepositorio<>), typeof(RepositorioBase<>));

        // Doctor y HistorialClinico necesitan su propio repositorio (ver RepositorioDoctor /
        // RepositorioHistorialClinico) porque ambos tienen una colección de Entities hijas que
        // RepositorioBase no puede cargar de forma genérica. Se registran DESPUÉS del genérico a
        // propósito: en el contenedor de DI de .NET, cuando hay más de un registro para el mismo
        // tipo de servicio, gana el último — así el resto de los agregados sigue resolviendo a
        // RepositorioBase<T> sin cambios.
        services.AddScoped<IRepositorio<Doctor>, RepositorioDoctor>();
        services.AddScoped<IRepositorio<HistorialClinico>, RepositorioHistorialClinico>();
        services.AddScoped<IRepositorioHistorialClinico, RepositorioHistorialClinico>();

        // Módulo 8 de Fase 6: validar que un doctor no quede con dos citas simultáneas requiere
        // consultar otras Citas — el Domain Service (Scheduling.Interfaces) se implementa aquí.
        services.AddScoped<IValidadorDisponibilidadDoctor, ValidadorDisponibilidadDoctor>();

        return services;
    }
}