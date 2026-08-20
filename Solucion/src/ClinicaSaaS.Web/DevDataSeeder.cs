using ClinicaSaaS.Application.Common.Interfaces;
using ClinicaSaaS.Domain.Security;
using ClinicaSaaS.Domain.Security.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace ClinicaSaaS.Web;

/// <summary>
/// SOLO PARA DESARROLLO — Program.cs únicamente la invoca cuando
/// <c>app.Environment.IsDevelopment()</c> es verdadero. Crea un único Usuario SuperAdmin SaaS
/// de prueba en cada arranque local si todavía no existe ninguno, para poder probar el login de
/// Fase 5 de inmediato sin esperar a que Fase 6 construya el módulo real de alta de usuarios.
///
/// El check es "¿existe un SuperAdmin?", no "¿existe algún Usuario?" — a propósito: en pruebas
/// es normal terminar con Empleados/Doctores/Pacientes ya creados (con sus propios Usuario) y
/// haber eliminado el superadmin de prueba aparte (ej. para probar el flujo de Eliminar). Si el
/// check fuera "algún Usuario", el seeder nunca volvería a correr en ese escenario y el login
/// quedaría bloqueado sin ningún superadmin para recuperarlo — justo el caso que motivó este ajuste.
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

        // Antes: repositorioUsuarios.Consultar().AnyAsync() — bloqueaba el reseed apenas
        // existiera CUALQUIER usuario (Empleados/Doctores ya usan uno cada uno). Ahora se
        // filtra por EsSuperAdminSaaS, que es lo que este seeder realmente garantiza.
        var yaHaySuperAdmin = await repositorioUsuarios.Consultar().AnyAsync(u => u.EsSuperAdminSaaS);
        if (yaHaySuperAdmin)
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