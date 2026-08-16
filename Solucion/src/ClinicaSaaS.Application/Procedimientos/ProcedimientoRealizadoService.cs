using ClinicaSaaS.Application.Common.Interfaces;
using ClinicaSaaS.Application.Doctores;
using ClinicaSaaS.Application.Pacientes;
using ClinicaSaaS.Domain.Clinical.Entities;
using ClinicaSaaS.Domain.Common.ValueObjects;
using ClinicaSaaS.SharedKernel.Results;

namespace ClinicaSaaS.Application.Procedimientos;

public sealed class ProcedimientoRealizadoService(
    IRepositorioHistorialClinico repositorioHistorial,
    IUnitOfWork unitOfWork,
    IPacienteService pacienteService,
    IDoctorService doctorService,
    IProcedimientoCatalogoService procedimientoCatalogoService) : IProcedimientoRealizadoService
{
    public async Task<IReadOnlyList<ProcedimientoRealizadoDto>> ListarPorPacienteAsync(Guid pacienteId, CancellationToken cancellationToken = default)
    {
        var historial = await repositorioHistorial.ObtenerPorPacienteIdAsync(pacienteId, cancellationToken);
        if (historial is null)
            return [];

        var nombrePorDoctor = await ObtenerNombresDoctoresAsync(cancellationToken);
        var catalogoPorId = (await procedimientoCatalogoService.ListarAsync(cancellationToken)).ToDictionary(c => c.Id);

        return historial.Entradas
            .SelectMany(entrada => entrada.ProcedimientosRealizados
                .Select(procedimiento => MapearADto(procedimiento, entrada, nombrePorDoctor, catalogoPorId)))
            .OrderByDescending(dto => dto.Fecha)
            .ToList();
    }

    public async Task<Result<Guid>> RegistrarAsync(RegistrarProcedimientoRealizadoRequest request, CancellationToken cancellationToken = default)
    {
        var paciente = await pacienteService.ObtenerAsync(request.PacienteId, cancellationToken);
        if (paciente is null)
            return new Error("ProcedimientoRealizado.PacienteNoEncontrado", "El paciente solicitado no existe.");

        // Consistencia entre agregados: doctor y procedimiento del catálogo deben pertenecer a
        // la misma clínica del paciente — mismo criterio que Horarios/Citas/Archivos.
        var doctor = await doctorService.ObtenerAsync(request.DoctorId, cancellationToken);
        if (doctor is null || doctor.ClinicaId != paciente.ClinicaId)
            return new Error("ProcedimientoRealizado.DoctorInvalido", "El doctor seleccionado no pertenece a la clínica del paciente.");

        var procedimientoCatalogo = await procedimientoCatalogoService.ObtenerAsync(request.ProcedimientoCatalogoId, cancellationToken);
        if (procedimientoCatalogo is null || procedimientoCatalogo.ClinicaId != paciente.ClinicaId)
            return new Error("ProcedimientoRealizado.ProcedimientoInvalido", "El procedimiento seleccionado no pertenece a la clínica del paciente.");

        if (!procedimientoCatalogo.Activo)
            return new Error("ProcedimientoRealizado.ProcedimientoInactivo", "El procedimiento seleccionado está inactivo en el catálogo.");

        var historial = await repositorioHistorial.ObtenerPorPacienteIdAsync(request.PacienteId, cancellationToken);
        if (historial is null || historial.Entradas.All(e => !e.EsVersionActual))
            return new Error("ProcedimientoRealizado.SinEntradas", "Registra al menos una entrada de historial clínico antes de registrar procedimientos.");

        var precioResultado = Dinero.Crear(request.PrecioAplicado ?? procedimientoCatalogo.PrecioBase);
        if (precioResultado.EsFallido)
            return precioResultado.Error;

        var resultado = historial.RegistrarProcedimientoEnEntradaActual(
            Guid.NewGuid(), request.ProcedimientoCatalogoId, request.DoctorId, request.PiezaDental,
            precioResultado.Value, DateTime.UtcNow, request.Notas);
        if (resultado.EsFallido)
            return resultado.Error;

        await unitOfWork.GuardarCambiosAsync(cancellationToken);
        return resultado.Value.Id;
    }

    private async Task<Dictionary<Guid, string>> ObtenerNombresDoctoresAsync(CancellationToken cancellationToken)
    {
        var doctores = await doctorService.ListarAsync(cancellationToken);
        return doctores.ToDictionary(d => d.Id, d => d.NombreUsuario);
    }

    private static ProcedimientoRealizadoDto MapearADto(
        ProcedimientoRealizado procedimiento, HistorialClinicoEntrada entrada,
        IReadOnlyDictionary<Guid, string> nombrePorDoctor, IReadOnlyDictionary<Guid, ProcedimientoCatalogoDto> catalogoPorId) => new(
            procedimiento.Id,
            entrada.Id,
            entrada.Version,
            procedimiento.ProcedimientoCatalogoId,
            catalogoPorId.TryGetValue(procedimiento.ProcedimientoCatalogoId, out var catalogo) ? catalogo.Codigo : "—",
            catalogoPorId.TryGetValue(procedimiento.ProcedimientoCatalogoId, out var catalogo2) ? catalogo2.Nombre : "Procedimiento eliminado",
            procedimiento.DoctorId,
            nombrePorDoctor.TryGetValue(procedimiento.DoctorId, out var nombreDoctor) ? nombreDoctor : procedimiento.DoctorId.ToString(),
            procedimiento.PiezaDental,
            procedimiento.PrecioAplicado.Monto,
            procedimiento.Fecha,
            procedimiento.Notas);
}