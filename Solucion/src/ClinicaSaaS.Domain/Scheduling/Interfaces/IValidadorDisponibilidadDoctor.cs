namespace ClinicaSaaS.Domain.Scheduling.Interfaces;

/// <summary>
/// Domain Service (interfaz). Verificar que un doctor no tenga dos citas simultáneas requiere
/// consultar OTRAS instancias de Cita fuera del agregado que se está creando/modificando —algo
/// que ninguna entidad de dominio puede hacer por sí sola sin acceso a un repositorio. Por eso
/// es una interfaz en el Domain (el "qué"), implementada en Persistence/Infrastructure (el
/// "cómo", con una consulta SQL eficiente) e inyectada en el Domain Service o Application
/// Handler que orquesta la creación de una Cita. SQL Server no soporta EXCLUDE nativo
/// (a diferencia de PostgreSQL), así que esta validación no puede vivir solo como constraint
/// de base de datos.
/// </summary>
public interface IValidadorDisponibilidadDoctor
{
    Task<bool> EstaDisponibleAsync(
        Guid doctorId, DateTime fechaHoraInicio, DateTime fechaHoraFin, Guid? citaAExcluirId, CancellationToken cancellationToken);
}
