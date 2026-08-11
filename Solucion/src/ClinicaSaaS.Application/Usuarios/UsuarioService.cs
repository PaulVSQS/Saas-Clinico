using ClinicaSaaS.Application.Clinicas;
using ClinicaSaaS.Application.Common.Interfaces;
using ClinicaSaaS.Domain.Security;
using ClinicaSaaS.Domain.Security.ValueObjects;
using ClinicaSaaS.SharedKernel.Results;

namespace ClinicaSaaS.Application.Usuarios;

public sealed class UsuarioService(
    IRepositorio<Usuario> repositorioUsuario,
    IRepositorio<UsuarioClinicaRol> repositorioRol,
    IPasswordHasher passwordHasher,
    IUnitOfWork unitOfWork,
    IClinicaService clinicaService,
    ICurrentUserContext currentUserContext) : IUsuarioService
{
    public async Task<IReadOnlyList<UsuarioDto>> ListarAsync(CancellationToken cancellationToken = default)
    {
        // Usuario no tiene filtro de tenant (identidad global), por eso ListarAsync(_ => true)
        // devuelve TODOS los usuarios de la plataforma — correcto para la vista de SuperAdmin SaaS.
        var usuarios = await repositorioUsuario.ListarAsync(_ => true, cancellationToken);

        return usuarios
            .OrderBy(u => u.NombreCompleto)
            .Select(MapearADto)
            .ToList();
    }

    public async Task<UsuarioDto?> ObtenerAsync(Guid usuarioId, CancellationToken cancellationToken = default)
    {
        var usuario = await repositorioUsuario.ObtenerPorIdAsync(usuarioId, cancellationToken);
        return usuario is null ? null : MapearADto(usuario);
    }

    public async Task<Result<Guid>> RegistrarAsync(RegistrarUsuarioRequest request, CancellationToken cancellationToken = default)
    {
        // Crear el Value Object primero para validar formato antes de consultar la BD.
        var emailResultado = Email.Crear(request.Email);
        if (emailResultado.EsFallido)
            return emailResultado.Error;

        if (string.IsNullOrWhiteSpace(request.Password))
            return new Error("Usuario.PasswordRequerido", "La contraseña es requerida.");

        // REGLA CRÍTICA: comparar el Value Object completo — NO u.Email.Valor == string.
        // Email tiene HasConversion en EF Core; comparar un miembro interno del VO no se
        // traduce a SQL. Comparar el VO completo sí funciona (EF usa el valor convertido).
        var emailVo = emailResultado.Value;
        var yaExiste = await repositorioUsuario.PrimeroOPredeterminadoAsync(
            u => u.Email == emailVo, cancellationToken);
        if (yaExiste is not null)
            return new Error("Usuario.EmailDuplicado", "Ya existe un usuario registrado con ese correo electrónico.");

        var passwordHash = passwordHasher.HashearPassword(request.Password);

        var usuarioResultado = Usuario.Registrar(Guid.NewGuid(), emailVo, passwordHash, request.NombreCompleto);
        if (usuarioResultado.EsFallido)
            return usuarioResultado.Error;

        var usuario = usuarioResultado.Value;
        await repositorioUsuario.AgregarAsync(usuario, cancellationToken);
        await unitOfWork.GuardarCambiosAsync(cancellationToken);

        return usuario.Id;
    }

    public async Task<Result> ActualizarDatosPersonalesAsync(
        Guid usuarioId, ActualizarDatosPersonalesRequest request, CancellationToken cancellationToken = default)
    {
        var usuario = await repositorioUsuario.ObtenerPorIdAsync(usuarioId, cancellationToken);
        if (usuario is null)
            return UsuarioNoEncontrado;

        var resultado = usuario.ActualizarDatosPersonales(request.NombreCompleto, request.Telefono);
        if (resultado.EsFallido)
            return resultado;

        await unitOfWork.GuardarCambiosAsync(cancellationToken);
        return Result.Exitoso();
    }

    public async Task<Result> DesactivarAsync(Guid usuarioId, CancellationToken cancellationToken = default)
    {
        var usuario = await repositorioUsuario.ObtenerPorIdAsync(usuarioId, cancellationToken);
        if (usuario is null)
            return UsuarioNoEncontrado;

        var resultado = usuario.Desactivar();
        if (resultado.EsFallido)
            return resultado;

        await unitOfWork.GuardarCambiosAsync(cancellationToken);
        return Result.Exitoso();
    }

    public async Task<Result> ReactivarAsync(Guid usuarioId, CancellationToken cancellationToken = default)
    {
        var usuario = await repositorioUsuario.ObtenerPorIdAsync(usuarioId, cancellationToken);
        if (usuario is null)
            return UsuarioNoEncontrado;

        var resultado = usuario.Reactivar();
        if (resultado.EsFallido)
            return resultado;

        await unitOfWork.GuardarCambiosAsync(cancellationToken);
        return Result.Exitoso();
    }

    public async Task<IReadOnlyList<UsuarioClinicaRolDto>> ListarMembresiasAsync(
        Guid usuarioId, CancellationToken cancellationToken = default)
    {
        // UsuarioClinicaRol sí tiene filtro de tenant: cuando no hay ClinicaId activo
        // (SuperAdmin SaaS), el filtro pasa sin restricción → devuelve membresías de todas
        // las clínicas, que es lo correcto para la vista de gestión de plataforma.
        var membresias = await repositorioRol.ListarAsync(m => m.UsuarioId == usuarioId, cancellationToken);

        // Para obtener el nombre de la clínica hacemos join en memoria usando los DTOs de
        // IClinicaService (ya registrado). El número de clínicas en un SaaS es pequeño,
        // por lo que este join in-process es aceptable y evita introducir una proyección
        // adicional o una nueva dependencia de Application hacia EF Core.
        var clinicas = await clinicaService.ListarAsync(cancellationToken);
        var nombrePorId = clinicas.ToDictionary(c => c.Id, c => c.NombreComercial);

        return membresias
            .OrderBy(m => m.FechaAsignacion)
            .Select(m => new UsuarioClinicaRolDto(
                m.Id,
                m.ClinicaId,
                nombrePorId.TryGetValue(m.ClinicaId, out var nombre) ? nombre : m.ClinicaId.ToString(),
                m.Rol,
                m.FechaAsignacion))
            .ToList();
    }

    public async Task<Result> AsignarRolAsync(AsignarRolRequest request, CancellationToken cancellationToken = default)
    {
        // Verificar que el usuario exista.
        var usuario = await repositorioUsuario.ObtenerPorIdAsync(request.UsuarioId, cancellationToken);
        if (usuario is null)
            return UsuarioNoEncontrado;

        // Verificar que no exista ya una membresía activa para el mismo usuario/clínica/rol.
        var yaAsignado = await repositorioRol.PrimeroOPredeterminadoAsync(
            m => m.UsuarioId == request.UsuarioId &&
                 m.ClinicaId == request.ClinicaId &&
                 m.Rol == request.Rol,
            cancellationToken);
        if (yaAsignado is not null)
            return new Error("UsuarioClinicaRol.YaAsignado", "El usuario ya tiene ese rol asignado en esa clínica.");

        var rolResultado = UsuarioClinicaRol.Asignar(
            Guid.NewGuid(), request.UsuarioId, request.ClinicaId, request.Rol,
            // ICurrentUserContext ya existe desde Fase 4 -- se usa aqui para dejar registrado
            // quien hizo la asignacion, en vez de null.
            asignadoPorUsuarioId: currentUserContext.UsuarioId,
            fechaUtc: DateTime.UtcNow);
        if (rolResultado.EsFallido)
            return rolResultado.Error;

        await repositorioRol.AgregarAsync(rolResultado.Value, cancellationToken);
        await unitOfWork.GuardarCambiosAsync(cancellationToken);
        return Result.Exitoso();
    }

    public async Task<Result> RevocarRolAsync(Guid usuarioClinicaRolId, CancellationToken cancellationToken = default)
    {
        var membresia = await repositorioRol.ObtenerPorIdAsync(usuarioClinicaRolId, cancellationToken);
        if (membresia is null)
            return new Error("UsuarioClinicaRol.NoEncontrado", "La membresía solicitada no existe.");

        // Igual que en AsignarRolAsync: se usa el usuario real de la sesion (Fase 4) para el
        // rastro de auditoria de quien revoco, en vez de Guid.Empty.
        var resultado = membresia.Revocar(
            usuarioId: currentUserContext.UsuarioId ?? Guid.Empty, fechaUtc: DateTime.UtcNow);
        if (resultado.EsFallido)
            return resultado;

        await unitOfWork.GuardarCambiosAsync(cancellationToken);
        return Result.Exitoso();
    }

    private static Error UsuarioNoEncontrado =>
        new("Usuario.NoEncontrado", "El usuario solicitado no existe.");

    private static UsuarioDto MapearADto(Usuario u) => new(
        u.Id, u.Email.Valor, u.NombreCompleto, u.Telefono, u.EsSuperAdminSaaS, u.Activo);
}