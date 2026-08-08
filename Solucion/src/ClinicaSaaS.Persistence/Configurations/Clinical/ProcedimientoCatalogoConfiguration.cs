using ClinicaSaaS.Domain.Clinical;
using ClinicaSaaS.Domain.Common.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClinicaSaaS.Persistence.Configurations.Clinical;

public sealed class ProcedimientoCatalogoConfiguration : IEntityTypeConfiguration<ProcedimientoCatalogo>
{
    public void Configure(EntityTypeBuilder<ProcedimientoCatalogo> builder)
    {
        builder.ToTable("ProcedimientosCatalogo", "Clinical");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id).ValueGeneratedNever();
        builder.Property(p => p.ClinicaId).HasColumnName("ClinicaId").IsRequired();
        builder.Property(p => p.Codigo).HasColumnName("Codigo").HasMaxLength(30).IsRequired();
        builder.Property(p => p.Nombre).HasColumnName("Nombre").HasMaxLength(200).IsRequired();
        builder.Property(p => p.Descripcion).HasColumnName("Descripcion").HasMaxLength(500);

        builder.Property(p => p.PrecioBase)
            .HasColumnName("PrecioBase")
            .HasConversion(d => d.Monto, m => Dinero.Crear(m).Value)
            .IsRequired();

        builder.Property(p => p.Activo).HasColumnName("Activo").IsRequired();

        builder.Property(p => p.FechaCreacion).HasColumnName("FechaCreacion").IsRequired();
        builder.Property(p => p.CreadoPorUsuarioId).HasColumnName("CreadoPorUsuarioId").IsRequired();
        builder.Property(p => p.FechaModificacion).HasColumnName("FechaModificacion");
        builder.Property(p => p.ModificadoPorUsuarioId).HasColumnName("ModificadoPorUsuarioId");

        builder.Property(p => p.EstaEliminado).HasColumnName("EstaEliminado").IsRequired();
        builder.Property(p => p.FechaEliminacion).HasColumnName("FechaEliminacion");
        builder.Property(p => p.EliminadoPorUsuarioId).HasColumnName("EliminadoPorUsuarioId");

        builder.HasIndex(p => new { p.ClinicaId, p.Codigo }).IsUnique();

        builder.Ignore(p => p.DomainEvents);
    }
}
