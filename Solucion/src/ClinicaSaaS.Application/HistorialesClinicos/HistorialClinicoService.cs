using ClinicaSaaS.Application.Citas;
using ClinicaSaaS.Application.Common.Interfaces;
using ClinicaSaaS.Application.Doctores;
using ClinicaSaaS.Application.Pacientes;
using ClinicaSaaS.Domain.Clinical;
using ClinicaSaaS.Domain.Clinical.Entities;
using ClinicaSaaS.Domain.Clinical.ValueObjects;
using ClinicaSaaS.SharedKernel.Results;

namespace ClinicaSaaS.Application.HistorialesClinicos;

public sealed class HistorialClinicoService(
    IRepositorioHistorialClinico repositorio,
    IUnitOfWork unitOfWork,
    IPacienteService pacienteService,
    IDoctorService doctorService,
    ICitaService citaService) : IHistorialClinicoService
{
    public async Task<IReadOnlyList<EntradaHistorialClinicoDto>> ListarEntradasPorPacienteAsync(Guid pacienteId, CancellationToken cancellationToken = default)
    {
        var historial = await repositorio.ObtenerPorPacienteIdAsync(pacienteId, cancellationToken);
        if (historial is null)
            return [];

        var nombrePorDoctor = await ObtenerNombresDoctoresAsync(cancellationToken);

        return historial.Entradas
            .OrderByDescending(e => e.Version)
            .Select(e => MapearADto(e, nombrePorDoctor))
            .ToList();
    }

    public async Task<Result<Guid>> AgregarEntradaAsync(AgregarEntradaHistorialClinicoRequest request, CancellationToken cancellationToken = default)
    {
        var paciente = await pacienteService.ObtenerAsync(request.PacienteId, cancellationToken);
        if (paciente is null)
            return new Error("HistorialClinico.PacienteNoEncontrado", "El paciente solicitado no existe.");

        // Consistencia entre agregados: el doctor que registra la entrada debe pertenecer a la
        // misma clínica del paciente — mismo criterio que la validación de consultorio en
        // Horarios/Citas.
        var doctor = await doctorService.ObtenerAsync(request.DoctorId, cancellationToken);
        if (doctor is null || doctor.ClinicaId != paciente.ClinicaId)
            return new Error("HistorialClinico.DoctorInvalido", "El doctor seleccionado no pertenece a la clínica del paciente.");

        if (request.CitaId is Guid citaId)
        {
            var citasDelPaciente = await citaService.ListarPorPacienteAsync(request.PacienteId, cancellationToken);
            if (!citasDelPaciente.Any(c => c.Id == citaId))
                return new Error("HistorialClinico.CitaInvalida", "La cita seleccionada no pertenece a este paciente.");
        }

        SignosVitales? signosVitales = null;
        if (request.SignosVitales is not null)
        {
            var sv = request.SignosVitales;
            var signosResultado = SignosVitales.Crear(
                sv.PresionSistolica, sv.PresionDiastolica, sv.TemperaturaCelsius,
                sv.FrecuenciaCardiaca, sv.FrecuenciaRespiratoria, sv.SaturacionOxigeno, sv.PesoKg, sv.TallaCm);
            if (signosResultado.EsFallido)
                return signosResultado.Error;

            signosVitales = signosResultado.Value;
        }

        // Creación perezosa del contenedor raíz — la primera entrada de un paciente abre su
        // historial automáticamente, sin un paso de setup manual separado.
        var historial = await repositorio.ObtenerPorPacienteIdAsync(request.PacienteId, cancellationToken);
        if (historial is null)
        {
            var creacion = HistorialClinico.Crear(Guid.NewGuid(), paciente.ClinicaId, request.PacienteId, DateTime.UtcNow);
            if (creacion.EsFallido)
                return creacion.Error;

            historial = creacion.Value;
            await repositorio.AgregarAsync(historial, cancellationToken);
        }

        var resultado = historial.AgregarEntrada(
            Guid.NewGuid(), request.DoctorId, request.CitaId, request.MotivoConsulta, request.Diagnostico,
            request.Tratamiento, request.Notas, signosVitales, DateTime.UtcNow);
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

    private static EntradaHistorialClinicoDto MapearADto(HistorialClinicoEntrada e, IReadOnlyDictionary<Guid, string> nombrePorDoctor) => new(
        e.Id,
        e.Version,
        e.EntradaAnteriorId,
        e.DoctorId,
        nombrePorDoctor.TryGetValue(e.DoctorId, out var nombreDoctor) ? nombreDoctor : e.DoctorId.ToString(),
        e.CitaId,
        e.MotivoConsulta,
        e.Diagnostico,
        e.Tratamiento,
        e.Notas,
        e.SignosVitales is null ? null : new SignosVitalesDto(
            e.SignosVitales.PresionSistolica, e.SignosVitales.PresionDiastolica, e.SignosVitales.TemperaturaCelsius,
            e.SignosVitales.FrecuenciaCardiaca, e.SignosVitales.FrecuenciaRespiratoria, e.SignosVitales.SaturacionOxigeno,
            e.SignosVitales.PesoKg, e.SignosVitales.TallaCm),
        e.FechaRegistro,
        e.EsVersionActual);
}