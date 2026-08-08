using ClinicaSaaS.Application.Common.Interfaces;

namespace ClinicaSaaS.Persistence.DesignTime;

/// <summary>
/// Implementaciones mínimas "de arranque" para que la solución compile y funcione end-to-end
/// HOY, antes de que exista la Fase 4/Infrastructure (que implementará la lectura real del
/// usuario/tenant desde el circuito de Blazor Server). Las usan tanto
/// ClinicaSaaSDbContextFactory (herramientas `dotnet ef`) como Program.cs (arranque real de
/// la app) — están marcadas 'public' solo para eso, pero deben reemplazarse por las
/// implementaciones reales de Infrastructure en cuanto se construya la Fase 4; no implementan
/// ninguna lógica de negocio, solo devuelven "sin usuario / sin tenant".
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
