using ClinicaSaaS.Application.Common.Interfaces;
using ClinicaSaaS.Domain.Security;
using ClinicaSaaS.Domain.Security.ValueObjects;
using ClinicaSaaS.SharedKernel.Results;

namespace ClinicaSaaS.Application.Security;

/// <summary>
/// Implementación del caso de uso de autenticación (Fase 5). Deliberadamente NO hace commit
/// (<see cref="IUnitOfWork.GuardarCambiosAsync"/>) por sí mismo, ni siquiera cuando reemplaza
/// el hash por un rehash — mantiene la misma regla del resto de la Fase 4: un servicio de
/// Application deja las entidades en seguimiento, y es el llamador (el endpoint de login en
/// Web) quien decide cuándo confirmar, dentro de su propia unidad de trabajo.
/// </summary>
public sealed class AutenticacionService(
    IRepositorio<Usuario> repositorioUsuarios,
    IRepositorio<UsuarioClinicaRol> repositorioMembresias,
    IPasswordHasher passwordHasher) : IAutenticacionService
{
    private static readonly Error CredencialesInvalidas =
        new("Autenticacion.CredencialesInvalidas", "Correo o contraseña incorrectos.");

    public async Task<Result<UsuarioAutenticado>> AutenticarAsync(
        string email, string passwordPlano, CancellationToken cancellationToken = default)
    {
        var emailResultado = Email.Crear(email);
        if (emailResultado.EsFallido)
            return CredencialesInvalidas;

        // IMPORTANTE: se compara el Value Object completo (u.Email == ...), NUNCA un miembro
        // interno suyo (u.Email.Valor == ...) dentro de un predicado — EF Core sabe traducir
        // una comparación sobre la propiedad completa aplicando el HasConversion configurado
        // en UsuarioConfiguration, pero no sabe "abrir" el objeto convertido para leer un
        // miembro suyo en SQL; eso produce InvalidOperationException en tiempo de ejecución,
        // no en compilación, porque LINQ solo se traduce quando la consulta corre.
        var emailBuscado = emailResultado.Value;

        // Usuario NO tiene filtro de tenant (es identidad global, ver Usuario.cs) — esta
        // consulta funciona igual antes y después de que exista una clínica activa.
        var usuario = await repositorioUsuarios.PrimeroOPredeterminadoAsync(
            u => u.Email == emailBuscado, cancellationToken);

        if (usuario is null)
            return CredencialesInvalidas;

        if (!usuario.Activo)
            return new Error("Autenticacion.UsuarioInactivo", "Este usuario está desactivado.");

        var verificacion = passwordHasher.VerificarPassword(usuario.PasswordHash, passwordPlano);
        if (verificacion == ResultadoVerificacionPassword.Incorrecta)
            return CredencialesInvalidas;

        if (verificacion == ResultadoVerificacionPassword.CorrectaRequiereRehash)
            _ = usuario.CambiarPassword(passwordHasher.HashearPassword(passwordPlano)); // no puede fallar: el hash siempre viene bien formado de IPasswordHasher

        // El filtro global de tenant de UsuarioClinicaRol se "abre" cuando no hay ClinicaId
        // activo todavía (ver QueryFilterExtensions, Fase 3) — exactamente el caso aquí, antes
        // de terminar de autenticar no existe todavía una clínica activa en la sesión. Por eso
        // esta consulta SÍ puede ver las membresías del usuario en TODAS sus clínicas.
        var membresiasActivas = await repositorioMembresias.ListarAsync(
            m => m.UsuarioId == usuario.Id && m.Activo, cancellationToken);

        return new UsuarioAutenticado(
            usuario.Id,
            usuario.Email.Valor,
            usuario.NombreCompleto,
            usuario.EsSuperAdminSaaS,
            membresiasActivas.Select(m => new MembresiaClinica(m.ClinicaId, m.Rol)).ToList());
    }
}