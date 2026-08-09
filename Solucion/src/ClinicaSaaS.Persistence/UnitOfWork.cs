using ClinicaSaaS.Application.Common.Interfaces;

namespace ClinicaSaaS.Persistence;

/// <summary>
/// Implementación directa de <see cref="IUnitOfWork"/>: delega en
/// <see cref="ClinicaSaaSDbContext.SaveChangesAsync"/>, que es exactamente donde ya corren los
/// 4 interceptores de Fase 3 (auditoría, bitácora, totales de Factura, SESSION_CONTEXT) — no
/// hay lógica adicional que agregar aquí, el propósito de esta clase es que Application dependa
/// de <see cref="IUnitOfWork"/> (su propia abstracción) y jamás de EF Core directamente.
/// </summary>
public sealed class UnitOfWork(ClinicaSaaSDbContext contexto) : IUnitOfWork
{
    public Task<int> GuardarCambiosAsync(CancellationToken cancellationToken = default) =>
        contexto.SaveChangesAsync(cancellationToken);
}
