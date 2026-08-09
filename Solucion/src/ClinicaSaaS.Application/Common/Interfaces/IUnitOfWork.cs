namespace ClinicaSaaS.Application.Common.Interfaces;

/// <summary>
/// Unidad de trabajo: confirma en una sola transacción todos los cambios hechos a través de
/// uno o varios <see cref="IRepositorio{TEntity}"/> durante el mismo caso de uso (ej. crear una
/// Cita y, en el mismo caso de uso, actualizar la disponibilidad del Consultorio). Separado de
/// <see cref="IRepositorio{TEntity}"/> a propósito: los repositorios solo ponen entidades bajo
/// seguimiento, nunca deciden cuándo confirmar — eso es explícitamente responsabilidad del
/// caso de uso que orquesta la operación completa.
/// </summary>
public interface IUnitOfWork
{
    Task<int> GuardarCambiosAsync(CancellationToken cancellationToken = default);
}
