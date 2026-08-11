using ClinicaSaaS.Application.Clinicas;
using ClinicaSaaS.Application.Common.Interfaces;
using ClinicaSaaS.Application.Usuarios;
using ClinicaSaaS.Domain.Personal;
using ClinicaSaaS.SharedKernel.Results;

namespace ClinicaSaaS.Application.Empleados;

public sealed class EmpleadoService(
    IRepositorio<Empleado> repositorio,
    IUnitOfWork unitOfWork,
    IUsuarioService usuarioService,
    IClinicaService clinicaService,
    ITenantContext tenantContext) : IEmpleadoService
{
    public async Task<IReadOnlyList<EmpleadoDto>> ListarAsync(CancellationToken cancellationToken = default)
    {
        // El filtro global de tenant (Fase 3) ya restringe esta consulta a la clínica activa
        // cuando el usuario logueado tiene una (un AdminClinica normal); si no hay clínica
        // activa (SuperAdmin SaaS), el filtro se abre y devuelve empleados de todas las
        // clínicas — vista de plataforma, correcta para ese caso.
        var empleados = await repositorio.ListarAsync(_ => true, cancellationToken);

        var (nombrePorUsuario, emailPorUsuario) = await ObtenerDatosUsuariosAsync(cancellationToken);
        var nombrePorClinica = await ObtenerNombresClinicasAsync(cancellationToken);

        return empleados
            .OrderBy(e => nombrePorUsuario.GetValueOrDefault(e.UsuarioId, string.Empty))
            .Select(e => MapearADto(e, nombrePorUsuario, emailPorUsuario, nombrePorClinica))
            .ToList();
    }

    public async Task<EmpleadoDto?> ObtenerAsync(Guid empleadoId, CancellationToken cancellationToken = default)
    {
        var empleado = await repositorio.ObtenerPorIdAsync(empleadoId, cancellationToken);
        if (empleado is null)
            return null;

        var (nombrePorUsuario, emailPorUsuario) = await ObtenerDatosUsuariosAsync(cancellationToken);
        var nombrePorClinica = await ObtenerNombresClinicasAsync(cancellationToken);

        return MapearADto(empleado, nombrePorUsuario, emailPorUsuario, nombrePorClinica);
    }

    public async Task<Result<Guid>> RegistrarAsync(RegistrarEmpleadoRequest request, CancellationToken cancellationToken = default)
    {
        // AISLAMIENTO DE TENANT EN ESCRITURA: si el usuario que llama tiene una clínica activa
        // en su sesión (un AdminClinica normal, no SuperAdmin), la ClinicaId real SIEMPRE es la
        // de su propia sesión — se ignora cualquier ClinicaId distinto que venga en el request.
        // Esto no es solo una comodidad de UI (ocultar el selector de clínica): es la barrera de
        // seguridad real, para que un AdminClinica no pueda dar de alta un empleado en una
        // clínica que no es la suya así manipule el formulario. Un SuperAdmin SaaS (sin
        // ClinicaId activo) sí puede especificar cualquier clínica — coherente con que gestiona
        // toda la plataforma.
        var clinicaId = tenantContext.ClinicaId ?? request.ClinicaId;

        var yaExiste = await repositorio.PrimeroOPredeterminadoAsync(
            e => e.UsuarioId == request.UsuarioId && e.ClinicaId == clinicaId, cancellationToken);
        if (yaExiste is not null)
            return new Error("Empleado.YaRegistrado", "Ese usuario ya está registrado como empleado en esta clínica.");

        var resultado = Empleado.Registrar(Guid.NewGuid(), request.UsuarioId, clinicaId, request.Cargo, request.FechaIngreso);
        if (resultado.EsFallido)
            return resultado.Error;

        var empleado = resultado.Value;
        await repositorio.AgregarAsync(empleado, cancellationToken);
        await unitOfWork.GuardarCambiosAsync(cancellationToken);

        return empleado.Id;
    }

    public async Task<Result> ActualizarDatosAsync(
        Guid empleadoId, ActualizarDatosEmpleadoRequest request, CancellationToken cancellationToken = default)
    {
        var empleado = await repositorio.ObtenerPorIdAsync(empleadoId, cancellationToken);
        if (empleado is null)
            return EmpleadoNoEncontrado;

        var resultado = empleado.ActualizarDatos(request.Cargo, request.FechaIngreso);
        if (resultado.EsFallido)
            return resultado;

        await unitOfWork.GuardarCambiosAsync(cancellationToken);
        return Result.Exitoso();
    }

    public async Task<Result> DesactivarAsync(Guid empleadoId, CancellationToken cancellationToken = default)
    {
        var empleado = await repositorio.ObtenerPorIdAsync(empleadoId, cancellationToken);
        if (empleado is null)
            return EmpleadoNoEncontrado;

        var resultado = empleado.Desactivar();
        if (resultado.EsFallido)
            return resultado;

        await unitOfWork.GuardarCambiosAsync(cancellationToken);
        return Result.Exitoso();
    }

    public async Task<Result> ReactivarAsync(Guid empleadoId, CancellationToken cancellationToken = default)
    {
        var empleado = await repositorio.ObtenerPorIdAsync(empleadoId, cancellationToken);
        if (empleado is null)
            return EmpleadoNoEncontrado;

        var resultado = empleado.Reactivar();
        if (resultado.EsFallido)
            return resultado;

        await unitOfWork.GuardarCambiosAsync(cancellationToken);
        return Result.Exitoso();
    }

    private static Error EmpleadoNoEncontrado =>
        new("Empleado.NoEncontrado", "El empleado solicitado no existe.");

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

    private static EmpleadoDto MapearADto(
        Empleado e,
        IReadOnlyDictionary<Guid, string> nombrePorUsuario,
        IReadOnlyDictionary<Guid, string> emailPorUsuario,
        IReadOnlyDictionary<Guid, string> nombrePorClinica) => new(
            e.Id,
            e.UsuarioId,
            nombrePorUsuario.TryGetValue(e.UsuarioId, out var nombre) ? nombre : "(usuario desconocido)",
            emailPorUsuario.TryGetValue(e.UsuarioId, out var email) ? email : string.Empty,
            e.ClinicaId,
            nombrePorClinica.TryGetValue(e.ClinicaId, out var nombreClinica) ? nombreClinica : e.ClinicaId.ToString(),
            e.Cargo,
            e.FechaIngreso,
            e.Activo);
}
