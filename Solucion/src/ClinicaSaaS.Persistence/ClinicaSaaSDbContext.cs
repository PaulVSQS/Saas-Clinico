using ClinicaSaaS.Application.Common.Interfaces;
using ClinicaSaaS.Domain.Billing;
using ClinicaSaaS.Domain.Clinical;
using ClinicaSaaS.Domain.Personal;
using ClinicaSaaS.Domain.Scheduling;
using ClinicaSaaS.Domain.Security;
using ClinicaSaaS.Persistence.Audit;
using ClinicaSaaS.Persistence.Extensions;
using ClinicaSaaS.Persistence.Security;
using Microsoft.EntityFrameworkCore;

namespace ClinicaSaaS.Persistence;

/// <summary>
/// Único DbContext de la solución (decisión ya tomada en el documento de diseño de BD,
/// sección 10.8: un solo contexto simplifica multi-tenant porque todas las entidades
/// comparten conexión y SESSION_CONTEXT). Las IEntityTypeConfiguration están organizadas en
/// carpetas por esquema (Security/Personal/Clinical/Scheduling/Billing/Audit) para que el
/// mapeo refleje la separación de bounded contexts aunque el contexto sea uno solo.
///
/// Solo se exponen DbSet de Aggregate Roots. Las Entities internas de cada agregado
/// (HorarioDoctor, HistorialClinicoEntrada, ArchivoClinico, ProcedimientoRealizado,
/// FacturaDetalle, Pago) NO tienen DbSet propio — la regla DDD es que un agregado interno
/// solo se consulta/modifica a través de su raíz, nunca directo. EF Core las sigue mapeando
/// (via las Configurations) y las materializa en memoria como parte del grafo del agregado,
/// simplemente no se ofrece una puerta de entrada para saltarse la raíz.
/// </summary>
public sealed class ClinicaSaaSDbContext : DbContext
{
    private readonly ITenantContext _tenantContext;

    public ClinicaSaaSDbContext(DbContextOptions<ClinicaSaaSDbContext> options, ITenantContext tenantContext)
        : base(options)
    {
        _tenantContext = tenantContext;
    }

    // Expuesto 'internal' únicamente para que QueryFilterExtensions (mismo ensamblado) pueda
    // referenciarlo al construir los filtros — nada fuera de Persistence debe usar esto.
    internal ITenantContext TenantContext => _tenantContext;

    // ---- Security ----
    public DbSet<Clinica> Clinicas => Set<Clinica>();
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<UsuarioClinicaRol> UsuarioClinicaRoles => Set<UsuarioClinicaRol>();
    internal DbSet<RolLookup> Roles => Set<RolLookup>();

    // ---- Personal ----
    public DbSet<Paciente> Pacientes => Set<Paciente>();
    public DbSet<Doctor> Doctores => Set<Doctor>();
    public DbSet<Empleado> Empleados => Set<Empleado>();

    // ---- Clinical ----
    public DbSet<HistorialClinico> HistorialesClinicos => Set<HistorialClinico>();
    public DbSet<ProcedimientoCatalogo> ProcedimientosCatalogo => Set<ProcedimientoCatalogo>();

    // ---- Scheduling ----
    public DbSet<Consultorio> Consultorios => Set<Consultorio>();
    public DbSet<Equipamiento> Equipamiento => Set<Equipamiento>();
    public DbSet<Cita> Citas => Set<Cita>();

    // ---- Billing ----
    public DbSet<Factura> Facturas => Set<Factura>();
    public DbSet<SecuenciaComprobante> SecuenciasComprobante => Set<SecuenciaComprobante>();

    // ---- Audit ----
    internal DbSet<AuditLogEntry> Auditoria => Set<AuditLogEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ClinicaSaaSDbContext).Assembly);

        // Tenant + soft delete (agregados que pertenecen a una clínica y se eliminan lógicamente)
        modelBuilder.AplicarFiltroTenantYSoftDeletePorId<Clinica>(this);
        modelBuilder.AplicarFiltroTenantYSoftDelete<UsuarioClinicaRol>(this);
        modelBuilder.AplicarFiltroTenantYSoftDelete<Paciente>(this);
        modelBuilder.AplicarFiltroTenantYSoftDelete<Doctor>(this);
        modelBuilder.AplicarFiltroTenantYSoftDelete<Empleado>(this);
        modelBuilder.AplicarFiltroTenantYSoftDelete<ProcedimientoCatalogo>(this);
        modelBuilder.AplicarFiltroTenantYSoftDelete<Consultorio>(this);
        modelBuilder.AplicarFiltroTenantYSoftDelete<Equipamiento>(this);
        modelBuilder.AplicarFiltroTenantYSoftDelete<Cita>(this);
        modelBuilder.AplicarFiltroTenantYSoftDelete<Factura>(this);

        // Solo tenant (no tienen soft delete propio)
        modelBuilder.AplicarFiltroSoloTenant<HistorialClinico>(this);
        modelBuilder.AplicarFiltroSoloTenant<SecuenciaComprobante>(this);

        // Solo soft delete (identidad global, no pertenece a una sola clínica)
       modelBuilder.AplicarFiltroSoloSoftDelete<Usuario>();
       
        base.OnModelCreating(modelBuilder);
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // Evita el error clásico de EF Core inferir decimal(18,0) y truncar centavos si algún
        // desarrollador olvida especificar precisión en una propiedad decimal nueva.
        configurationBuilder.Properties<decimal>().HavePrecision(18, 2);
    }
}
