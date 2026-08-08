using ClinicaSaaS.Domain.Scheduling;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClinicaSaaS.Persistence.Configurations.Scheduling;

public sealed class EquipamientoConfiguration : IEntityTypeConfiguration<Equipamiento>
{
    public void Configure(EntityTypeBuilder<Equipamiento> builder)
    {
        builder.ToTable("Equipamiento", "Scheduling");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id).ValueGeneratedNever();
        builder.Property(e => e.ClinicaId).HasColumnName("ClinicaId").IsRequired();
        builder.Property(e => e.ConsultorioId).HasColumnName("ConsultorioId");
        builder.Property(e => e.Nombre).HasColumnName("Nombre").HasMaxLength(150).IsRequired();
        builder.Property(e => e.NumeroSerie).HasColumnName("NumeroSerie").HasMaxLength(100);
        builder.Property(e => e.FechaAdquisicion).HasColumnName("FechaAdquisicion");
        builder.Property(e => e.ProximoMantenimiento).HasColumnName("ProximoMantenimiento");
        builder.Property(e => e.Activo).HasColumnName("Activo").IsRequired();

        builder.Property(e => e.FechaCreacion).HasColumnName("FechaCreacion").IsRequired();
        builder.Property(e => e.CreadoPorUsuarioId).HasColumnName("CreadoPorUsuarioId").IsRequired();
        builder.Property(e => e.FechaModificacion).HasColumnName("FechaModificacion");
        builder.Property(e => e.ModificadoPorUsuarioId).HasColumnName("ModificadoPorUsuarioId");

        builder.Property(e => e.EstaEliminado).HasColumnName("EstaEliminado").IsRequired();
        builder.Property(e => e.FechaEliminacion).HasColumnName("FechaEliminacion");
        builder.Property(e => e.EliminadoPorUsuarioId).HasColumnName("EliminadoPorUsuarioId");

        builder.HasOne<Consultorio>().WithMany().HasForeignKey(e => e.ConsultorioId).OnDelete(DeleteBehavior.Restrict);

        builder.Ignore(e => e.DomainEvents);
    }
}
