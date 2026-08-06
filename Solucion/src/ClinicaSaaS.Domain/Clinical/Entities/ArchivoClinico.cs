using ClinicaSaaS.Domain.Clinical.Enums;
using ClinicaSaaS.Domain.Clinical.ValueObjects;
using ClinicaSaaS.SharedKernel.Abstractions;
using ClinicaSaaS.SharedKernel.Common;

namespace ClinicaSaaS.Domain.Clinical.Entities;

/// <summary>
/// Entity dentro del agregado HistorialClinico, ligada a una HistorialClinicoEntrada específica
/// (nunca directo al paciente — requisito explícito de negocio: cada imagen/PDF queda trazado
/// a la consulta exacta donde se generó). No guarda el binario, solo la referencia a blob
/// storage + hash de integridad.
/// </summary>
public sealed class ArchivoClinico : EntityBase, ISoftDeletable
{
    public TipoArchivoClinico TipoArchivo { get; private set; }
    public string NombreOriginal { get; private set; }
    public string RutaAlmacenamiento { get; private set; }
    public HashArchivo Hash { get; private set; }
    public long TamanoBytes { get; private set; }
    public Guid SubidoPorUsuarioId { get; private set; }
    public DateTime FechaSubida { get; private set; }

    public bool EstaEliminado { get; private set; }
    public DateTime? FechaEliminacion { get; private set; }
    public Guid? EliminadoPorUsuarioId { get; private set; }

    private ArchivoClinico() { } // EF Core

    internal ArchivoClinico(
        Guid id, TipoArchivoClinico tipoArchivo, string nombreOriginal, string rutaAlmacenamiento,
        HashArchivo hash, long tamanoBytes, Guid subidoPorUsuarioId, DateTime fechaUtc) : base(id)
    {
        TipoArchivo = tipoArchivo;
        NombreOriginal = nombreOriginal;
        RutaAlmacenamiento = rutaAlmacenamiento;
        Hash = hash;
        TamanoBytes = tamanoBytes;
        SubidoPorUsuarioId = subidoPorUsuarioId;
        FechaSubida = fechaUtc;
    }

    internal void Eliminar(Guid usuarioId, DateTime fechaUtc)
    {
        if (EstaEliminado) return;
        EstaEliminado = true;
        FechaEliminacion = fechaUtc;
        EliminadoPorUsuarioId = usuarioId;
    }
}
