using ClinicaSaaS.Domain.Billing;
using ClinicaSaaS.Domain.Billing.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClinicaSaaS.Persistence.Configurations.Billing;

public sealed class SecuenciaComprobanteConfiguration : IEntityTypeConfiguration<SecuenciaComprobante>
{
    public void Configure(EntityTypeBuilder<SecuenciaComprobante> builder)
    {
        builder.ToTable("SecuenciasComprobante", "Billing");
        builder.HasKey(s => s.Id);

        builder.Property(s => s.Id).ValueGeneratedNever();
        builder.Property(s => s.ClinicaId).HasColumnName("ClinicaId").IsRequired();

        builder.Property(s => s.TipoComprobante)
            .HasColumnName("TipoComprobante")
            .HasConversion(t => t.Valor, v => CodigoComprobanteFiscal.Crear(v).Value)
            .HasMaxLength(5)
            .IsRequired();

        builder.Property(s => s.EsElectronico).HasColumnName("EsElectronico").IsRequired();
        builder.Property(s => s.SecuenciaActual).HasColumnName("SecuenciaActual").IsRequired();
        builder.Property(s => s.SecuenciaDesde).HasColumnName("SecuenciaDesde").IsRequired();
        builder.Property(s => s.SecuenciaHasta).HasColumnName("SecuenciaHasta").IsRequired();
        builder.Property(s => s.FechaVencimiento).HasColumnName("FechaVencimiento");
        builder.Property(s => s.Activo).HasColumnName("Activo").IsRequired();

        builder.HasIndex(s => new { s.ClinicaId, s.TipoComprobante }).IsUnique();

        builder.Ignore(s => s.DomainEvents);
    }
}
