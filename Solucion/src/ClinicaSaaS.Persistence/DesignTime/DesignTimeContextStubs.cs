using ClinicaSaaS.Application.Common.Interfaces;

namespace ClinicaSaaS.Persistence.DesignTime;

/// <summary>
/// Implementaciones mínimas "sin usuario / sin tenant", usadas EXCLUSIVAMENTE por
/// <see cref="ClinicaSaaSDbContextFactory"/> para que las herramientas de diseño de EF Core
/// (`dotnet ef migrations add`, `dotnet ef database update`) puedan construir el DbContext sin
/// depender de un circuito de Blazor Server real (que no existe en ese contexto de ejecución).
/// Desde Fase 4, Program.cs YA NO las usa en el arranque real de la app — ahí se registran
/// las implementaciones reales de Infrastructure (<c>HttpUserContext</c>, vía
/// <c>AddInfrastructure()</c>). Están marcadas 'public' únicamente porque
/// ClinicaSaaSDbContextFactory las instancia directamente; no implementan ninguna lógica de
/// negocio.
/// </summary>
public sealed class DesignTimeCurrentUserContext : ICurrentUserContext
{
    public Guid? UsuarioId => null;
    public string? DireccionIp => null;
    public string? Dispositivo => null;
}

public sealed class DesignTimeTenantContext : ITenantContext
{
    public Guid? ClinicaId => null;
}
