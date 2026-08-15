using ClinicaSaaS.SharedKernel.Results;

namespace ClinicaSaaS.Application.HistorialesClinicos;

/// <summary>
/// Los 8 campos comunes del Value Object SignosVitales (Domain), aplanados para el formulario —
/// todos opcionales, mismo criterio que el dominio (no todas las consultas registran todos).
/// </summary>
public sealed record SignosVitalesDto(
    decimal? PresionSistolica,
    decimal? PresionDiastolica,
    decimal? TemperaturaCelsius,
    int? FrecuenciaCardiaca,
    int? FrecuenciaRespiratoria,
    decimal? SaturacionOxigeno,
    decimal? PesoKg,
    decimal? TallaCm);

/// <summary>
/// Vista de una entrada del historial hacia afuera de Application. Nunca hay un
/// "ActualizarEntradaRequest" — el dominio no lo permite (ver HistorialClinicoEntrada.cs,
/// append-only), así que esta vista es siempre de solo lectura desde la UI.
/// </summary>
public sealed record EntradaHistorialClinicoDto(
    Guid Id,
    int Version,
    Guid? EntradaAnteriorId,
    Guid DoctorId,
    string NombreDoctor,
    Guid? CitaId,
    string? MotivoConsulta,
    string? Diagnostico,
    string? Tratamiento,
    string? Notas,
    SignosVitalesDto? SignosVitales,
    DateTime FechaRegistro,
    bool EsVersionActual);

public sealed record AgregarEntradaHistorialClinicoRequest(
    Guid PacienteId,
    Guid DoctorId,
    Guid? CitaId,
    string? MotivoConsulta,
    string? Diagnostico,
    string? Tratamiento,
    string? Notas,
    SignosVitalesDto? SignosVitales);

/// <summary>
/// Módulo 9 de Fase 6 — Historia Clínica: el expediente médico versionado de un paciente. Un
/// HistorialClinico se crea de forma perezosa la primera vez que se agrega una entrada — no hay
/// un paso manual de "abrir expediente" separado, respetando igual la relación 1:1 con Paciente
/// que exige la base de datos.
///
/// REGLA DE NEGOCIO QUE ESTE SERVICIO NUNCA ROMPE: no existe ni existirá un método para editar o
/// eliminar una entrada existente — corregir un dato clínico es agregar una entrada nueva. Esto
/// no es una limitación técnica, es la regla de negocio explícita del dominio (ver
/// HistorialClinicoEntrada.cs).
///
/// Mismo criterio de protección que los módulos anteriores: Policy "AdminClinica", con
/// validación de que el doctor pertenezca a la clínica del paciente antes de escribir.
///
/// DELIBERADAMENTE FUERA de este módulo: adjuntar archivos clínicos y registrar procedimientos
/// realizados — ambos métodos ya existen en el agregado HistorialClinico (Fase 2) pero
/// corresponden a los Módulos 10 y 11 del roadmap, no se exponen aquí todavía.
/// </summary>
public interface IHistorialClinicoService
{
    /// <summary>Todas las entradas del paciente, más reciente primero. Lista vacía si el paciente no tiene historial todavía (nunca null).</summary>
    Task<IReadOnlyList<EntradaHistorialClinicoDto>> ListarEntradasPorPacienteAsync(Guid pacienteId, CancellationToken cancellationToken = default);

    Task<Result<Guid>> AgregarEntradaAsync(AgregarEntradaHistorialClinicoRequest request, CancellationToken cancellationToken = default);
}