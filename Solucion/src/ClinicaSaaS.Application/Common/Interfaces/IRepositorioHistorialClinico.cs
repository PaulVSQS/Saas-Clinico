namespace ClinicaSaaS.Application.Common.Interfaces;

/// <summary>
/// Especialización de IRepositorio&lt;HistorialClinico&gt; — la UI nunca navega por el Id del
/// historial directamente (no tiene sentido de negocio propio fuera del paciente al que
/// pertenece), siempre parte del PacienteId. Mismo criterio recomendado en el comentario de
/// RepositorioBase para consultas que el contrato genérico no puede expresar.
/// </summary>
public interface IRepositorioHistorialClinico : IRepositorio<ClinicaSaaS.Domain.Clinical.HistorialClinico>
{
    Task<ClinicaSaaS.Domain.Clinical.HistorialClinico?> ObtenerPorPacienteIdAsync(Guid pacienteId, CancellationToken cancellationToken = default);
}