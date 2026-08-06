using ClinicaSaaS.Domain.Clinical.Enums;
using ClinicaSaaS.Domain.Clinical.ValueObjects;
using ClinicaSaaS.Domain.Common.ValueObjects;
using ClinicaSaaS.SharedKernel.Common;
using ClinicaSaaS.SharedKernel.Results;

namespace ClinicaSaaS.Domain.Clinical.Entities;

/// <summary>
/// Entity dentro del agregado HistorialClinico. Representa UNA consulta/versión del expediente.
/// REGLA DE NEGOCIO CRÍTICA: es append-only — no existe ningún método "Editar" sobre los campos
/// clínicos (MotivoConsulta, Diagnostico, Tratamiento, etc.) una vez creada. Un expediente
/// alterado retroactivamente sin dejar rastro es inaceptable legal y médicamente; corregir un
/// dato significa crear una nueva entrada enlazada a la anterior vía EntradaAnteriorId, nunca
/// mutar la existente. Solo AgregarArchivo/AgregarProcedimientoRealizado (colecciones internas,
/// aditivas) y MarcarComoNoVersionActual (housekeeping interno del agregado) son mutaciones
/// permitidas.
/// </summary>
public sealed class HistorialClinicoEntrada : EntityBase
{
    private readonly List<ArchivoClinico> _archivos = [];
    private readonly List<ProcedimientoRealizado> _procedimientosRealizados = [];

    public int Version { get; private set; }
    public Guid? EntradaAnteriorId { get; private set; }
    public Guid DoctorId { get; private set; }
    public Guid? CitaId { get; private set; }
    public string? MotivoConsulta { get; private set; }
    public string? Diagnostico { get; private set; }
    public string? Tratamiento { get; private set; }
    public string? Notas { get; private set; }
    public SignosVitales? SignosVitales { get; private set; }
    public DateTime FechaRegistro { get; private set; }
    public bool EsVersionActual { get; private set; }

    public IReadOnlyCollection<ArchivoClinico> Archivos => _archivos.AsReadOnly();
    public IReadOnlyCollection<ProcedimientoRealizado> ProcedimientosRealizados => _procedimientosRealizados.AsReadOnly();

    private HistorialClinicoEntrada() { } // EF Core

    internal HistorialClinicoEntrada(
        Guid id, int version, Guid? entradaAnteriorId, Guid doctorId, Guid? citaId,
        string? motivoConsulta, string? diagnostico, string? tratamiento, string? notas,
        SignosVitales? signosVitales, DateTime fechaUtc) : base(id)
    {
        Version = version;
        EntradaAnteriorId = entradaAnteriorId;
        DoctorId = doctorId;
        CitaId = citaId;
        MotivoConsulta = motivoConsulta;
        Diagnostico = diagnostico;
        Tratamiento = tratamiento;
        Notas = notas;
        SignosVitales = signosVitales;
        FechaRegistro = fechaUtc;
        EsVersionActual = true;
    }

    internal void MarcarComoNoVersionActual() => EsVersionActual = false;

    internal Result<ArchivoClinico> AgregarArchivo(
        Guid idArchivo, TipoArchivoClinico tipoArchivo, string nombreOriginal, string rutaAlmacenamiento,
        HashArchivo hash, long tamanoBytes, Guid subidoPorUsuarioId, DateTime fechaUtc)
    {
        if (tamanoBytes <= 0)
            return new Error("ArchivoClinico.TamanoInvalido", "El tamaño del archivo debe ser mayor a cero.");

        var archivo = new ArchivoClinico(
            idArchivo, tipoArchivo, nombreOriginal, rutaAlmacenamiento, hash, tamanoBytes, subidoPorUsuarioId, fechaUtc);
        _archivos.Add(archivo);
        return archivo;
    }

    internal ProcedimientoRealizado RegistrarProcedimientoRealizado(
        Guid idProcedimiento, Guid procedimientoCatalogoId, Guid doctorId, string? piezaDental,
        Dinero precioAplicado, DateTime fechaUtc, string? notas)
    {
        var procedimiento = new ProcedimientoRealizado(
            idProcedimiento, procedimientoCatalogoId, doctorId, piezaDental, precioAplicado, fechaUtc, notas);
        _procedimientosRealizados.Add(procedimiento);
        return procedimiento;
    }
}
