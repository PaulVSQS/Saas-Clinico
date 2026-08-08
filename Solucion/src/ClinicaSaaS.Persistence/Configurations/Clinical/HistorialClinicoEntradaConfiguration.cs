using ClinicaSaaS.Domain.Clinical.Entities;
using ClinicaSaaS.Domain.Clinical.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClinicaSaaS.Persistence.Configurations.Clinical;

public sealed class HistorialClinicoEntradaConfiguration : IEntityTypeConfiguration<HistorialClinicoEntrada>
{
    public void Configure(EntityTypeBuilder<HistorialClinicoEntrada> builder)
    {
        builder.ToTable("HistorialClinicoEntradas", "Clinical");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id).ValueGeneratedNever();
        builder.Property<Guid>("HistorialClinicoId").HasColumnName("HistorialClinicoId").IsRequired();
        builder.Property(e => e.Version).HasColumnName("Version").IsRequired();
        builder.Property(e => e.EntradaAnteriorId).HasColumnName("EntradaAnteriorId");
        builder.Property(e => e.DoctorId).HasColumnName("DoctorId").IsRequired();
        builder.Property(e => e.CitaId).HasColumnName("CitaId");
        builder.Property(e => e.MotivoConsulta).HasColumnName("MotivoConsulta").HasMaxLength(500);
        builder.Property(e => e.Diagnostico).HasColumnName("Diagnostico");
        builder.Property(e => e.Tratamiento).HasColumnName("Tratamiento");
        builder.Property(e => e.Notas).HasColumnName("Notas");

        // SignosVitales es un Value Object de 8 campos, pero la BD aprobada lo guarda como
        // un único JSON flexible (SignosVitalesJson), no como columnas planas — se respeta el
        // diseño físico con un ValueConverter de serialización, manteniendo el VO tipado en
        // memoria para las reglas de negocio del dominio.
        var signosVitalesComparer = new ValueComparer<SignosVitales?>(
            (a, b) => (a == null && b == null) || (a != null && b != null && a.Equals(b)),
            v => v == null ? 0 : v.GetHashCode(),
            v => v);

        builder.Property(e => e.SignosVitales)
            .HasColumnName("SignosVitalesJson")
            .HasConversion(
                sv => SignosVitalesJsonMapper.Serializar(sv),
                json => SignosVitalesJsonMapper.Deserializar(json))
            .Metadata.SetValueComparer(signosVitalesComparer);

        builder.Property(e => e.FechaRegistro).HasColumnName("FechaRegistro").IsRequired();
        builder.Property(e => e.EsVersionActual).HasColumnName("EsVersionActual").IsRequired();

        builder.HasIndex("HistorialClinicoId", nameof(HistorialClinicoEntrada.EsVersionActual));
        builder.HasIndex(e => e.DoctorId);
        builder.HasIndex("HistorialClinicoId", nameof(HistorialClinicoEntrada.Version)).IsUnique();

        // Auto-referencia: la entrada anterior en la cadena de versiones del expediente.
        builder.HasOne<HistorialClinicoEntrada>().WithMany().HasForeignKey(e => e.EntradaAnteriorId).OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(e => e.Archivos)
            .WithOne()
            .HasForeignKey("HistorialClinicoEntradaId")
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(e => e.Archivos).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(e => e.ProcedimientosRealizados)
            .WithOne()
            .HasForeignKey("HistorialClinicoEntradaId")
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(e => e.ProcedimientosRealizados).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Ignore(e => e.DomainEvents);
    }
}
