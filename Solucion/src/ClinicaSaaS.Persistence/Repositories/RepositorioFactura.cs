using ClinicaSaaS.Domain.Billing;
using Microsoft.EntityFrameworkCore;

namespace ClinicaSaaS.Persistence.Repositories;

/// <summary>
/// Especialización de RepositorioBase para Factura. Necesaria por el mismo motivo que
/// RepositorioDoctor/RepositorioHistorialClinico: Detalles y Pagos son colecciones de Entities
/// internas del agregado (Fase 2) que RepositorioBase.ObtenerPorIdAsync no carga por defecto.
/// </summary>
public sealed class RepositorioFactura(ClinicaSaaSDbContext contexto) : RepositorioBase<Factura>(contexto)
{
    public override async Task<Factura?> ObtenerPorIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await DbSet.Include(f => f.Detalles).Include(f => f.Pagos)
            .FirstOrDefaultAsync(f => f.Id == id, cancellationToken);

    public override async Task<IReadOnlyList<Factura>> ListarAsync(
        System.Linq.Expressions.Expression<Func<Factura, bool>> predicado, CancellationToken cancellationToken = default) =>
        await DbSet.Include(f => f.Detalles).Include(f => f.Pagos)
            .Where(predicado)
            .ToListAsync(cancellationToken);
}