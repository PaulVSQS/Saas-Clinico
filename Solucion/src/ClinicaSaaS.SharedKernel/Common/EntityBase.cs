using ClinicaSaaS.SharedKernel.Abstractions;

namespace ClinicaSaaS.SharedKernel.Common;

/// <summary>
/// Clase base abstracta para todas las entidades del dominio.
/// Encapsula el Id (inmutable una vez asignado) y la colección de Domain Events
/// que permite desacoplar efectos secundarios del núcleo del dominio.
/// El Id se genera en el cliente (Blazor/Application) antes del SaveChanges,
/// lo que permite construir el grafo de objetos completo en memoria antes de persistir.
/// </summary>
public abstract class EntityBase : IEntity
{
    private readonly List<object> _domainEvents = [];

    protected EntityBase() { }

    protected EntityBase(Guid id)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("El Id de una entidad no puede ser un GUID vacío.", nameof(id));

        Id = id;
    }

    public Guid Id { get; private set; }

    public IReadOnlyCollection<object> DomainEvents => _domainEvents.AsReadOnly();

    protected void RaiseEvent(object domainEvent)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);
        _domainEvents.Add(domainEvent);
    }

    public void ClearDomainEvents() => _domainEvents.Clear();
}
