using ClinicaSaaS.Domain.Billing.Entities;
using ClinicaSaaS.Domain.Common.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClinicaSaaS.Persistence.Configurations.Billing;

public sealed class PagoConfiguration : IEntityTypeConfiguration<Pago>
{
    public void Configure(EntityTypeBuilder<Pago> builder)
    {
        builder.ToTable("Pagos", "Billing");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id).ValueGeneratedNever();
        builder.Property<Guid>("FacturaId").HasColumnName("FacturaId").IsRequired();

        builder.Property(p => p.Monto)
            .HasColumnName("Monto")
            .HasConversion(m => m.Monto, v => Dinero.Crear(v).Value)
            .IsRequired();

        builder.Property(p => p.FechaHora).HasColumnName("FechaHora").IsRequired();
        builder.Property(p => p.MetodoPago).HasColumnName("MetodoPago").HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(p => p.Referencia).HasColumnName("Referencia").HasMaxLength(100);
        builder.Property(p => p.RegistradoPorUsuarioId).HasColumnName("RegistradoPorUsuarioId").IsRequired();
        builder.Property(p => p.EstaAnulado).HasColumnName("EstaAnulado").IsRequired();
        builder.Property(p => p.MotivoAnulacion).HasColumnName("MotivoAnulacion").HasMaxLength(300);

        builder.Property<byte[]>("RowVersion").IsRowVersion();

        builder.HasIndex("FacturaId");
        builder.HasIndex(p => p.FechaHora);

        builder.Ignore(p => p.DomainEvents);
    }
}
