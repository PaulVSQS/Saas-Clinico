using ClinicaSaaS.Domain.Billing;
using ClinicaSaaS.Domain.Common.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClinicaSaaS.Persistence.Configurations.Billing;

public sealed class FacturaConfiguration : IEntityTypeConfiguration<Factura>
{
    public void Configure(EntityTypeBuilder<Factura> builder)
    {
        builder.ToTable("Facturas", "Billing");
        builder.HasKey(f => f.Id);

        builder.Property(f => f.Id).ValueGeneratedNever();
        builder.Property(f => f.ClinicaId).HasColumnName("ClinicaId").IsRequired();
        builder.Property(f => f.PacienteId).HasColumnName("PacienteId").IsRequired();
        builder.Property(f => f.TipoFactura).HasColumnName("TipoFactura").HasConversion<string>().HasMaxLength(10).IsRequired();
        builder.Property(f => f.NumeroComprobante).HasColumnName("NumeroComprobante").HasMaxLength(20);
        builder.Property(f => f.EstadoDGII).HasColumnName("EstadoDGII").HasConversion<string>().HasMaxLength(20);
        builder.Property(f => f.FechaEmision).HasColumnName("FechaEmision").IsRequired();

        // Subtotal y Total son propiedades CALCULADAS en el dominio (siempre derivadas de la
        // suma de Detalles, nunca un campo editable) — pero la BD aprobada sí las guarda como
        // columnas físicas (para reportería sin tener que recalcular sumando detalles en cada
        // consulta). Se mapean como shadow properties con un nombre DISTINTO al de la
        // propiedad CLR calculada (para no chocar con el Ignore() de abajo) y el interceptor
        // de guardado (FacturaTotalesSaveChangesInterceptor) las sincroniza con el valor
        // calculado justo antes de cada SaveChanges — el dominio sigue siendo la única fuente
        // de verdad del cálculo, la columna es solo una caché persistida de su resultado.
        builder.Property<decimal>("SubtotalAlmacenado").HasColumnName("Subtotal").IsRequired();

        builder.Property(f => f.Descuento)
            .HasColumnName("Descuento")
            .HasConversion(d => d.Monto, m => Dinero.Crear(m).Value)
            .IsRequired();

        builder.Property(f => f.Impuestos)
            .HasColumnName("Impuestos")
            .HasConversion(d => d.Monto, m => Dinero.Crear(m).Value)
            .IsRequired();

        builder.Property<decimal>("TotalAlmacenado").HasColumnName("Total").IsRequired();

        builder.Property(f => f.Estado).HasColumnName("Estado").HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(f => f.EstaAnulada).HasColumnName("EstaAnulada").IsRequired();
        builder.Property(f => f.MotivoAnulacion).HasColumnName("MotivoAnulacion").HasMaxLength(300);
        builder.Property(f => f.CreadoPorUsuarioId).HasColumnName("CreadoPorUsuarioId").IsRequired();

        builder.Property(f => f.EstaEliminado).HasColumnName("EstaEliminado").IsRequired();
        builder.Property(f => f.FechaEliminacion).HasColumnName("FechaEliminacion");
        builder.Property(f => f.EliminadoPorUsuarioId).HasColumnName("EliminadoPorUsuarioId");

        builder.Property<byte[]>("RowVersion").IsRowVersion();

        builder.HasIndex(f => new { f.ClinicaId, f.FechaEmision });
        builder.HasIndex(f => f.PacienteId);
        builder.HasIndex(f => new { f.ClinicaId, f.NumeroComprobante }).IsUnique();

        builder.HasMany(f => f.Detalles).WithOne().HasForeignKey("FacturaId").OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(f => f.Detalles).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(f => f.Pagos).WithOne().HasForeignKey("FacturaId").OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(f => f.Pagos).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Ignore(f => f.Subtotal);
        builder.Ignore(f => f.Total);
        builder.Ignore(f => f.TotalPagado);
        builder.Ignore(f => f.SaldoPendiente);
        builder.Ignore(f => f.DomainEvents);
    }
}
