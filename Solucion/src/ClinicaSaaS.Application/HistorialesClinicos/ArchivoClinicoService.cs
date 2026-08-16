using ClinicaSaaS.Application.Common.Interfaces;
using ClinicaSaaS.Application.Pacientes;
using ClinicaSaaS.Application.Usuarios;
using ClinicaSaaS.Domain.Clinical.Entities;
using ClinicaSaaS.Domain.Clinical.Enums;
using ClinicaSaaS.Domain.Clinical.ValueObjects;
using ClinicaSaaS.SharedKernel.Results;

namespace ClinicaSaaS.Application.HistorialesClinicos;

public sealed class ArchivoClinicoService(
    IRepositorioHistorialClinico repositorio,
    IUnitOfWork unitOfWork,
    IFileStorageService fileStorageService,
    IPacienteService pacienteService,
    IUsuarioService usuarioService,
    ICurrentUserContext currentUserContext) : IArchivoClinicoService
{
    public async Task<IReadOnlyList<ArchivoClinicoDto>> ListarPorPacienteAsync(Guid pacienteId, CancellationToken cancellationToken = default)
    {
        var historial = await repositorio.ObtenerPorPacienteIdAsync(pacienteId, cancellationToken);
        if (historial is null)
            return [];

        var nombrePorUsuario = await ObtenerNombresUsuariosAsync(cancellationToken);

        return historial.Entradas
            .SelectMany(entrada => entrada.Archivos
                .Where(a => !a.EstaEliminado)
                .Select(archivo => MapearADto(archivo, entrada, nombrePorUsuario)))
            .OrderByDescending(dto => dto.FechaSubida)
            .ToList();
    }

    public async Task<Result<Guid>> SubirArchivoAsync(
        Guid pacienteId, TipoArchivoClinico tipoArchivo, string nombreOriginal, Stream contenido, CancellationToken cancellationToken = default)
    {
        var paciente = await pacienteService.ObtenerAsync(pacienteId, cancellationToken);
        if (paciente is null)
            return new Error("ArchivoClinico.PacienteNoEncontrado", "El paciente solicitado no existe.");

        var historial = await repositorio.ObtenerPorPacienteIdAsync(pacienteId, cancellationToken);
        if (historial is null || historial.Entradas.All(e => !e.EsVersionActual))
            return new Error("ArchivoClinico.SinEntradas", "Registra al menos una entrada de historial clínico antes de adjuntar archivos.");

        // Carpeta por clínica+paciente — nunca por nombre real, mismo criterio de aislamiento
        // que el resto del sistema (ver LocalFileStorageService).
        var carpeta = $"{paciente.ClinicaId}/{pacienteId}";
        var almacenamiento = await fileStorageService.GuardarAsync(contenido, nombreOriginal, carpeta, cancellationToken);

        var hashResultado = HashArchivo.Crear(almacenamiento.HashSha256);
        if (hashResultado.EsFallido)
        {
            // No debería ocurrir nunca (el hash lo calcula IFileStorageService con SHA-256 real),
            // pero si el formato fallara por cualquier motivo, no dejamos el binario huérfano.
            await fileStorageService.EliminarAsync(almacenamiento.RutaAlmacenamiento, cancellationToken);
            return hashResultado.Error;
        }

        var resultado = historial.AgregarArchivoAEntradaActual(
            Guid.NewGuid(), tipoArchivo, nombreOriginal, almacenamiento.RutaAlmacenamiento,
            hashResultado.Value, almacenamiento.TamanoBytes, currentUserContext.UsuarioId ?? Guid.Empty, DateTime.UtcNow);

        if (resultado.EsFallido)
        {
            // Compensación: el binario ya se subió físicamente pero el dominio rechazó la
            // operación (ej. la entrada activa cambió entre el chequeo de arriba y este punto).
            await fileStorageService.EliminarAsync(almacenamiento.RutaAlmacenamiento, cancellationToken);
            return resultado.Error;
        }

        try
        {
            await unitOfWork.GuardarCambiosAsync(cancellationToken);
        }
        catch
        {
            // Compensación: la base de datos rechazó la transacción — no dejamos un binario en
            // disco sin ninguna referencia que lo respalde.
            await fileStorageService.EliminarAsync(almacenamiento.RutaAlmacenamiento, cancellationToken);
            throw;
        }

        return resultado.Value.Id;
    }

    public async Task<Result<ArchivoDescargadoDto>> DescargarAsync(Guid pacienteId, Guid archivoId, CancellationToken cancellationToken = default)
    {
        var historial = await repositorio.ObtenerPorPacienteIdAsync(pacienteId, cancellationToken);
        if (historial is null)
            return ArchivoNoEncontrado;

        var archivo = historial.Entradas
            .SelectMany(e => e.Archivos)
            .FirstOrDefault(a => a.Id == archivoId && !a.EstaEliminado);
        if (archivo is null)
            return ArchivoNoEncontrado;

        var contenido = await fileStorageService.ObtenerAsync(archivo.RutaAlmacenamiento, cancellationToken);
        return new ArchivoDescargadoDto(contenido, archivo.NombreOriginal, ResolverTipoContenido(archivo.NombreOriginal));
    }

    public async Task<Result> EliminarAsync(Guid pacienteId, Guid archivoId, CancellationToken cancellationToken = default)
    {
        var historial = await repositorio.ObtenerPorPacienteIdAsync(pacienteId, cancellationToken);
        if (historial is null)
            return ArchivoNoEncontrado;

        // Solo baja lógica — el binario físico se conserva a propósito (ver comentario de
        // IArchivoClinicoService). NUNCA llamar aquí a fileStorageService.EliminarAsync.
        var resultado = historial.EliminarArchivo(archivoId, currentUserContext.UsuarioId ?? Guid.Empty, DateTime.UtcNow);
        if (resultado.EsFallido)
            return resultado;

        await unitOfWork.GuardarCambiosAsync(cancellationToken);
        return Result.Exitoso();
    }

    private static Error ArchivoNoEncontrado =>
        new("ArchivoClinico.NoEncontrado", "El archivo solicitado no existe.");

    private static string ResolverTipoContenido(string nombreArchivo) => Path.GetExtension(nombreArchivo).ToLowerInvariant() switch
    {
        ".pdf" => "application/pdf",
        ".jpg" or ".jpeg" => "image/jpeg",
        ".png" => "image/png",
        _ => "application/octet-stream"
    };

    private async Task<Dictionary<Guid, string>> ObtenerNombresUsuariosAsync(CancellationToken cancellationToken)
    {
        var usuarios = await usuarioService.ListarAsync(cancellationToken);
        return usuarios.ToDictionary(u => u.Id, u => u.NombreCompleto);
    }

    private static ArchivoClinicoDto MapearADto(
        ArchivoClinico archivo, HistorialClinicoEntrada entrada, IReadOnlyDictionary<Guid, string> nombrePorUsuario) => new(
            archivo.Id,
            entrada.Id,
            entrada.Version,
            archivo.TipoArchivo,
            archivo.NombreOriginal,
            archivo.TamanoBytes,
            archivo.SubidoPorUsuarioId,
            nombrePorUsuario.TryGetValue(archivo.SubidoPorUsuarioId, out var nombre) ? nombre : archivo.SubidoPorUsuarioId.ToString(),
            archivo.FechaSubida);
}