using ClinicaSaaS.Domain.Personal;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClinicaSaaS.Persistence.Configurations.Personal;

public sealed class EmpleadoConfiguration : IEntityTypeConfiguration<Empleado>
{
    public void Configure(EntityTypeBuilder<Empleado> builder)
    {
        builder.ToTable("Empleados", "Personal");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id).ValueGeneratedNever();
        builder.Property(e => e.UsuarioId).HasColumnName("UsuarioId").IsRequired();
        builder.Property(e => e.ClinicaId).HasColumnName("ClinicaId").IsRequired();
        builder.Property(e => e.Cargo).HasColumnName("Cargo").HasMaxLength(100).IsRequired();
        builder.Property(e => e.FechaIngreso).HasColumnName("FechaIngreso");
        builder.Property(e => e.Activo).HasColumnName("Activo").IsRequired();

        builder.Property(e => e.FechaCreacion).HasColumnName("FechaCreacion").IsRequired();
        builder.Property(e => e.CreadoPorUsuarioId).HasColumnName("CreadoPorUsuarioId").IsRequired();
        builder.Property(e => e.FechaModificacion).HasColumnName("FechaModificacion");
        builder.Property(e => e.ModificadoPorUsuarioId).HasColumnName("ModificadoPorUsuarioId");

        builder.Property(e => e.EstaEliminado).HasColumnName("EstaEliminado").IsRequired();
        builder.Property(e => e.FechaEliminacion).HasColumnName("FechaEliminacion");
        builder.Property(e => e.EliminadoPorUsuarioId).HasColumnName("EliminadoPorUsuarioId");

        builder.HasIndex(e => new { e.UsuarioId, e.ClinicaId }).IsUnique();

        builder.Ignore(e => e.DomainEvents);
    }
}
