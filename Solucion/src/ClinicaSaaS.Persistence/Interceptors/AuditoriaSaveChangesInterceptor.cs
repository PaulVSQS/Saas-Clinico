using System.Text.Json;
using ClinicaSaaS.Application.Common.Interfaces;
using ClinicaSaaS.Persistence.Audit;
using ClinicaSaaS.SharedKernel.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace ClinicaSaaS.Persistence.Interceptors;

/// <summary>
/// Escribe una fila en Audit.Auditoria por cada entidad creada/modificada/eliminada
/// (soft-delete) en el SaveChanges. Es insert-only y nunca participa del mismo SaveChanges
/// como para fallar la transacción de negocio por un problema de bitácora — se agregan las
/// filas de auditoría al MISMO ChangeTracker antes de que se ejecute el guardado real, así
/// viajan en la misma transacción (todo o nada) sin necesitar una segunda ronda de SaveChanges.
/// </summary>
public sealed class AuditoriaSaveChangesInterceptor(ICurrentUserContext currentUserContext, ITenantContext tenantContext)
    : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        RegistrarAuditoria(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        RegistrarAuditoria(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void RegistrarAuditoria(DbContext? context)
    {
        if (context is null) return;

        var ahoraUtc = DateTime.UtcNow;
        var entradas = new List<AuditLogEntry>();

        foreach (var entry in context.ChangeTracker.Entries())
        {
            if (entry.Entity is AuditLogEntry) continue; // nunca auditar la propia bitácora
            if (entry.State is not (EntityState.Added or EntityState.Modified)) continue;

            var accion = DeterminarAccion(entry);
            if (accion is null) continue; // Modified sin cambios reales de negocio (ej. solo timestamps)

            Guid? clinicaId = entry.Entity is ITenantEntity tenant ? tenant.ClinicaId : tenantContext.ClinicaId;

            entradas.Add(new AuditLogEntry(
                clinicaId: clinicaId,
                usuarioId: currentUserContext.UsuarioId,
                accion: accion,
                entidadNombre: entry.Entity.GetType().Name,
                entidadId: entry.Entity is IEntity conId ? conId.Id.ToString() : "N/D",
                datosAnteriores: accion == "Update" || accion == "Delete" ? SerializarValoresOriginales(entry) : null,
                datosNuevos: accion == "Create" || accion == "Update" ? SerializarValoresActuales(entry) : null,
                fechaUtc: ahoraUtc,
                direccionIp: currentUserContext.DireccionIp,
                dispositivo: currentUserContext.Dispositivo));
        }

        if (entradas.Count > 0)
            context.AddRange(entradas);
    }

    private static string? DeterminarAccion(EntityEntry entry)
    {
        if (entry.State == EntityState.Added) return "Create";

        // Modified: distinguir "eliminación lógica" de una actualización normal de negocio.
        var estaEliminadoProp = entry.Properties.FirstOrDefault(p => p.Metadata.Name == "EstaEliminado");
        if (estaEliminadoProp is not null &&
            estaEliminadoProp.OriginalValue is false &&
            estaEliminadoProp.CurrentValue is true)
        {
            return "Delete";
        }

        return entry.Properties.Any(p => p.IsModified) ? "Update" : null;
    }

    private static string SerializarValoresActuales(EntityEntry entry) =>
        JsonSerializer.Serialize(entry.Properties.ToDictionary(p => p.Metadata.Name, p => p.CurrentValue));

    private static string SerializarValoresOriginales(EntityEntry entry) =>
        JsonSerializer.Serialize(entry.Properties.Where(p => p.IsModified).ToDictionary(p => p.Metadata.Name, p => p.OriginalValue));
}
