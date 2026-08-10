using System.Linq.Expressions;
using ClinicaSaaS.SharedKernel.Common;

namespace ClinicaSaaS.Application.Common.Interfaces;

/// <summary>
/// Contrato de acceso a datos genérico para Aggregate Roots. DECISIÓN DE DISEÑO: se expone
/// <see cref="Consultar"/> devolviendo <see cref="IQueryable{TEntity}"/> en vez de métodos
/// específicos por cada filtro posible (ObtenerActivos, ObtenerPorNombre, etc.) — Application
/// compone la consulta (Where/Include/OrderBy) caso por caso en cada handler de Fase 6, y
/// Persistence sigue siendo la única capa que sabe traducir eso a SQL. Esto evita que
/// Application termine importando EF Core directamente para poder hacer un filtro que este
/// contrato genérico no previó.
/// No incluye Actualizar/Modificar: una entidad obtenida vía <see cref="ObtenerPorIdAsync"/> ya
/// queda bajo seguimiento (tracking) del ORM — modificarla con sus propios métodos de dominio y
/// llamar <see cref="IUnitOfWork.GuardarCambiosAsync"/> es suficiente. Tampoco incluye Eliminar:
/// el soft-delete es un método de dominio (<c>Entidad.Eliminar(...)</c>, ver SoftDeleteEntity en
/// SharedKernel) — este repositorio nunca ejecuta un DELETE físico.
/// </summary>
public interface IRepositorio<TEntity> where TEntity : EntityBase
{
    /// <summary>
    /// Busca por Id respetando los Global Query Filters de tenant y soft-delete ya configurados
    /// en el DbContext (Fase 3) — nunca devuelve una entidad de otra clínica ni una eliminada.
    /// </summary>
    Task<TEntity?> ObtenerPorIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Punto de partida para consultas compuestas por el llamador (Where, Include, OrderBy,
    /// paginación). Ya trae aplicados los Global Query Filters del DbContext.
    /// </summary>
    IQueryable<TEntity> Consultar();

    /// <summary>
    /// Pone en seguimiento una entidad nueva (Aggregate Root completo, con sus Entities/Value
    /// Objects internos ya armados). No persiste por sí sola — requiere
    /// <see cref="IUnitOfWork.GuardarCambiosAsync"/> para llegar a la base de datos, dentro de la
    /// misma unidad de trabajo que cualquier otro cambio del caso de uso.
    /// </summary>
    Task AgregarAsync(TEntity entidad, CancellationToken cancellationToken = default);

    /// <summary>
    /// Primer resultado que cumple <paramref name="predicado"/>, o null. Se agrega en Fase 5
    /// (junto con <see cref="ListarAsync"/>) porque Application necesita ejecutar consultas
    /// async por criterio propio (ej. buscar Usuario por Email al autenticar) SIN poder llamar
    /// FirstOrDefaultAsync/ToListAsync de EF Core directamente sobre el IQueryable de
    /// <see cref="Consultar"/> — esos son extension methods del paquete EF Core, que Application
    /// no referencia a propósito. Mantiene la misma regla que el resto de la interfaz: Persistence
    /// es la única capa que sabe ejecutar la consulta contra la base de datos.
    /// </summary>
    Task<TEntity?> PrimeroOPredeterminadoAsync(Expression<Func<TEntity, bool>> predicado, CancellationToken cancellationToken = default);

    /// <summary>Todos los resultados que cumplen <paramref name="predicado"/>. Ver <see cref="PrimeroOPredeterminadoAsync"/>.</summary>
    Task<IReadOnlyList<TEntity>> ListarAsync(Expression<Func<TEntity, bool>> predicado, CancellationToken cancellationToken = default);
}
