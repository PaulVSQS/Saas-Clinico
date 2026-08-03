namespace ClinicaSaaS.SharedKernel.Abstractions;

/// <summary>
/// Contrato de soft delete universal.
/// REGLA: Ninguna entidad de negocio se elimina físicamente.
/// El método Eliminar() de las entidades setea EstaEliminado = true,
/// lo que EF Core convierte en un UPDATE (nunca en DELETE).
/// </summary>
public interface ISoftDeletable
{
    bool EstaEliminado { get; }
    DateTime? FechaEliminacion { get; }
    Guid? EliminadoPorUsuarioId { get; }
}
