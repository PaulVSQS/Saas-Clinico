using ClinicaSaaS.Persistence.Audit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClinicaSaaS.Persistence.Configurations.Audit;

public sealed class AuditLogEntryConfiguration : IEntityTypeConfiguration<AuditLogEntry>
{
    public void Configure(EntityTypeBuilder<AuditLogEntry> builder)
    {
        builder.ToTable("Auditoria", "Audit");
        builder.HasKey(a => a.Id);

        builder.Property(a => a.Id).HasColumnName("Id").ValueGeneratedOnAdd();
        builder.Property(a => a.ClinicaId).HasColumnName("ClinicaId");
        builder.Property(a => a.UsuarioId).HasColumnName("UsuarioId");
        builder.Property(a => a.Accion).HasColumnName("Accion").HasMaxLength(20).IsRequired();
        builder.Property(a => a.EntidadNombre).HasColumnName("EntidadNombre").HasMaxLength(100).IsRequired();
        builder.Property(a => a.EntidadId).HasColumnName("EntidadId").HasMaxLength(100).IsRequired();
        builder.Property(a => a.DatosAnteriores).HasColumnName("DatosAnteriores");
        builder.Property(a => a.DatosNuevos).HasColumnName("DatosNuevos");
        builder.Property(a => a.FechaHora).HasColumnName("FechaHora").IsRequired();
        builder.Property(a => a.DireccionIp).HasColumnName("DireccionIP").HasMaxLength(50);
        builder.Property(a => a.Dispositivo).HasColumnName("Dispositivo").HasMaxLength(300);

        builder.HasIndex(a => new { a.ClinicaId, a.FechaHora });
        builder.HasIndex(a => new { a.EntidadNombre, a.EntidadId });
        builder.HasIndex(a => a.UsuarioId);

        builder.ToTable(t => t.HasCheckConstraint(
            "CK_Auditoria_Accion",
            "Accion IN ('Create','Update','Delete','Login','Logout','Anular','Exportar')"));
    }
}
