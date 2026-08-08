using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace ClinicaSaaS.Persistence.DesignTime;

/// <summary>
/// Permite que las herramientas de EF Core (`dotnet ef migrations add`, `dotnet ef database
/// update`) construyan el DbContext SIN levantar todo el host de ASP.NET Core / Blazor Server
/// ni resolver ICurrentUserContext/ITenantContext reales (que hoy no existen — se implementan
/// en la Fase 4/Infrastructure). Solo se usa en tiempo de diseño; en producción, el DbContext
/// se registra en Program.cs con las implementaciones reales.
///
/// Lee la cadena de conexión de appsettings.json del proyecto Web (o de la variable de entorno
/// ConnectionStrings__DefaultConnection) para no duplicar la configuración de conexión en dos
/// lugares distintos de la solución.
/// </summary>
public sealed class ClinicaSaaSDbContextFactory : IDesignTimeDbContextFactory<ClinicaSaaSDbContext>
{
    public ClinicaSaaSDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Path.Combine(Directory.GetCurrentDirectory(), "..", "ClinicaSaaS.Web"))
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? "Server=(localdb)\\MSSQLLocalDB;Database=ClinicaSaaS;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true";

        var optionsBuilder = new DbContextOptionsBuilder<ClinicaSaaSDbContext>();
        optionsBuilder.UseSqlServer(connectionString, sql =>
            sql.MigrationsAssembly(typeof(ClinicaSaaSDbContext).Assembly.FullName));

        return new ClinicaSaaSDbContext(optionsBuilder.Options, new DesignTimeTenantContext());
    }
}
