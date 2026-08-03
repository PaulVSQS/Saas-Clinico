namespace ClinicaSaaS.SharedKernel.Abstractions;

/// <summary>
/// Contrato de auditoría para entidades que requieren trazabilidad completa de creación y modificación.
/// Implementado como interfaz para permitir aplicación automática de Global Query Filters
/// y llenado automático mediante SaveChangesInterceptor.
/// </summary>
public interface IAuditableEntity : IEntity
{
    DateTime FechaCreacion { get; }
    Guid CreadoPorUsuarioId { get; }
    DateTime? FechaModificacion { get; }
    Guid? ModificadoPorUsuarioId { get; }
}
