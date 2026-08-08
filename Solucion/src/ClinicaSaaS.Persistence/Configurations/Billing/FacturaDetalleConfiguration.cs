using ClinicaSaaS.Domain.Billing.Entities;
using ClinicaSaaS.Domain.Clinical.Entities;
using ClinicaSaaS.Domain.Common.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClinicaSaaS.Persistence.Configurations.Billing;

public sealed class FacturaDetalleConfiguration : IEntityTypeConfiguration<FacturaDetalle>
{
    public void Configure(EntityTypeBuilder<FacturaDetalle> builder)
    {
        builder.ToTable("FacturaDetalles", "Billing");
        builder.HasKey(d => d.Id);

        builder.Property(d => d.Id).ValueGeneratedNever();
        builder.Property<Guid>("FacturaId").HasColumnName("FacturaId").IsRequired();
        builder.Property(d => d.ProcedimientoRealizadoId).HasColumnName("ProcedimientoRealizadoId");
        builder.Property(d => d.Descripcion).HasColumnName("Descripcion").HasMaxLength(300).IsRequired();
        builder.Property(d => d.Cantidad).HasColumnName("Cantidad").IsRequired();

        builder.Property(d => d.PrecioUnitario)
            .HasColumnName("PrecioUnitario")
            .HasConversion(m => m.Monto, v => Dinero.Crear(v).Value)
            .IsRequired();

        // Mismo patrón que Factura.Subtotal/Total: es calculado en el dominio
        // (Cantidad * PrecioUnitario) pero la BD lo guarda como columna física.
        builder.Property<decimal>("SubtotalAlmacenado").HasColumnName("Subtotal").IsRequired();

        builder.HasOne<ProcedimientoRealizado>().WithMany().HasForeignKey(d => d.ProcedimientoRealizadoId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex("FacturaId");

        builder.Ignore(d => d.Subtotal);
        builder.Ignore(d => d.DomainEvents);
    }
}
