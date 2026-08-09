using ClinicaSaaS.Application.Common.Interfaces;
using ClinicaSaaS.Persistence.Interceptors;
using ClinicaSaaS.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

// ClinicaSaaSDbContext y UnitOfWork viven en el namespace padre ClinicaSaaS.Persistence — un
// namespace hijo (DependencyInjection) no los ve automáticamente, hace falta el using explícito.
using ClinicaSaaS.Persistence;

namespace ClinicaSaaS.Persistence.DependencyInjection;

/// <summary>
/// Punto único de registro de todo lo que ofrece Persistence: el DbContext (con sus 4
/// interceptores de Fase 3), el repositorio genérico y el Unit of Work de Fase 4. Antes de
/// esta clase, Program.cs armaba el DbContext "a mano" — se movió aquí para que Program.cs no
/// tenga que conocer los interceptores ni el detalle de cómo se conecta a SQL Server, solo que
/// existe una capa de Persistencia y hay que registrarla. Mismo patrón que
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

        return services;
    }
}
