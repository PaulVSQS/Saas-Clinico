using ClinicaSaaS.Application.Common.Interfaces;
using ClinicaSaaS.Application.Consultorios;
using ClinicaSaaS.Domain.Personal;
using ClinicaSaaS.Domain.Personal.Entities;
using ClinicaSaaS.Domain.Personal.ValueObjects;
using ClinicaSaaS.SharedKernel.Results;

namespace ClinicaSaaS.Application.Horarios;

public sealed class HorarioService(
    IRepositorio<Doctor> repositorioDoctor,
    IUnitOfWork unitOfWork,
    IConsultorioService consultorioService) : IHorarioService
{
    public async Task<IReadOnlyList<HorarioDto>> ListarPorDoctorAsync(Guid doctorId, CancellationToken cancellationToken = default)
    {
        var doctor = await repositorioDoctor.ObtenerPorIdAsync(doctorId, cancellationToken);
        if (doctor is null)
            return [];

        var nombrePorConsultorio = await ObtenerNombresConsultoriosAsync(cancellationToken);

        return doctor.Horarios
            .OrderBy(h => h.DiaSemana ?? DayOfWeek.Sunday)
            .ThenBy(h => h.Bloque.HoraInicio)
            .Select(h => MapearADto(doctor.Id, h, nombrePorConsultorio))
            .ToList();
    }

    public async Task<Result<Guid>> AgregarAsync(AgregarHorarioRequest request, CancellationToken cancellationToken = default)
    {
        var doctor = await repositorioDoctor.ObtenerPorIdAsync(request.DoctorId, cancellationToken);
        if (doctor is null)
            return DoctorNoEncontrado;

        var validacionConsultorio = await ValidarConsultorioAsync(request.ConsultorioId, doctor.ClinicaId, cancellationToken);
        if (validacionConsultorio is not null)
            return validacionConsultorio;

        var bloqueResultado = BloqueHorario.Crear(request.HoraInicio, request.HoraFin);
        if (bloqueResultado.EsFallido)
            return bloqueResultado.Error;

        var vigenciaResultado = PeriodoVigencia.Crear(request.FechaInicioVigencia, request.FechaFinVigencia);
        if (vigenciaResultado.EsFallido)
            return vigenciaResultado.Error;

        var resultado = doctor.AgregarHorario(
            Guid.NewGuid(), request.ConsultorioId, request.TipoHorario, request.DiaSemana,
            bloqueResultado.Value, vigenciaResultado.Value);
        if (resultado.EsFallido)
            return resultado.Error;

        await unitOfWork.GuardarCambiosAsync(cancellationToken);
        return resultado.Value.Id;
    }

    public async Task<Result<IReadOnlyList<Guid>>> AgregarEnDiasAsync(AgregarHorarioEnDiasRequest request, CancellationToken cancellationToken = default)
    {
        var doctor = await repositorioDoctor.ObtenerPorIdAsync(request.DoctorId, cancellationToken);
        if (doctor is null)
            return DoctorNoEncontrado;

        var validacionConsultorio = await ValidarConsultorioAsync(request.ConsultorioId, doctor.ClinicaId, cancellationToken);
        if (validacionConsultorio is not null)
            return validacionConsultorio;

        var bloqueResultado = BloqueHorario.Crear(request.HoraInicio, request.HoraFin);
        if (bloqueResultado.EsFallido)
            return bloqueResultado.Error;

        var vigenciaResultado = PeriodoVigencia.Crear(request.FechaInicioVigencia, request.FechaFinVigencia);
        if (vigenciaResultado.EsFallido)
            return vigenciaResultado.Error;

        var idsPorDia = request.Dias.Distinct().ToDictionary(dia => dia, _ => Guid.NewGuid());

        var resultado = doctor.AgregarHorarioEnVariosDias(
            idsPorDia, request.ConsultorioId, request.TipoHorario, bloqueResultado.Value, vigenciaResultado.Value);
        if (resultado.EsFallido)
            return resultado.Error;

        await unitOfWork.GuardarCambiosAsync(cancellationToken);
        return Result<IReadOnlyList<Guid>>.Exitoso(resultado.Value.Select(h => h.Id).ToList());
    }

    public async Task<Result> ActualizarAsync(
        Guid doctorId, Guid horarioId, ActualizarHorarioRequest request, CancellationToken cancellationToken = default)
    {
        var doctor = await repositorioDoctor.ObtenerPorIdAsync(doctorId, cancellationToken);
        if (doctor is null)
            return DoctorNoEncontrado;

        var validacionConsultorio = await ValidarConsultorioAsync(request.ConsultorioId, doctor.ClinicaId, cancellationToken);
        if (validacionConsultorio is not null)
            return validacionConsultorio;

        var bloqueResultado = BloqueHorario.Crear(request.HoraInicio, request.HoraFin);
        if (bloqueResultado.EsFallido)
            return bloqueResultado.Error;

        var vigenciaResultado = PeriodoVigencia.Crear(request.FechaInicioVigencia, request.FechaFinVigencia);
        if (vigenciaResultado.EsFallido)
            return vigenciaResultado.Error;

        var resultado = doctor.ActualizarHorario(
            horarioId, request.ConsultorioId, request.TipoHorario, request.DiaSemana,
            bloqueResultado.Value, vigenciaResultado.Value);
        if (resultado.EsFallido)
            return resultado;

        await unitOfWork.GuardarCambiosAsync(cancellationToken);
        return Result.Exitoso();
    }

    public async Task<Result> DesactivarAsync(Guid doctorId, Guid horarioId, CancellationToken cancellationToken = default)
    {
        var doctor = await repositorioDoctor.ObtenerPorIdAsync(doctorId, cancellationToken);
        if (doctor is null)
            return DoctorNoEncontrado;

        var resultado = doctor.DesactivarHorario(horarioId);
        if (resultado.EsFallido)
            return resultado;

        await unitOfWork.GuardarCambiosAsync(cancellationToken);
        return Result.Exitoso();
    }

    public async Task<Result> ReactivarAsync(Guid doctorId, Guid horarioId, CancellationToken cancellationToken = default)
    {
        var doctor = await repositorioDoctor.ObtenerPorIdAsync(doctorId, cancellationToken);
        if (doctor is null)
            return DoctorNoEncontrado;

        var resultado = doctor.ReactivarHorario(horarioId);
        if (resultado.EsFallido)
            return resultado;

        await unitOfWork.GuardarCambiosAsync(cancellationToken);
        return Result.Exitoso();
    }

    public async Task<Result> EliminarAsync(Guid doctorId, Guid horarioId, CancellationToken cancellationToken = default)
    {
        var doctor = await repositorioDoctor.ObtenerPorIdAsync(doctorId, cancellationToken);
        if (doctor is null)
            return DoctorNoEncontrado;

        var resultado = doctor.EliminarHorario(horarioId);
        if (resultado.EsFallido)
            return resultado;

        await unitOfWork.GuardarCambiosAsync(cancellationToken);
        return Result.Exitoso();
    }

    private static Error DoctorNoEncontrado =>
        new("Doctor.NoEncontrado", "El doctor solicitado no existe.");

    /// <summary>
    /// Consistencia entre agregados (Doctor y Consultorio): el dominio no la valida porque no es
    /// invariante de un solo agregado, pero Application sí debe evitar que un bloque quede
    /// apuntando al consultorio de OTRA clínica. Devuelve null si no hay error.
    /// </summary>
    private async Task<Error?> ValidarConsultorioAsync(Guid? consultorioId, Guid clinicaIdDoctor, CancellationToken cancellationToken)
    {
        if (consultorioId is not Guid id)
            return null;

        var consultorio = await consultorioService.ObtenerAsync(id, cancellationToken);
        if (consultorio is null || consultorio.ClinicaId != clinicaIdDoctor)
            return new Error("Horario.ConsultorioInvalido", "El consultorio seleccionado no pertenece a la clínica del doctor.");

        return null;
    }

    private async Task<Dictionary<Guid, string>> ObtenerNombresConsultoriosAsync(CancellationToken cancellationToken)
    {
        var consultorios = await consultorioService.ListarAsync(cancellationToken);
        return consultorios.ToDictionary(c => c.Id, c => c.Nombre);
    }

    private static HorarioDto MapearADto(
        Guid doctorId, HorarioDoctor h, IReadOnlyDictionary<Guid, string> nombrePorConsultorio) => new(
            h.Id,
            doctorId,
            h.ConsultorioId,
            h.ConsultorioId is Guid consultorioId && nombrePorConsultorio.TryGetValue(consultorioId, out var nombre) ? nombre : null,
            h.TipoHorario,
            h.DiaSemana,
            h.Bloque.HoraInicio,
            h.Bloque.HoraFin,
            h.Vigencia.FechaInicio,
            h.Vigencia.FechaFin,
            h.Activo);
}
