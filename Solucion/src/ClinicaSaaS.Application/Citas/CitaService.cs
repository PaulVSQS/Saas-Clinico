using ClinicaSaaS.Application.Clinicas;
using ClinicaSaaS.Application.Common.Interfaces;
using ClinicaSaaS.Application.Consultorios;
using ClinicaSaaS.Application.Doctores;
using ClinicaSaaS.Application.Pacientes;
using ClinicaSaaS.Domain.Scheduling;
using ClinicaSaaS.Domain.Scheduling.Interfaces;
using ClinicaSaaS.SharedKernel.Results;

namespace ClinicaSaaS.Application.Citas;

public sealed class CitaService(
    IRepositorio<Cita> repositorio,
    IUnitOfWork unitOfWork,
    IValidadorDisponibilidadDoctor validadorDisponibilidad,
    IClinicaService clinicaService,
    IPacienteService pacienteService,
    IDoctorService doctorService,
    IConsultorioService consultorioService,
    ITenantContext tenantContext,
    ICurrentUserContext currentUserContext) : ICitaService
{
    public async Task<IReadOnlyList<CitaDto>> ListarPorDoctorYFechaAsync(Guid doctorId, DateOnly fecha, CancellationToken cancellationToken = default)
    {
        var inicioDia = fecha.ToDateTime(TimeOnly.MinValue);
        var finDia = fecha.ToDateTime(TimeOnly.MaxValue);

        var citas = await repositorio.ListarAsync(
            c => c.DoctorId == doctorId && c.FechaHoraInicio >= inicioDia && c.FechaHoraInicio <= finDia,
            cancellationToken);

        var joins = await ObtenerJoinsAsync(cancellationToken);

        return citas
            .OrderBy(c => c.FechaHoraInicio)
            .Select(c => MapearADto(c, joins))
            .ToList();
    }

    public async Task<IReadOnlyList<CitaDto>> ListarPorPacienteAsync(Guid pacienteId, CancellationToken cancellationToken = default)
    {
        var citas = await repositorio.ListarAsync(c => c.PacienteId == pacienteId, cancellationToken);
        var joins = await ObtenerJoinsAsync(cancellationToken);

        return citas
            .OrderByDescending(c => c.FechaHoraInicio)
            .Select(c => MapearADto(c, joins))
            .ToList();
    }

    public async Task<CitaDto?> ObtenerAsync(Guid citaId, CancellationToken cancellationToken = default)
    {
        var cita = await repositorio.ObtenerPorIdAsync(citaId, cancellationToken);
        if (cita is null)
            return null;

        var joins = await ObtenerJoinsAsync(cancellationToken);
        return MapearADto(cita, joins);
    }

    public async Task<Result<Guid>> ProgramarAsync(ProgramarCitaRequest request, CancellationToken cancellationToken = default)
    {
        // AISLAMIENTO DE TENANT EN ESCRITURA — mismo criterio que los módulos anteriores.
        var clinicaId = tenantContext.ClinicaId ?? request.ClinicaId;

        var validacionConsultorio = await ValidarConsultorioAsync(request.ConsultorioId, clinicaId, cancellationToken);
        if (validacionConsultorio is not null)
            return validacionConsultorio;

        var disponible = await validadorDisponibilidad.EstaDisponibleAsync(
            request.DoctorId, request.FechaHoraInicio, request.FechaHoraFin, citaAExcluirId: null, cancellationToken);
        if (!disponible)
            return new Error("Cita.DoctorNoDisponible", "El doctor ya tiene una cita programada en ese horario.");

        var resultado = Cita.Programar(
            Guid.NewGuid(), clinicaId, request.PacienteId, request.DoctorId, request.ConsultorioId,
            request.FechaHoraInicio, request.FechaHoraFin, request.MotivoConsulta,
            currentUserContext.UsuarioId ?? Guid.Empty);
        if (resultado.EsFallido)
            return resultado.Error;

        var cita = resultado.Value;
        await repositorio.AgregarAsync(cita, cancellationToken);
        await unitOfWork.GuardarCambiosAsync(cancellationToken);

        return cita.Id;
    }

    public async Task<Result> ConfirmarAsync(Guid citaId, CancellationToken cancellationToken = default) =>
        await AplicarTransicionAsync(citaId, cita => cita.Confirmar(), cancellationToken);

    public async Task<Result> IniciarAtencionAsync(Guid citaId, CancellationToken cancellationToken = default) =>
        await AplicarTransicionAsync(citaId, cita => cita.IniciarAtencion(), cancellationToken);

    public async Task<Result> CompletarAsync(Guid citaId, CancellationToken cancellationToken = default) =>
        await AplicarTransicionAsync(citaId, cita => cita.Completar(DateTime.UtcNow), cancellationToken);

    public async Task<Result> CancelarAsync(Guid citaId, CancellationToken cancellationToken = default) =>
        await AplicarTransicionAsync(citaId, cita => cita.Cancelar(), cancellationToken);

    public async Task<Result> MarcarNoAsistioAsync(Guid citaId, CancellationToken cancellationToken = default) =>
        await AplicarTransicionAsync(citaId, cita => cita.MarcarNoAsistio(), cancellationToken);

    public async Task<Result> ReprogramarAsync(Guid citaId, ReprogramarCitaRequest request, CancellationToken cancellationToken = default)
    {
        var cita = await repositorio.ObtenerPorIdAsync(citaId, cancellationToken);
        if (cita is null)
            return CitaNoEncontrada;

        var disponible = await validadorDisponibilidad.EstaDisponibleAsync(
            cita.DoctorId, request.NuevaFechaHoraInicio, request.NuevaFechaHoraFin, citaAExcluirId: citaId, cancellationToken);
        if (!disponible)
            return new Error("Cita.DoctorNoDisponible", "El doctor ya tiene una cita programada en ese horario.");

        var resultado = cita.Reprogramar(request.NuevaFechaHoraInicio, request.NuevaFechaHoraFin);
        if (resultado.EsFallido)
            return resultado;

        await unitOfWork.GuardarCambiosAsync(cancellationToken);
        return Result.Exitoso();
    }

    public async Task<Result> EliminarAsync(Guid citaId, CancellationToken cancellationToken = default)
    {
        var cita = await repositorio.ObtenerPorIdAsync(citaId, cancellationToken);
        if (cita is null)
            return CitaNoEncontrada;

        var resultado = cita.Eliminar(currentUserContext.UsuarioId ?? Guid.Empty, DateTime.UtcNow);
        if (resultado.EsFallido)
            return resultado;

        await unitOfWork.GuardarCambiosAsync(cancellationToken);
        return Result.Exitoso();
    }

    private async Task<Result> AplicarTransicionAsync(Guid citaId, Func<Cita, Result> transicion, CancellationToken cancellationToken)
    {
        var cita = await repositorio.ObtenerPorIdAsync(citaId, cancellationToken);
        if (cita is null)
            return CitaNoEncontrada;

        var resultado = transicion(cita);
        if (resultado.EsFallido)
            return resultado;

        await unitOfWork.GuardarCambiosAsync(cancellationToken);
        return Result.Exitoso();
    }

    private static Error CitaNoEncontrada =>
        new("Cita.NoEncontrada", "La cita solicitada no existe.");

    private async Task<Error?> ValidarConsultorioAsync(Guid? consultorioId, Guid clinicaId, CancellationToken cancellationToken)
    {
        if (consultorioId is not Guid id)
            return null;

        var consultorio = await consultorioService.ObtenerAsync(id, cancellationToken);
        if (consultorio is null || consultorio.ClinicaId != clinicaId)
            return new Error("Cita.ConsultorioInvalido", "El consultorio seleccionado no pertenece a la clínica.");

        return null;
    }

    private sealed record Joins(
        IReadOnlyDictionary<Guid, string> NombrePorClinica,
        IReadOnlyDictionary<Guid, string> NombrePorPaciente,
        IReadOnlyDictionary<Guid, string> NombrePorDoctor,
        IReadOnlyDictionary<Guid, string> NombrePorConsultorio);

    private async Task<Joins> ObtenerJoinsAsync(CancellationToken cancellationToken)
    {
        var clinicas = await clinicaService.ListarAsync(cancellationToken);
        var pacientes = await pacienteService.ListarAsync(cancellationToken: cancellationToken);
        var doctores = await doctorService.ListarAsync(cancellationToken);
        var consultorios = await consultorioService.ListarAsync(cancellationToken);

        return new Joins(
            clinicas.ToDictionary(c => c.Id, c => c.NombreComercial),
            pacientes.ToDictionary(p => p.Id, p => p.NombreCompleto),
            doctores.ToDictionary(d => d.Id, d => d.NombreUsuario),
            consultorios.ToDictionary(c => c.Id, c => c.Nombre));
    }

    private static CitaDto MapearADto(Cita c, Joins joins) => new(
        c.Id,
        c.ClinicaId,
        joins.NombrePorClinica.TryGetValue(c.ClinicaId, out var nombreClinica) ? nombreClinica : c.ClinicaId.ToString(),
        c.PacienteId,
        joins.NombrePorPaciente.TryGetValue(c.PacienteId, out var nombrePaciente) ? nombrePaciente : c.PacienteId.ToString(),
        c.DoctorId,
        joins.NombrePorDoctor.TryGetValue(c.DoctorId, out var nombreDoctor) ? nombreDoctor : c.DoctorId.ToString(),
        c.ConsultorioId,
        c.ConsultorioId is Guid consultorioId && joins.NombrePorConsultorio.TryGetValue(consultorioId, out var nombreConsultorio) ? nombreConsultorio : null,
        c.FechaHoraInicio,
        c.FechaHoraFin,
        c.Estado,
        c.MotivoConsulta);
}