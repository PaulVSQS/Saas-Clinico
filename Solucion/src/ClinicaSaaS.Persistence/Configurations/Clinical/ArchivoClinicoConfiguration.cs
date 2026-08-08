using ClinicaSaaS.Domain.Clinical.Entities;
using ClinicaSaaS.Domain.Clinical.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClinicaSaaS.Persistence.Configurations.Clinical;

public sealed class ArchivoClinicoConfiguration : IEntityTypeConfiguration<ArchivoClinico>
{
    public void Configure(EntityTypeBuilder<ArchivoClinico> builder)
    {
        builder.ToTable("ArchivosClinicos", "Clinical");
        builder.HasKey(a => a.Id);

        builder.Property(a => a.Id).ValueGeneratedNever();
        builder.Property<Guid>("HistorialClinicoEntradaId").HasColumnName("HistorialClinicoEntradaId").IsRequired();

        builder.Property(a => a.TipoArchivo).HasColumnName("TipoArchivo").HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(a => a.NombreOriginal).HasColumnName("NombreOriginal").HasMaxLength(260).IsRequired();
        builder.Property(a => a.RutaAlmacenamiento).HasColumnName("RutaAlmacenamiento").HasMaxLength(500).IsRequired();

        builder.Property(a => a.Hash)
            .HasColumnName("HashSHA256")
            .HasConversion(h => h.Valor, v => HashArchivo.Crear(v).Value)
            .HasMaxLength(64)
            .IsFixedLength()
            .IsRequired();

        builder.Property(a => a.TamanoBytes).HasColumnName("TamanoBytes").IsRequired();
        builder.Property(a => a.SubidoPorUsuarioId).HasColumnName("SubidoPorUsuarioId").IsRequired();
        builder.Property(a => a.FechaSubida).HasColumnName("FechaSubida").IsRequired();

        builder.Property(a => a.EstaEliminado).HasColumnName("EstaEliminado").IsRequired();
        builder.Property(a => a.FechaEliminacion).HasColumnName("FechaEliminacion");
        builder.Property(a => a.EliminadoPorUsuarioId).HasColumnName("EliminadoPorUsuarioId");

        builder.HasIndex("HistorialClinicoEntradaId");

        builder.Ignore(a => a.DomainEvents);
    }
}
