using System.Security.Cryptography;
using ClinicaSaaS.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace ClinicaSaaS.Infrastructure.Storage;

/// <summary>
/// Implementación de <see cref="IFileStorageService"/> sobre disco local. Es la implementación
/// correcta para desarrollo/on-premise de una sola instancia; el día que el SaaS necesite
/// escalar horizontalmente (varias instancias de la app compartiendo archivos) esto se
/// reemplaza por una implementación de Azure Blob Storage / S3 detrás de la MISMA interfaz —
/// ningún caso de uso de Application ni componente de Web debería cambiar ese día.
///
/// Ruta configurable vía appsettings ("Storage:RutaLocal"), relativa al ContentRoot de la app
/// (nunca a WebRoot/wwwroot: los archivos clínicos son sensibles y jamás deben quedar servibles
/// como contenido estático público). Si no se configura, cae a "App_Data/archivos-clinicos".
/// </summary>
public sealed class LocalFileStorageService : IFileStorageService
{
    private readonly string _rutaRaiz;

    public LocalFileStorageService(IConfiguration configuration, IHostEnvironment entorno)
    {
        var rutaConfigurada = configuration["Storage:RutaLocal"] ?? "App_Data/archivos-clinicos";
        _rutaRaiz = Path.IsPathRooted(rutaConfigurada)
            ? rutaConfigurada
            : Path.Combine(entorno.ContentRootPath, rutaConfigurada);

        Directory.CreateDirectory(_rutaRaiz);
    }

    public async Task<ResultadoAlmacenamiento> GuardarAsync(
        Stream contenido, string nombreOriginal, string carpeta, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(contenido);
        if (string.IsNullOrWhiteSpace(carpeta))
            throw new ArgumentException("La carpeta destino no puede estar vacía.", nameof(carpeta));

        var carpetaAbsoluta = Path.Combine(_rutaRaiz, carpeta);
        Directory.CreateDirectory(carpetaAbsoluta);

        // Nombre físico siempre generado (nunca el nombre original) — evita colisiones y evita
        // exponer en la ruta de almacenamiento el nombre de archivo que subió el usuario.
        var extension = Path.GetExtension(nombreOriginal);
        var nombreFisico = $"{Guid.NewGuid():N}{extension}";
        var rutaAbsoluta = Path.Combine(carpetaAbsoluta, nombreFisico);

        using var sha256 = SHA256.Create();

        await using (var destino = new FileStream(
            rutaAbsoluta, FileMode.CreateNew, FileAccess.Write, FileShare.None, bufferSize: 81920, useAsync: true))
        await using (var cryptoStream = new CryptoStream(destino, sha256, CryptoStreamMode.Write))
        {
            await contenido.CopyToAsync(cryptoStream, cancellationToken);
        }

        var hash = Convert.ToHexString(sha256.Hash!).ToLowerInvariant();
        var tamanoBytes = new FileInfo(rutaAbsoluta).Length;

        // Ruta relativa (no absoluta) es lo que se persiste en ArchivoClinico.RutaAlmacenamiento
        // — así la carpeta raíz puede moverse/migrar sin tener que re-escribir la base de datos.
        var rutaRelativa = Path.Combine(carpeta, nombreFisico).Replace(Path.DirectorySeparatorChar, '/');

        return new ResultadoAlmacenamiento(rutaRelativa, hash, tamanoBytes);
    }

    public Task<Stream> ObtenerAsync(string rutaAlmacenamiento, CancellationToken cancellationToken = default)
    {
        var rutaAbsoluta = ResolverRutaAbsoluta(rutaAlmacenamiento);
        Stream stream = new FileStream(rutaAbsoluta, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 81920, useAsync: true);
        return Task.FromResult(stream);
    }

    public Task EliminarAsync(string rutaAlmacenamiento, CancellationToken cancellationToken = default)
    {
        var rutaAbsoluta = ResolverRutaAbsoluta(rutaAlmacenamiento);
        if (File.Exists(rutaAbsoluta))
            File.Delete(rutaAbsoluta);

        return Task.CompletedTask;
    }

    private string ResolverRutaAbsoluta(string rutaAlmacenamiento)
    {
        if (string.IsNullOrWhiteSpace(rutaAlmacenamiento))
            throw new ArgumentException("La ruta de almacenamiento no puede estar vacía.", nameof(rutaAlmacenamiento));

        return Path.Combine(_rutaRaiz, rutaAlmacenamiento.Replace('/', Path.DirectorySeparatorChar));
    }
}
