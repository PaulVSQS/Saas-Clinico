using ClinicaSaaS.Domain.Security;
using ClinicaSaaS.Domain.Security.Enums;
using ClinicaSaaS.Domain.Security.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClinicaSaaS.Persistence.Configurations.Security;

public sealed class ClinicaConfiguration : IEntityTypeConfiguration<Clinica>
{
    public void Configure(EntityTypeBuilder<Clinica> builder)
    {
        builder.ToTable("Clinicas", "Security");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Id).ValueGeneratedNever();
        builder.Property(c => c.RazonSocial).HasColumnName("RazonSocial").HasMaxLength(200).IsRequired();
        builder.Property(c => c.NombreComercial).HasColumnName("NombreComercial").HasMaxLength(200).IsRequired();

        builder.Property(c => c.Rnc)
            .HasColumnName("RNC")
            .HasConversion(rnc => rnc.Valor, valor => Rnc.Crear(valor).Value)
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(c => c.Direccion).HasColumnName("Direccion").HasMaxLength(300);
        builder.Property(c => c.Telefono).HasColumnName("Telefono").HasMaxLength(30);

        builder.Property(c => c.Email)
            .HasColumnName("Email")
            .HasConversion(email => email == null ? null : email.Valor, valor => valor == null ? null : Email.Crear(valor).Value)
            .HasMaxLength(150);

        builder.Property(c => c.PlanSuscripcion)
            .HasColumnName("PlanSuscripcion")
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(c => c.Activo).HasColumnName("Activo").IsRequired();

        builder.Property(c => c.FechaCreacion).HasColumnName("FechaCreacion").IsRequired();
        builder.Property(c => c.CreadoPorUsuarioId).HasColumnName("CreadoPorUsuarioId").IsRequired();
        builder.Property(c => c.FechaModificacion).HasColumnName("FechaModificacion");
        builder.Property(c => c.ModificadoPorUsuarioId).HasColumnName("ModificadoPorUsuarioId");

        builder.Property(c => c.EstaEliminado).HasColumnName("EstaEliminado").IsRequired();
        builder.Property(c => c.FechaEliminacion).HasColumnName("FechaEliminacion");
        builder.Property(c => c.EliminadoPorUsuarioId).HasColumnName("EliminadoPorUsuarioId");

        builder.Property<byte[]>("RowVersion").IsRowVersion();

        builder.HasIndex(c => c.Rnc).IsUnique();

        builder.Ignore(c => c.DomainEvents);
        builder.Ignore(c => c.ClinicaId); // = Id, no es columna propia (ver comentario en Clinica.cs)
    }
}
