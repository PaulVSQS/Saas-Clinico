using ClinicaSaaS.Domain.Security;
using ClinicaSaaS.Persistence.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClinicaSaaS.Persistence.Configurations.Security;

public sealed class UsuarioClinicaRolConfiguration : IEntityTypeConfiguration<UsuarioClinicaRol>
{
    public void Configure(EntityTypeBuilder<UsuarioClinicaRol> builder)
    {
        builder.ToTable("UsuarioClinicaRoles", "Security");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.UsuarioId).HasColumnName("UsuarioId").IsRequired();
        builder.Property(x => x.ClinicaId).HasColumnName("ClinicaId").IsRequired();

        builder.Property(x => x.Rol)
            .HasColumnName("RolId")
            .HasConversion<int>()
            .IsRequired();

        builder.Property(x => x.FechaAsignacion).HasColumnName("FechaAsignacion").IsRequired();
        builder.Property(x => x.AsignadoPorUsuarioId).HasColumnName("AsignadoPorUsuarioId");
        builder.Property(x => x.Activo).HasColumnName("Activo").IsRequired();

        builder.Property(x => x.EstaEliminado).HasColumnName("EstaEliminado").IsRequired();
        builder.Property(x => x.FechaEliminacion).HasColumnName("FechaEliminacion");
        builder.Property(x => x.EliminadoPorUsuarioId).HasColumnName("EliminadoPorUsuarioId");

        builder.HasOne<Usuario>().WithMany().HasForeignKey(x => x.UsuarioId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Clinica>().WithMany().HasForeignKey(x => x.ClinicaId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Usuario>().WithMany().HasForeignKey(x => x.AsignadoPorUsuarioId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.UsuarioId, x.ClinicaId, x.Rol }).IsUnique();
        builder.HasIndex(x => x.ClinicaId);
        builder.HasIndex(x => x.UsuarioId);

        builder.Ignore(x => x.DomainEvents);
    }
}
