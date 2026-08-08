using ClinicaSaaS.Domain.Personal;
using ClinicaSaaS.Domain.Personal.Enums;
using ClinicaSaaS.Domain.Personal.ValueObjects;
using ClinicaSaaS.Domain.Security.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClinicaSaaS.Persistence.Configurations.Personal;

public sealed class PacienteConfiguration : IEntityTypeConfiguration<Paciente>
{
    public void Configure(EntityTypeBuilder<Paciente> builder)
    {
        builder.ToTable("Pacientes", "Personal");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id).ValueGeneratedNever();
        builder.Property(p => p.ClinicaId).HasColumnName("ClinicaId").IsRequired();
        builder.Property(p => p.Nombres).HasColumnName("Nombres").HasMaxLength(150).IsRequired();
        builder.Property(p => p.Apellidos).HasColumnName("Apellidos").HasMaxLength(150).IsRequired();

        // DocumentoIdentidad es un Value Object de dos columnas (Documento + TipoDocumento).
        // OwnsOne con nombres de columna explícitos para calzar exacto con el script SQL.
        builder.OwnsOne(p => p.Documento, doc =>
        {
            doc.Property(d => d.Numero).HasColumnName("Documento").HasMaxLength(20);
            doc.Property(d => d.Tipo).HasColumnName("TipoDocumento").HasConversion<string>().HasMaxLength(20);
        });

        builder.Property(p => p.FechaNacimiento).HasColumnName("FechaNacimiento");

        builder.Property(p => p.Genero)
            .HasColumnName("Sexo")
            .HasConversion(
                g => g == Genero.Masculino ? "M" : g == Genero.Femenino ? "F" : null,
                s => s == "M" ? Genero.Masculino : s == "F" ? Genero.Femenino : (Genero?)null)
            .HasMaxLength(1)
            .IsFixedLength();

        builder.Property(p => p.Telefono).HasColumnName("Telefono").HasMaxLength(30);

        builder.Property(p => p.Email)
            .HasColumnName("Email")
            .HasConversion(e => e == null ? null : e.Valor, v => v == null ? null : Email.Crear(v).Value)
            .HasMaxLength(150);

        builder.Property(p => p.Direccion).HasColumnName("Direccion").HasMaxLength(300);

        builder.OwnsOne(p => p.ContactoEmergencia, c =>
        {
            c.Property(x => x.Nombre).HasColumnName("ContactoEmergenciaNombre").HasMaxLength(150);
            c.Property(x => x.Telefono).HasColumnName("ContactoEmergenciaTelefono").HasMaxLength(30);
        });

        builder.Property(p => p.FechaCreacion).HasColumnName("FechaCreacion").IsRequired();
        builder.Property(p => p.CreadoPorUsuarioId).HasColumnName("CreadoPorUsuarioId").IsRequired();
        builder.Property(p => p.FechaModificacion).HasColumnName("FechaModificacion");
        builder.Property(p => p.ModificadoPorUsuarioId).HasColumnName("ModificadoPorUsuarioId");

        builder.Property(p => p.EstaEliminado).HasColumnName("EstaEliminado").IsRequired();
        builder.Property(p => p.FechaEliminacion).HasColumnName("FechaEliminacion");
        builder.Property(p => p.EliminadoPorUsuarioId).HasColumnName("EliminadoPorUsuarioId");

        builder.Property<byte[]>("RowVersion").IsRowVersion();

        builder.HasIndex(p => new { p.ClinicaId, p.Apellidos, p.Nombres });

        builder.Ignore(p => p.DomainEvents);
        builder.Ignore(p => p.NombreCompleto);
    }
}
