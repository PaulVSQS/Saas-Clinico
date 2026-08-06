using ClinicaSaaS.Domain.Clinical.Entities;
using ClinicaSaaS.Domain.Clinical.Enums;
using ClinicaSaaS.Domain.Clinical.Events;
using ClinicaSaaS.Domain.Clinical.ValueObjects;
using ClinicaSaaS.Domain.Common.ValueObjects;
using ClinicaSaaS.SharedKernel.Abstractions;
using ClinicaSaaS.SharedKernel.Common;
using ClinicaSaaS.SharedKernel.Results;

namespace ClinicaSaaS.Domain.Clinical;

/// <summary>
/// Aggregate Root. Contenedor raíz del expediente médico de UN paciente (relación 1:1 forzada
/// por UQ_Historial_Paciente en BD). Es su propio agregado —separado de Paciente— porque tiene
/// un límite de consistencia distinto: agregar una entrada clínica no debe requerir cargar ni
/// bloquear el registro de datos personales del paciente, y viceversa. Contiene las
/// HistorialClinicoEntradas como Entities internas porque la regla de versionado (append-only,
/// una sola EsVersionActual=true a la vez) solo puede garantizarse si todas las entradas se
/// modifican dentro de la misma transacción de agregado.
/// </summary>
public sealed class HistorialClinico : EntityBase, ITenantEntity
{
    private readonly List<HistorialClinicoEntrada> _entradas = [];

    public Guid ClinicaId { get; private set; }
    public Guid PacienteId { get; private set; }
    public DateTime FechaCreacion { get; private set; }

    public IReadOnlyCollection<HistorialClinicoEntrada> Entradas => _entradas.AsReadOnly();

    private HistorialClinico() { } // EF Core

    private HistorialClinico(Guid id, Guid clinicaId, Guid pacienteId, DateTime fechaUtc) : base(id)
    {
        ClinicaId = clinicaId;
        PacienteId = pacienteId;
        FechaCreacion = fechaUtc;
    }

    public static Result<HistorialClinico> Crear(Guid id, Guid clinicaId, Guid pacienteId, DateTime fechaUtc)
    {
        Guard.ContraGuidVacio(id, nameof(id));
        Guard.ContraGuidVacio(clinicaId, nameof(clinicaId));
        Guard.ContraGuidVacio(pacienteId, nameof(pacienteId));

        return new HistorialClinico(id, clinicaId, pacienteId, fechaUtc);
    }

    /// <summary>
    /// Único punto de entrada para registrar una consulta. Nunca hay un método "EditarEntrada":
    /// corregir un dato clínico significa agregar una entrada nueva enlazada a la anterior.
    /// </summary>
    public Result<HistorialClinicoEntrada> AgregarEntrada(
        Guid idNuevaEntrada, Guid doctorId, Guid? citaId, string? motivoConsulta, string? diagnostico,
        string? tratamiento, string? notas, SignosVitales? signosVitales, DateTime fechaUtc)
    {
        Guard.ContraGuidVacio(idNuevaEntrada, nameof(idNuevaEntrada));
        Guard.ContraGuidVacio(doctorId, nameof(doctorId));

        var entradaAnterior = _entradas.SingleOrDefault(e => e.EsVersionActual);
        entradaAnterior?.MarcarComoNoVersionActual();

        var nuevaVersion = (entradaAnterior?.Version ?? 0) + 1;

        var entrada = new HistorialClinicoEntrada(
            idNuevaEntrada, nuevaVersion, entradaAnterior?.Id, doctorId, citaId,
            motivoConsulta, diagnostico, tratamiento, notas, signosVitales, fechaUtc);

        _entradas.Add(entrada);

        RaiseEvent(new HistorialClinicoEntradaAgregadaDomainEvent(Id, entrada.Id, PacienteId, doctorId, nuevaVersion, fechaUtc));

        return entrada;
    }

    public Result<ArchivoClinico> AgregarArchivoAEntradaActual(
        Guid idArchivo, TipoArchivoClinico tipoArchivo, string nombreOriginal, string rutaAlmacenamiento,
        HashArchivo hash, long tamanoBytes, Guid subidoPorUsuarioId, DateTime fechaUtc)
    {
        var entradaActual = _entradas.SingleOrDefault(e => e.EsVersionActual);
        if (entradaActual is null)
            return new Error("HistorialClinico.SinEntradas", "El historial no tiene ninguna entrada registrada.");

        return entradaActual.AgregarArchivo(idArchivo, tipoArchivo, nombreOriginal, rutaAlmacenamiento, hash, tamanoBytes, subidoPorUsuarioId, fechaUtc);
    }

    public Result<ProcedimientoRealizado> RegistrarProcedimientoEnEntradaActual(
        Guid idProcedimiento, Guid procedimientoCatalogoId, Guid doctorId, string? piezaDental,
        Dinero precioAplicado, DateTime fechaUtc, string? notas)
    {
        var entradaActual = _entradas.SingleOrDefault(e => e.EsVersionActual);
        if (entradaActual is null)
            return new Error("HistorialClinico.SinEntradas", "El historial no tiene ninguna entrada registrada.");

        return entradaActual.RegistrarProcedimientoRealizado(idProcedimiento, procedimientoCatalogoId, doctorId, piezaDental, precioAplicado, fechaUtc, notas);
    }
}
