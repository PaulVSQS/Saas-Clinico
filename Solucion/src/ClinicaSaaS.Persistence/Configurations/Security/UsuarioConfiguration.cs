using ClinicaSaaS.Domain.Security;
using ClinicaSaaS.Domain.Security.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClinicaSaaS.Persistence.Configurations.Security;

public sealed class UsuarioConfiguration : IEntityTypeConfiguration<Usuario>
{
    public void Configure(EntityTypeBuilder<Usuario> builder)
    {
        builder.ToTable("Usuarios", "Security");
        builder.HasKey(u => u.Id);

        builder.Property(u => u.Id).ValueGeneratedNever();

        builder.Property(u => u.Email)
            .HasColumnName("Email")
            .HasConversion(email => email.Valor, valor => Email.Crear(valor).Value)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(u => u.PasswordHash).HasColumnName("PasswordHash").HasMaxLength(256).IsRequired();
        builder.Property(u => u.NombreCompleto).HasColumnName("NombreCompleto").HasMaxLength(200).IsRequired();
        builder.Property(u => u.Telefono).HasColumnName("Telefono").HasMaxLength(30);
        builder.Property(u => u.EsSuperAdminSaaS).HasColumnName("EsSuperAdminSaaS").IsRequired();
        builder.Property(u => u.Activo).HasColumnName("Activo").IsRequired();

        builder.Property(u => u.FechaCreacion).HasColumnName("FechaCreacion").IsRequired();
        builder.Property(u => u.CreadoPorUsuarioId).HasColumnName("CreadoPorUsuarioId").IsRequired();
        builder.Property(u => u.FechaModificacion).HasColumnName("FechaModificacion");
        builder.Property(u => u.ModificadoPorUsuarioId).HasColumnName("ModificadoPorUsuarioId");

        builder.Property(u => u.EstaEliminado).HasColumnName("EstaEliminado").IsRequired();
        builder.Property(u => u.FechaEliminacion).HasColumnName("FechaEliminacion");
        builder.Property(u => u.EliminadoPorUsuarioId).HasColumnName("EliminadoPorUsuarioId");

        builder.Property<byte[]>("RowVersion").IsRowVersion();

        builder.HasIndex(u => u.Email).IsUnique();

        builder.Ignore(u => u.DomainEvents);
    }
}
