namespace ClinicaSaaS.Application.Common.Interfaces;

/// <summary>
/// Abstracción sobre la hora del sistema. Ningún caso de uso ni servicio de Application/
/// Infrastructure debe llamar a <c>DateTime.UtcNow</c> directamente — usar esta interfaz permite
/// congelar/controlar el tiempo en pruebas unitarias (ej. validar que no se pueda agendar una
/// Cita en el pasado, o que un ProcedimientoCatalogo recién creado tenga la fecha esperada) sin
/// depender del reloj real de la máquina que corre el test.
/// Nota: los interceptores de EF Core en Persistence (auditoría, timestamps) siguen usando
/// <c>DateTime.UtcNow</c> directo a propósito — corren fuera de Application, en el límite de
/// infraestructura de persistencia, donde esa abstracción no aporta valor de testeo adicional.
/// </summary>
public interface IDateTimeProvider
{
    DateTime UtcNow { get; }
}
