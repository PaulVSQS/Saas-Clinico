using ClinicaSaaS.Application.Common.Interfaces;
using ClinicaSaaS.Infrastructure.Identity;
using ClinicaSaaS.Infrastructure.Storage;
using ClinicaSaaS.Infrastructure.Time;
using Microsoft.Extensions.DependencyInjection;

namespace ClinicaSaaS.Infrastructure.DependencyInjection;

/// <summary>
/// Punto único de registro de todo lo que ofrece Infrastructure (Fase 4): las
/// implementaciones reales de usuario/tenant actual, fecha/hora y almacenamiento de archivos.
/// Program.cs solo necesita llamar <c>builder.Services.AddInfrastructure()</c> — no debe conocer
/// ni importar clases concretas de este proyecto directamente.
/// </summary>
public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();

        // HttpUserContext implementa dos interfaces (ICurrentUserContext e ITenantContext) desde
        // una sola instancia por circuito — se registra una vez como Scoped y ambas interfaces
        // resuelven a esa misma instancia, para no leer el HttpContext dos veces por circuito.
        services.AddScoped<HttpUserContext>();
        services.AddScoped<ICurrentUserContext>(sp => sp.GetRequiredService<HttpUserContext>());
        services.AddScoped<ITenantContext>(sp => sp.GetRequiredService<HttpUserContext>());

        services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();
        services.AddSingleton<IFileStorageService, LocalFileStorageService>();

        // Fase 5 — PasswordHasher<TUser> es thread-safe y sin estado propio por request, por
        // eso Singleton (igual criterio que SystemDateTimeProvider/LocalFileStorageService).
        services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();

        return services;
    }
}
