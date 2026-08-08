using ClinicaSaaS.Persistence.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClinicaSaaS.Persistence.Configurations.Security;

/// <summary>
/// Mapea el catálogo Security.Roles y siembra sus 5 filas fijas (mismos valores que el
/// enum Domain.Security.Enums.RolClinica) vía HasData, reproduciendo el mismo INSERT
/// fijo que ya trae el script SQL aprobado — esto NO es un cambio de diseño, solo la
/// forma en que EF Core reproduce ese mismo seed dentro de una migración.
/// </summary>
public sealed class RolLookupConfiguration : IEntityTypeConfiguration<RolLookup>
{
    public void Configure(EntityTypeBuilder<RolLookup> builder)
    {
        builder.ToTable("Roles", "Security");
        builder.HasKey(r => r.Id);

        builder.Property(r => r.Id).ValueGeneratedNever();
        builder.Property(r => r.Nombre).HasColumnName("Nombre").HasMaxLength(50).IsRequired();
        builder.HasIndex(r => r.Nombre).IsUnique();

        builder.HasData(
            new RolLookup(1, "AdminClinica"),
            new RolLookup(2, "Recepcion"),
            new RolLookup(3, "Doctor"),
            new RolLookup(4, "Empleado"),
            new RolLookup(5, "Auditor"));
    }
}
