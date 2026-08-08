using ClinicaSaaS.Domain.Clinical;
using ClinicaSaaS.Domain.Clinical.Entities;
using ClinicaSaaS.Domain.Common.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClinicaSaaS.Persistence.Configurations.Clinical;

public sealed class ProcedimientoRealizadoConfiguration : IEntityTypeConfiguration<ProcedimientoRealizado>
{
    public void Configure(EntityTypeBuilder<ProcedimientoRealizado> builder)
    {
        builder.ToTable("ProcedimientosRealizados", "Clinical");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id).ValueGeneratedNever();
        builder.Property<Guid>("HistorialClinicoEntradaId").HasColumnName("HistorialClinicoEntradaId").IsRequired();
        builder.Property(p => p.ProcedimientoCatalogoId).HasColumnName("ProcedimientoCatalogoId").IsRequired();
        builder.Property(p => p.DoctorId).HasColumnName("DoctorId").IsRequired();
        builder.Property(p => p.PiezaDental).HasColumnName("PiezaDental").HasMaxLength(10);

        builder.Property(p => p.PrecioAplicado)
            .HasColumnName("PrecioAplicado")
            .HasConversion(d => d.Monto, m => Dinero.Crear(m).Value)
            .IsRequired();

        builder.Property(p => p.Fecha).HasColumnName("Fecha").IsRequired();
        builder.Property(p => p.Notas).HasColumnName("Notas").HasMaxLength(500);

        builder.HasOne<ProcedimientoCatalogo>().WithMany().HasForeignKey(p => p.ProcedimientoCatalogoId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex("HistorialClinicoEntradaId");

        builder.Ignore(p => p.DomainEvents);
    }
}
