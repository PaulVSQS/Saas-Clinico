namespace ClinicaSaaS.Application.Common.Interfaces;

/// <summary>
/// Resultado de guardar un archivo: exactamente los datos que necesita
/// <c>ArchivoClinico</c> (Fase 2, Clinical) para construirse — RutaAlmacenamiento, Hash y
/// TamanoBytes. Se calcula aquí (no en el dominio) porque calcular un SHA-256 o decidir el
/// nombre físico en disco/blob es una preocupación de infraestructura, no una regla de negocio;
/// el dominio solo valida el FORMATO del hash recibido (ver HashArchivo.Crear), nunca lo calcula.
/// </summary>
public sealed record ResultadoAlmacenamiento(string RutaAlmacenamiento, string HashSha256, long TamanoBytes);

/// <summary>
/// Abstracción de almacenamiento de archivos binarios (hoy: imágenes/PDFs de
/// <c>ArchivoClinico</c>; a futuro podría reutilizarse para otros adjuntos). La implementación
/// concreta vive en Infrastructure y hoy es almacenamiento local en disco — pensado
/// deliberadamente para poder reemplazarse por Azure Blob Storage / S3 más adelante sin tocar
/// ninguna capa que dependa de esta interfaz (Application, Web).
/// </summary>
public interface IFileStorageService
{
    /// <summary>
    /// Guarda el contenido del stream bajo la <paramref name="carpeta"/> indicada (ej.
    /// "{ClinicaId}/{PacienteId}") y devuelve la ruta relativa final, el hash SHA-256 calculado
    /// del contenido y su tamaño en bytes. El nombre físico del archivo NUNCA es
    /// <paramref name="nombreOriginal"/> tal cual — se genera un nombre único para evitar
    /// colisiones y para no exponer el nombre que subió el usuario en la ruta de almacenamiento.
    /// </summary>
    Task<ResultadoAlmacenamiento> GuardarAsync(
        Stream contenido, string nombreOriginal, string carpeta, CancellationToken cancellationToken = default);

    /// <summary>Abre el archivo para lectura. El llamador es responsable de disponer el stream.</summary>
    Task<Stream> ObtenerAsync(string rutaAlmacenamiento, CancellationToken cancellationToken = default);

    /// <summary>
    /// Elimina el archivo físico. Se usa únicamente en compensación de errores (ej. la
    /// transacción de guardar el ArchivoClinico falló después de subir el binario) — el borrado
    /// lógico normal de un ArchivoClinico es soft-delete de dominio y NUNCA borra el binario.
    /// </summary>
    Task EliminarAsync(string rutaAlmacenamiento, CancellationToken cancellationToken = default);
}
