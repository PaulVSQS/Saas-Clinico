using ClinicaSaaS.SharedKernel.Abstractions;

namespace ClinicaSaaS.SharedKernel.Common;

/// <summary>
/// Clase base para entidades con soft delete.
/// DECISIÓN ARQUITECTÓNICA: El método Eliminar() es el único punto de entrada para
/// marcar una entidad como eliminada. DbSet.Remove() está prohibido en toda la solución.
/// Esto garantiza que FechaEliminacion y EliminadoPorUsuarioId siempre se capturen.
/// EF Core trata la llamada a Eliminar() como un UPDATE (State = Modified), nunca como DELETE.
/// </summary>
public abstract class SoftDeleteEntity : AuditableEntity, ISoftDeletable
{
    protected SoftDeleteEntity() { }

    protected SoftDeleteEntity(Guid id) : base(id) { }

    public bool EstaEliminado { get; private set; }
    public DateTime? FechaEliminacion { get; private set; }
    public Guid? EliminadoPorUsuarioId { get; private set; }

    /// <summary>
    /// Marca la entidad como eliminada lógicamente.
    /// Es el único mecanismo de "eliminación" permitido en el sistema.
    /// </summary>
    protected void Eliminar(Guid usuarioId, DateTime fechaUtc)
    {
        if (EstaEliminado)
            return; // Idempotente: eliminar dos veces no tiene efecto.

        EstaEliminado = true;
        FechaEliminacion = fechaUtc;
        EliminadoPorUsuarioId = usuarioId;
    }
}
