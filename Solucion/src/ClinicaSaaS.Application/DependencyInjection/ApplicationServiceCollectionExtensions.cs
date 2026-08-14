using ClinicaSaaS.Application.Clinicas;
using ClinicaSaaS.Application.Common.Interfaces;
using ClinicaSaaS.Application.Consultorios;
using ClinicaSaaS.Application.Doctores;
using ClinicaSaaS.Application.Empleados;
using ClinicaSaaS.Application.Horarios;
using ClinicaSaaS.Application.Security;
using ClinicaSaaS.Application.Usuarios;
using Microsoft.Extensions.DependencyInjection;

namespace ClinicaSaaS.Application.DependencyInjection;

/// <summary>
/// Punto único de registro de los servicios de Application (Fase 5: IAutenticacionService;
/// Fase 6: un servicio por módulo de negocio — Clínicas, Usuarios, Empleados, Doctores,
/// Consultorios, Horarios, y los que sigan). Mismo patrón que AddInfrastructure()/AddPersistence()
/// — Program.cs no debe conocer las clases concretas de este proyecto, solo llamar
/// builder.Services.AddApplication().
/// </summary>
public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IAutenticacionService, AutenticacionService>();
        // Módulo 1 de Fase 6: Clínicas
        services.AddScoped<IClinicaService, ClinicaService>();
        // Módulo 2 de Fase 6: Usuarios
        services.AddScoped<IUsuarioService, UsuarioService>();
        // Módulo 3 de Fase 6: Empleados
        services.AddScoped<IEmpleadoService, EmpleadoService>();
        // Módulo 4 de Fase 6: Doctores
        services.AddScoped<IDoctorService, DoctorService>();
        // Módulo 5 de Fase 6: Consultorios
        services.AddScoped<IConsultorioService, ConsultorioService>();
        // Módulo 6 de Fase 6: Horarios
        services.AddScoped<IHorarioService, HorarioService>();
        return services;
    }
}
