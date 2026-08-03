namespace ClinicaSaaS.SharedKernel.Abstractions;

/// <summary>
/// Contrato base para todas las entidades del sistema.
/// Garantiza que toda entidad tenga un identificador único.
/// </summary>
public interface IEntity
{
    Guid Id { get; }
}
