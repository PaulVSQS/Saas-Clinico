using ClinicaSaaS.SharedKernel.Results;

namespace ClinicaSaaS.Application.Procedimientos;

/// <summary>
/// Vista de un procedimiento realizado hacia afuera de Application — join en memoria con el
/// catálogo (nombre/código en el momento de la CONSULTA, no necesariamente el actual — si el
/// catálogo cambió de nombre después, este DTO sigue mostrando el nombre vigente hoy, no un
/// snapshot de texto; el snapshot real y legalmente relevante es PrecioAplicado, que si viene de
/// la entidad de dominio) y con Doctor.
/// </summary>
public sealed record ProcedimientoRealizadoDto(
    Guid Id,
    Guid HistorialClinicoEntradaId,
    int VersionEntrada,
    Guid ProcedimientoCatalogoId,
    string CodigoProcedimiento,
    string NombreProcedimiento,
    Guid DoctorId,
    string NombreDoctor,
    string? PiezaDental,
    decimal PrecioAplicado,
    DateTime Fecha,
    string? Notas);

/// <summary>
/// PrecioAplicado es opcional a propósito: si se omite, toma el precio actual del catálogo como
/// snapshot (mismo criterio que el propio comentario del dominio: "puede diferir del catálogo" —
/// implica que un valor explícito, ej. un descuento puntual, es un caso de uso legítimo).
/// </summary>
public sealed record RegistrarProcedimientoRealizadoRequest(
    Guid PacienteId,
    Guid ProcedimientoCatalogoId,
    Guid DoctorId,
    string? PiezaDental,
    decimal? PrecioAplicado,
    string? Notas);

/// <summary>
/// Módulo 11 de Fase 6 — registra que un procedimiento del catálogo se ejecutó de verdad, dentro
/// de la entrada actual del historial clínico de un paciente (mismo criterio de "cuelga de la
/// entrada actual" que Archivos, Módulo 10).
///
/// NO HAY EliminarAsync EN ESTE SERVICIO — a propósito. ProcedimientoRealizado no tiene columnas
/// de soft-delete en su tabla (a diferencia de ArchivoClinico); es evidencia clínica y financiera
/// permanente, mismo espíritu que el snapshot de PrecioAplicado. Corregir un registro mal hecho
/// requeriría tocar el esquema de base de datos — una decisión de producto para la segunda pasada
/// de ajustes, no algo que este módulo deba resolver estirando el dominio sin respaldo del schema.
///
/// Mismo criterio de protección que los módulos anteriores: Policy "AdminClinica".
/// </summary>
public interface IProcedimientoRealizadoService
{
    Task<IReadOnlyList<ProcedimientoRealizadoDto>> ListarPorPacienteAsync(Guid pacienteId, CancellationToken cancellationToken = default);

    Task<Result<Guid>> RegistrarAsync(RegistrarProcedimientoRealizadoRequest request, CancellationToken cancellationToken = default);
}