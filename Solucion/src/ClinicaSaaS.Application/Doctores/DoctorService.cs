using ClinicaSaaS.Application.Clinicas;
using ClinicaSaaS.Application.Common.Interfaces;
using ClinicaSaaS.Application.Usuarios;
using ClinicaSaaS.Domain.Personal;
using ClinicaSaaS.SharedKernel.Results;

namespace ClinicaSaaS.Application.Doctores;

public sealed class DoctorService(
    IRepositorio<Doctor> repositorio,
    IUnitOfWork unitOfWork,
    IUsuarioService usuarioService,
    IClinicaService clinicaService,
    ITenantContext tenantContext) : IDoctorService
{
    public async Task<IReadOnlyList<DoctorDto>> ListarAsync(CancellationToken cancellationToken = default)
    {
        // El filtro global de tenant (Fase 3) restringe esta consulta a la clínica activa del
        // usuario logueado (un AdminClinica normal); sin clínica activa (SuperAdmin SaaS), el
        // filtro se abre y devuelve doctores de todas las clínicas — vista de plataforma.
        var doctores = await repositorio.ListarAsync(_ => true, cancellationToken);

        var (nombrePorUsuario, emailPorUsuario) = await ObtenerDatosUsuariosAsync(cancellationToken);
        var nombrePorClinica = await ObtenerNombresClinicasAsync(cancellationToken);

        return doctores
            .OrderBy(d => nombrePorUsuario.GetValueOrDefault(d.UsuarioId, string.Empty))
            .Select(d => MapearADto(d, nombrePorUsuario, emailPorUsuario, nombrePorClinica))
            .ToList();
    }

    public async Task<DoctorDto?> ObtenerAsync(Guid doctorId, CancellationToken cancellationToken = default)
    {
        var doctor = await repositorio.ObtenerPorIdAsync(doctorId, cancellationToken);
        if (doctor is null)
            return null;

        var (nombrePorUsuario, emailPorUsuario) = await ObtenerDatosUsuariosAsync(cancellationToken);
        var nombrePorClinica = await ObtenerNombresClinicasAsync(cancellationToken);

        return MapearADto(doctor, nombrePorUsuario, emailPorUsuario, nombrePorClinica);
    }

    public async Task<Result<Guid>> RegistrarAsync(RegistrarDoctorRequest request, CancellationToken cancellationToken = default)
    {
        // AISLAMIENTO DE TENANT EN ESCRITURA — mismo criterio que EmpleadoService (Módulo 3):
        // si el usuario que llama tiene una clínica activa en su sesión (AdminClinica normal),
        // esa es SIEMPRE la ClinicaId real, sin importar qué venga en el request. Solo un
        // SuperAdmin SaaS (sin clínica activa) puede especificar cualquier clínica.
        var clinicaId = tenantContext.ClinicaId ?? request.ClinicaId;

        var yaExiste = await repositorio.PrimeroOPredeterminadoAsync(
            d => d.UsuarioId == request.UsuarioId && d.ClinicaId == clinicaId, cancellationToken);
        if (yaExiste is not null)
            return new Error("Doctor.YaRegistrado", "Ese usuario ya está registrado como doctor en esta clínica.");

        var resultado = Doctor.Registrar(Guid.NewGuid(), request.UsuarioId, clinicaId, request.Especialidad, request.NumeroExequatur);
        if (resultado.EsFallido)
            return resultado.Error;

        var doctor = resultado.Value;
        await repositorio.AgregarAsync(doctor, cancellationToken);
        await unitOfWork.GuardarCambiosAsync(cancellationToken);

        return doctor.Id;
    }

    public async Task<Result> ActualizarDatosAsync(
        Guid doctorId, ActualizarDatosDoctorRequest request, CancellationToken cancellationToken = default)
    {
        var doctor = await repositorio.ObtenerPorIdAsync(doctorId, cancellationToken);
        if (doctor is null)
            return DoctorNoEncontrado;

        var resultado = doctor.ActualizarDatos(request.Especialidad, request.NumeroExequatur);
        if (resultado.EsFallido)
            return resultado;

        await unitOfWork.GuardarCambiosAsync(cancellationToken);
        return Result.Exitoso();
    }

    public async Task<Result> DesactivarAsync(Guid doctorId, CancellationToken cancellationToken = default)
    {
        var doctor = await repositorio.ObtenerPorIdAsync(doctorId, cancellationToken);
        if (doctor is null)
            return DoctorNoEncontrado;

        var resultado = doctor.Desactivar();
        if (resultado.EsFallido)
            return resultado;

        await unitOfWork.GuardarCambiosAsync(cancellationToken);
        return Result.Exitoso();
    }

    public async Task<Result> ReactivarAsync(Guid doctorId, CancellationToken cancellationToken = default)
    {
        var doctor = await repositorio.ObtenerPorIdAsync(doctorId, cancellationToken);
        if (doctor is null)
            return DoctorNoEncontrado;

        var resultado = doctor.Reactivar();
        if (resultado.EsFallido)
            return resultado;

        await unitOfWork.GuardarCambiosAsync(cancellationToken);
        return Result.Exitoso();
    }

    private static Error DoctorNoEncontrado =>
        new("Doctor.NoEncontrado", "El doctor solicitado no existe.");

    private async Task<(Dictionary<Guid, string> nombres, Dictionary<Guid, string> emails)> ObtenerDatosUsuariosAsync(
        CancellationToken cancellationToken)
    {
        var usuarios = await usuarioService.ListarAsync(cancellationToken);
        return (
            usuarios.ToDictionary(u => u.Id, u => u.NombreCompleto),
            usuarios.ToDictionary(u => u.Id, u => u.Email));
    }

    private async Task<Dictionary<Guid, string>> ObtenerNombresClinicasAsync(CancellationToken cancellationToken)
    {
        var clinicas = await clinicaService.ListarAsync(cancellationToken);
        return clinicas.ToDictionary(c => c.Id, c => c.NombreComercial);
    }

    private static DoctorDto MapearADto(
        Doctor d,
        IReadOnlyDictionary<Guid, string> nombrePorUsuario,
        IReadOnlyDictionary<Guid, string> emailPorUsuario,
        IReadOnlyDictionary<Guid, string> nombrePorClinica) => new(
            d.Id,
            d.UsuarioId,
            nombrePorUsuario.TryGetValue(d.UsuarioId, out var nombre) ? nombre : "(usuario desconocido)",
            emailPorUsuario.TryGetValue(d.UsuarioId, out var email) ? email : string.Empty,
            d.ClinicaId,
            nombrePorClinica.TryGetValue(d.ClinicaId, out var nombreClinica) ? nombreClinica : d.ClinicaId.ToString(),
            d.Especialidad,
            d.NumeroExequatur,
            d.Activo);
}
