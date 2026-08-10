using ClinicaSaaS.Application.Common.Interfaces;
using ClinicaSaaS.Domain.Security;
using ClinicaSaaS.Domain.Security.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace ClinicaSaaS.Web;

/// <summary>
/// SOLO PARA DESARROLLO — Program.cs únicamente la invoca cuando
/// <c>app.Environment.IsDevelopment()</c> es verdadero. Crea un único Usuario SuperAdmin SaaS
/// de prueba la primera vez que se corre la app localmente y todavía no existe ningún Usuario
/// en la base de datos, para poder probar el login de Fase 5 de inmediato sin esperar a que
/// Fase 6 construya el módulo real de alta de usuarios.
///
/// BÓRRESE ESTE ARCHIVO (y su llamada en Program.cs) en cuanto exista un flujo real de registro
/// de usuarios — no está pensado para sobrevivir más allá de esa fase.
/// </summary>
internal static class DevDataSeeder
{
    private const string EmailSuperAdminDev = "admin@clinicasaas.dev";
    private const string PasswordSuperAdminDev = "Admin123!";

    public static async Task SembrarUsuarioSuperAdminAsync(IServiceProvider servicios)
    {
        var repositorioUsuarios = servicios.GetRequiredService<IRepositorio<Usuario>>();

        var yaHayUsuarios = await repositorioUsuarios.Consultar().AnyAsync();
        if (yaHayUsuarios)
            return;

        var passwordHasher = servicios.GetRequiredService<IPasswordHasher>();
        var email = Email.Crear(EmailSuperAdminDev).Value;
        var hash = passwordHasher.HashearPassword(PasswordSuperAdminDev);

        var resultado = Usuario.Registrar(Guid.NewGuid(), email, hash, "Super Admin (dev)");
        if (resultado.EsFallido)
            return;

        var usuario = resultado.Value;
        usuario.OtorgarSuperAdminSaaS();

        await repositorioUsuarios.AgregarAsync(usuario);

        var unitOfWork = servicios.GetRequiredService<IUnitOfWork>();
        await unitOfWork.GuardarCambiosAsync();
    }
}
