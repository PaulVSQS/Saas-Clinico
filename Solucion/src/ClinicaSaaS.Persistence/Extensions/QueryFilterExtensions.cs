using ClinicaSaaS.SharedKernel.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace ClinicaSaaS.Persistence.Extensions;

/// <summary>
/// Aplica los Global Query Filters de aislamiento multi-tenant y soft-delete.
/// DECISIÓN DE DISEÑO (se aparta ligeramente de la sugerencia del documento de BD de iterar
/// modelBuilder.Model.GetEntityTypes() por reflexión): en vez de un único bucle genérico que
/// detecta automáticamente qué entidades implementan ITenantEntity/ISoftDeletable, se llama
/// explícitamente método por entidad desde ClinicaSaaSDbContext.OnModelCreating. Es más texto,
/// pero para el filtro que garantiza el aislamiento entre clínicas de una BD médica/financiera
/// se prioriza que cualquier desarrollador pueda leer exactamente qué entidad tiene qué filtro
/// sin tener que razonar sobre reflexión — el riesgo de una fuga de datos entre tenants por un
/// bug silencioso en un bucle reflejado no vale el ahorro de líneas. Queda documentado aquí
/// como la mejora que se evaluó y por qué se descartó.
///
/// NOTA TÉCNICA CRÍTICA: el filtro referencia `context.TenantContext` (una propiedad de la
/// instancia de ClinicaSaaSDbContext), NUNCA un valor capturado de otra fuente. EF Core
/// reconoce las expresiones de filtro que acceden a miembros de la instancia del propio
/// DbContext y las re-evalúa en cada consulta contra la instancia ACTUAL (que en Blazor Server
/// es de vida "scoped", una por circuito/usuario) — así el modelo compilado se cachea una sola
/// vez por tipo de DbContext (como hace EF Core normalmente) sin que eso "congele" el
/// ClinicaId de la primera petición para todas las peticiones siguientes.
/// </summary>
internal static class QueryFilterExtensions
{
    public static void AplicarFiltroTenantYSoftDelete<TEntity>(this ModelBuilder modelBuilder, ClinicaSaaSDbContext context)
        where TEntity : class, ITenantEntity, ISoftDeletable
    {
        modelBuilder.Entity<TEntity>().HasQueryFilter(e =>
            !e.EstaEliminado &&
            (!context.TenantContext.ClinicaId.HasValue || e.ClinicaId == context.TenantContext.ClinicaId));
    }

    public static void AplicarFiltroSoloTenant<TEntity>(this ModelBuilder modelBuilder, ClinicaSaaSDbContext context)
        where TEntity : class, ITenantEntity
    {
        modelBuilder.Entity<TEntity>().HasQueryFilter(e =>
            !context.TenantContext.ClinicaId.HasValue || e.ClinicaId == context.TenantContext.ClinicaId);
    }

    public static void AplicarFiltroSoloSoftDelete<TEntity>(this ModelBuilder modelBuilder)
        where TEntity : class, ISoftDeletable
    {
        modelBuilder.Entity<TEntity>().HasQueryFilter(e => !e.EstaEliminado);
    }
}
