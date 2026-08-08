using ClinicaSaaS.Domain.Scheduling;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClinicaSaaS.Persistence.Configurations.Scheduling;

public sealed class ConsultorioConfiguration : IEntityTypeConfiguration<Consultorio>
{
    public void Configure(EntityTypeBuilder<Consultorio> builder)
    {
        builder.ToTable("Consultorios", "Scheduling");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Id).ValueGeneratedNever();
        builder.Property(c => c.ClinicaId).HasColumnName("ClinicaId").IsRequired();
        builder.Property(c => c.Nombre).HasColumnName("Nombre").HasMaxLength(100).IsRequired();
        builder.Property(c => c.Piso).HasColumnName("Piso").HasMaxLength(20);
        builder.Property(c => c.Activo).HasColumnName("Activo").IsRequired();

        builder.Property(c => c.FechaCreacion).HasColumnName("FechaCreacion").IsRequired();
        builder.Property(c => c.CreadoPorUsuarioId).HasColumnName("CreadoPorUsuarioId").IsRequired();
        builder.Property(c => c.FechaModificacion).HasColumnName("FechaModificacion");
        builder.Property(c => c.ModificadoPorUsuarioId).HasColumnName("ModificadoPorUsuarioId");

        builder.Property(c => c.EstaEliminado).HasColumnName("EstaEliminado").IsRequired();
        builder.Property(c => c.FechaEliminacion).HasColumnName("FechaEliminacion");
        builder.Property(c => c.EliminadoPorUsuarioId).HasColumnName("EliminadoPorUsuarioId");

        builder.Ignore(c => c.DomainEvents);
    }
}
