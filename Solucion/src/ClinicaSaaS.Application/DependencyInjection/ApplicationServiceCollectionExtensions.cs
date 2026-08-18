using ClinicaSaaS.Application.Citas;
using ClinicaSaaS.Application.Clinicas;
using ClinicaSaaS.Application.Common.Interfaces;
using ClinicaSaaS.Application.Consultorios;
using ClinicaSaaS.Application.Doctores;
using ClinicaSaaS.Application.Empleados;
using ClinicaSaaS.Application.Facturacion;
using ClinicaSaaS.Application.HistorialesClinicos;
using ClinicaSaaS.Application.Horarios;
using ClinicaSaaS.Application.Pacientes;
using ClinicaSaaS.Application.Procedimientos;
using ClinicaSaaS.Application.Security;
using ClinicaSaaS.Application.Usuarios;
using Microsoft.Extensions.DependencyInjection;

namespace ClinicaSaaS.Application.DependencyInjection;

/// <summary>
/// Punto único de registro de los servicios de Application (Fase 5: IAutenticacionService;
/// Fase 6: un servicio por módulo de negocio — Clínicas, Usuarios, Empleados, Doctores,
/// Consultorios, Horarios, Pacientes, Citas, Historia Clínica, Archivos Médicos, Procedimientos,
/// Facturación, Pagos, y los que sigan). Mismo patrón que AddInfrastructure()/AddPersistence()
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
        // Módulo 7 de Fase 6: Pacientes
        services.AddScoped<IPacienteService, PacienteService>();
        // Módulo 8 de Fase 6: Citas
        services.AddScoped<ICitaService, CitaService>();
        // Módulo 9 de Fase 6: Historia Clínica
        services.AddScoped<IHistorialClinicoService, HistorialClinicoService>();
        // Módulo 10 de Fase 6: Archivos Médicos
        services.AddScoped<IArchivoClinicoService, ArchivoClinicoService>();
        // Módulo 11 de Fase 6: Procedimientos
        services.AddScoped<IProcedimientoCatalogoService, ProcedimientoCatalogoService>();
        services.AddScoped<IProcedimientoRealizadoService, ProcedimientoRealizadoService>();
        // Módulo 12 de Fase 6: Facturación
        services.AddScoped<ISecuenciaComprobanteService, SecuenciaComprobanteService>();
        services.AddScoped<IFacturaService, FacturaService>();
        // Módulo 13 de Fase 6: Pagos
        services.AddScoped<IPagoService, PagoService>();
        return services;
    }
}