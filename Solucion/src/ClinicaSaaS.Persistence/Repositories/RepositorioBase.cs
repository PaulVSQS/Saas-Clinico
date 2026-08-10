using System.Linq.Expressions;
using ClinicaSaaS.Application.Common.Interfaces;
using ClinicaSaaS.SharedKernel.Common;
using Microsoft.EntityFrameworkCore;

namespace ClinicaSaaS.Persistence.Repositories;

/// <summary>
/// Implementación única de <see cref="IRepositorio{TEntity}"/> para todos los Aggregate Roots
/// — no hay una clase de repositorio por cada agregado (Clinica, Paciente, Doctor, etc.) porque
/// hasta ahora ningún caso de uso necesita una consulta que no pueda expresarse componiendo
/// <see cref="Consultar"/> desde Application (Where/Include/OrderBy), o con
/// <see cref="PrimeroOPredeterminadoAsync"/>/<see cref="ListarAsync"/> (Fase 5) cuando necesita
/// ejecutar la consulta de forma async. Si en Fase 6 algún módulo necesita una consulta imposible
/// de expresar así (ej. SQL crudo con hints de rendimiento), la solución es agregar una interfaz
/// específica en Application que herede de <see cref="IRepositorio{TEntity}"/>
/// (ej. IPacienteRepositorio) con el método adicional, e implementarla en este mismo namespace
/// heredando de esta clase — no reescribir el CRUD base.
/// </summary>
public class RepositorioBase<TEntity>(ClinicaSaaSDbContext contexto) : IRepositorio<TEntity>
    where TEntity : EntityBase
{
    protected readonly ClinicaSaaSDbContext Contexto = contexto;
    protected readonly DbSet<TEntity> DbSet = contexto.Set<TEntity>();

    public virtual async Task<TEntity?> ObtenerPorIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await DbSet.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

    public virtual IQueryable<TEntity> Consultar() => DbSet.AsQueryable();

    public virtual async Task AgregarAsync(TEntity entidad, CancellationToken cancellationToken = default) =>
        await DbSet.AddAsync(entidad, cancellationToken);

    public virtual async Task<TEntity?> PrimeroOPredeterminadoAsync(
        Expression<Func<TEntity, bool>> predicado, CancellationToken cancellationToken = default) =>
        await DbSet.FirstOrDefaultAsync(predicado, cancellationToken);

    public virtual async Task<IReadOnlyList<TEntity>> ListarAsync(
        Expression<Func<TEntity, bool>> predicado, CancellationToken cancellationToken = default) =>
        await DbSet.Where(predicado).ToListAsync(cancellationToken);
}
