namespace ClinicaSaaS.SharedKernel.Abstractions;

/// <summary>
/// Contrato multi-tenant. Toda entidad que pertenezca a una clínica específica
/// debe implementar esta interfaz para que EF Core aplique automáticamente
/// el Global Query Filter de aislamiento de tenant.
/// La doble capa de seguridad (Global Query Filter + RLS de SQL Server) garantiza
/// que ningún dato de una clínica sea visible para otra.
/// </summary>
public interface ITenantEntity : IEntity
{
    Guid ClinicaId { get; }
}
