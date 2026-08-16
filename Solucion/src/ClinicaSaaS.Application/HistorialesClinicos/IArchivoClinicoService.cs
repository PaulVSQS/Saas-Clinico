using ClinicaSaaS.Domain.Clinical.Enums;
using ClinicaSaaS.SharedKernel.Results;

namespace ClinicaSaaS.Application.HistorialesClinicos;

/// <summary>
/// Vista de un archivo clínico hacia afuera de Application. Deliberadamente NO incluye
/// RutaAlmacenamiento ni HashSha256 — el navegador nunca necesita conocer la ruta interna de
/// almacenamiento ni el hash de integridad; la descarga se resuelve del lado del servidor
/// (ver DescargarAsync + ArchivosClinicosEndpoints en Web).
/// </summary>
public sealed record ArchivoClinicoDto(
    Guid Id,
    Guid HistorialClinicoEntradaId,
    int VersionEntrada,
    TipoArchivoClinico TipoArchivo,
    string NombreOriginal,
    long TamanoBytes,
    Guid SubidoPorUsuarioId,
    string NombreSubidoPor,
    DateTime FechaSubida);

/// <summary>Contenido listo para servir como descarga HTTP — ver ArchivosClinicosEndpoints.</summary>
public sealed record ArchivoDescargadoDto(Stream Contenido, string NombreOriginal, string TipoContenido);

/// <summary>
/// Módulo 10 de Fase 6 — Archivos Médicos: radiografías, fotos intraorales, PDFs y resultados de
/// laboratorio adjuntos a una entrada de historial clínico. Un archivo SIEMPRE cuelga de "la
/// entrada actual" del historial (nunca del paciente directo) — así lo exige el propio dominio
/// (HistorialClinico.AgregarArchivoAEntradaActual), reflejando el requisito de negocio de que
/// cada imagen quede trazada a la consulta exacta donde se generó.
///
/// El almacenamiento físico del binario es responsabilidad de IFileStorageService (Fase 4) — este
/// servicio nunca toca el disco directamente. Al eliminar un archivo, SOLO se hace baja lógica de
/// dominio; el binario físico se conserva (ver comentario de IFileStorageService.EliminarAsync:
/// "el borrado lógico normal... NUNCA borra el binario"). IFileStorageService.EliminarAsync solo
/// se usa aquí como compensación cuando la subida física tuvo éxito pero el resto de la operación
/// falló — nunca como parte de un borrado normal iniciado por el usuario.
///
/// Mismo criterio de protección que los módulos anteriores: Policy "AdminClinica".
/// </summary>
public interface IArchivoClinicoService
{
    /// <summary>Todos los archivos no eliminados del paciente, más reciente primero, con la versión de la entrada a la que pertenecen.</summary>
    Task<IReadOnlyList<ArchivoClinicoDto>> ListarPorPacienteAsync(Guid pacienteId, CancellationToken cancellationToken = default);

    Task<Result<Guid>> SubirArchivoAsync(
        Guid pacienteId, TipoArchivoClinico tipoArchivo, string nombreOriginal, Stream contenido, CancellationToken cancellationToken = default);

    Task<Result<ArchivoDescargadoDto>> DescargarAsync(Guid pacienteId, Guid archivoId, CancellationToken cancellationToken = default);

    /// <summary>Baja lógica únicamente — el binario físico se conserva. Ver comentario de la interfaz.</summary>
    Task<Result> EliminarAsync(Guid pacienteId, Guid archivoId, CancellationToken cancellationToken = default);
}