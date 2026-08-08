using ClinicaSaaS.Domain.Billing;
using ClinicaSaaS.Domain.Billing.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace ClinicaSaaS.Persistence.Interceptors;

/// <summary>
/// Factura.Subtotal/Total y FacturaDetalle.Subtotal son propiedades CALCULADAS en el dominio
/// (nunca campos editables — ver justificación en Factura.cs de Fase 2), pero la BD aprobada
/// las guarda como columnas físicas para reportería. Este interceptor sincroniza el valor
/// calculado hacia las shadow properties "SubtotalAlmacenado"/"TotalAlmacenado" justo antes de
/// cada SaveChanges — el dominio sigue siendo la única fuente de verdad del cálculo, esto solo
/// empuja el resultado hacia la columna persistida.
/// </summary>
public sealed class FacturaTotalesSaveChangesInterceptor : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        SincronizarTotales(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        SincronizarTotales(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private static void SincronizarTotales(DbContext? context)
    {
        if (context is null) return;

        foreach (var entry in context.ChangeTracker.Entries<Factura>())
        {
            if (entry.State is EntityState.Added or EntityState.Modified)
            {
                entry.Property("SubtotalAlmacenado").CurrentValue = entry.Entity.Subtotal.Monto;
                entry.Property("TotalAlmacenado").CurrentValue = entry.Entity.Total.Monto;
            }
        }

        foreach (var entry in context.ChangeTracker.Entries<FacturaDetalle>())
        {
            if (entry.State is EntityState.Added or EntityState.Modified)
            {
                entry.Property("SubtotalAlmacenado").CurrentValue = entry.Entity.Subtotal.Monto;
            }
        }
    }
}
