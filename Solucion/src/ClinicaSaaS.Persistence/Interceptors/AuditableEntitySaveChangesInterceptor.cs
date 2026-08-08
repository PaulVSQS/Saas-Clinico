using ClinicaSaaS.Application.Common.Interfaces;
using ClinicaSaaS.SharedKernel.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace ClinicaSaaS.Persistence.Interceptors;

/// <summary>
/// Rellena FechaCreacion/CreadoPorUsuarioId (al insertar) y FechaModificacion/
/// ModificadoPorUsuarioId (al actualizar) en toda entidad AuditableEntity, sin que ningún
/// caso de uso tenga que acordarse de hacerlo. Requiere que SharedKernel exponga
/// [InternalsVisibleTo("ClinicaSaaS.Persistence")] — EstablecerAuditoriaCreacion/
/// EstablecerAuditoriaModificacion son 'internal' a propósito para que solo este interceptor
/// (nunca Application ni Web) pueda invocarlos directamente.
/// </summary>
public sealed class AuditableEntitySaveChangesInterceptor(ICurrentUserContext currentUserContext) : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        AplicarAuditoria(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        AplicarAuditoria(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void AplicarAuditoria(DbContext? context)
    {
        if (context is null) return;

        var usuarioId = currentUserContext.UsuarioId ?? Guid.Empty;
        var ahoraUtc = DateTime.UtcNow;

        foreach (var entry in context.ChangeTracker.Entries<AuditableEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.EstablecerAuditoriaCreacion(usuarioId, ahoraUtc);
                    break;
                case EntityState.Modified:
                    entry.Entity.EstablecerAuditoriaModificacion(usuarioId, ahoraUtc);
                    break;
            }
        }
    }
}
