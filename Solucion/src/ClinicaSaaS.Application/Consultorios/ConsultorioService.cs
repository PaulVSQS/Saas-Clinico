using ClinicaSaaS.Application.Clinicas;
using ClinicaSaaS.Application.Common.Interfaces;
using ClinicaSaaS.Domain.Scheduling;
using ClinicaSaaS.SharedKernel.Results;

namespace ClinicaSaaS.Application.Consultorios;

public sealed class ConsultorioService(
    IRepositorio<Consultorio> repositorio,
    IUnitOfWork unitOfWork,
    IClinicaService clinicaService,
    ITenantContext tenantContext,
    ICurrentUserContext currentUserContext) : IConsultorioService
{
    public async Task<IReadOnlyList<ConsultorioDto>> ListarAsync(CancellationToken cancellationToken = default)
    {
        // El filtro global de tenant (Fase 3) restringe esta consulta a la clínica activa del
        // usuario logueado (un AdminClinica normal); sin clínica activa (SuperAdmin SaaS), el
        // filtro se abre y devuelve consultorios de todas las clínicas — vista de plataforma.
        var consultorios = await repositorio.ListarAsync(_ => true, cancellationToken);

        var nombrePorClinica = await ObtenerNombresClinicasAsync(cancellationToken);

        return consultorios
            .OrderBy(c => c.Nombre)
            .Select(c => MapearADto(c, nombrePorClinica))
            .ToList();
    }

    public async Task<ConsultorioDto?> ObtenerAsync(Guid consultorioId, CancellationToken cancellationToken = default)
    {
        var consultorio = await repositorio.ObtenerPorIdAsync(consultorioId, cancellationToken);
        if (consultorio is null)
            return null;

        var nombrePorClinica = await ObtenerNombresClinicasAsync(cancellationToken);
        return MapearADto(consultorio, nombrePorClinica);
    }

    public async Task<Result<Guid>> RegistrarAsync(RegistrarConsultorioRequest request, CancellationToken cancellationToken = default)
    {
        // AISLAMIENTO DE TENANT EN ESCRITURA — mismo criterio que DoctorService/EmpleadoService
        // (Módulos 3 y 4): si el usuario que llama tiene una clínica activa en su sesión
        // (AdminClinica normal), esa es SIEMPRE la ClinicaId real, sin importar qué venga en el
        // request. Solo un SuperAdmin SaaS (sin clínica activa) puede especificar cualquier clínica.
        var clinicaId = tenantContext.ClinicaId ?? request.ClinicaId;

        var resultado = Consultorio.Crear(Guid.NewGuid(), clinicaId, request.Nombre, request.Piso);
        if (resultado.EsFallido)
            return resultado.Error;

        var consultorio = resultado.Value;
        await repositorio.AgregarAsync(consultorio, cancellationToken);
        await unitOfWork.GuardarCambiosAsync(cancellationToken);

        return consultorio.Id;
    }

    public async Task<Result> ActualizarDatosAsync(
        Guid consultorioId, ActualizarDatosConsultorioRequest request, CancellationToken cancellationToken = default)
    {
        var consultorio = await repositorio.ObtenerPorIdAsync(consultorioId, cancellationToken);
        if (consultorio is null)
            return ConsultorioNoEncontrado;

        var resultado = consultorio.ActualizarDatos(request.Nombre, request.Piso);
        if (resultado.EsFallido)
            return resultado;

        await unitOfWork.GuardarCambiosAsync(cancellationToken);
        return Result.Exitoso();
    }

    public async Task<Result> DesactivarAsync(Guid consultorioId, CancellationToken cancellationToken = default)
    {
        var consultorio = await repositorio.ObtenerPorIdAsync(consultorioId, cancellationToken);
        if (consultorio is null)
            return ConsultorioNoEncontrado;

        var resultado = consultorio.Desactivar();
        if (resultado.EsFallido)
            return resultado;

        await unitOfWork.GuardarCambiosAsync(cancellationToken);
        return Result.Exitoso();
    }

    public async Task<Result> ReactivarAsync(Guid consultorioId, CancellationToken cancellationToken = default)
    {
        var consultorio = await repositorio.ObtenerPorIdAsync(consultorioId, cancellationToken);
        if (consultorio is null)
            return ConsultorioNoEncontrado;

        var resultado = consultorio.Reactivar();
        if (resultado.EsFallido)
            return resultado;

        await unitOfWork.GuardarCambiosAsync(cancellationToken);
        return Result.Exitoso();
    }

    public async Task<Result> EliminarAsync(Guid consultorioId, CancellationToken cancellationToken = default)
    {
        var consultorio = await repositorio.ObtenerPorIdAsync(consultorioId, cancellationToken);
        if (consultorio is null)
            return ConsultorioNoEncontrado;

        var resultado = consultorio.Eliminar(currentUserContext.UsuarioId ?? Guid.Empty, DateTime.UtcNow);
        if (resultado.EsFallido)
            return resultado;

        await unitOfWork.GuardarCambiosAsync(cancellationToken);
        return Result.Exitoso();
    }

    private static Error ConsultorioNoEncontrado =>
        new("Consultorio.NoEncontrado", "El consultorio solicitado no existe.");

    private async Task<Dictionary<Guid, string>> ObtenerNombresClinicasAsync(CancellationToken cancellationToken)
    {
        var clinicas = await clinicaService.ListarAsync(cancellationToken);
        return clinicas.ToDictionary(c => c.Id, c => c.NombreComercial);
    }

    private static ConsultorioDto MapearADto(Consultorio c, IReadOnlyDictionary<Guid, string> nombrePorClinica) => new(
        c.Id,
        c.ClinicaId,
        nombrePorClinica.TryGetValue(c.ClinicaId, out var nombreClinica) ? nombreClinica : c.ClinicaId.ToString(),
        c.Nombre,
        c.Piso,
        c.Activo);
}