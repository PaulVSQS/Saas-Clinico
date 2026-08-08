using ClinicaSaaS.Domain.Clinical;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClinicaSaaS.Persistence.Configurations.Clinical;

public sealed class HistorialClinicoConfiguration : IEntityTypeConfiguration<HistorialClinico>
{
    public void Configure(EntityTypeBuilder<HistorialClinico> builder)
    {
        builder.ToTable("HistorialesClinicos", "Clinical");
        builder.HasKey(h => h.Id);

        builder.Property(h => h.Id).ValueGeneratedNever();
        builder.Property(h => h.ClinicaId).HasColumnName("ClinicaId").IsRequired();
        builder.Property(h => h.PacienteId).HasColumnName("PacienteId").IsRequired();
        builder.Property(h => h.FechaCreacion).HasColumnName("FechaCreacion").IsRequired();

        builder.HasIndex(h => h.PacienteId).IsUnique();

        builder.HasMany(h => h.Entradas)
            .WithOne()
            .HasForeignKey("HistorialClinicoId")
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(h => h.Entradas).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Ignore(h => h.DomainEvents);
    }
}
