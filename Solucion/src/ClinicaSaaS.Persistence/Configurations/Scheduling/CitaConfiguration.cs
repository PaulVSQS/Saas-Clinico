using ClinicaSaaS.Domain.Personal;
using ClinicaSaaS.Domain.Scheduling;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClinicaSaaS.Persistence.Configurations.Scheduling;

public sealed class CitaConfiguration : IEntityTypeConfiguration<Cita>
{
    public void Configure(EntityTypeBuilder<Cita> builder)
    {
        builder.ToTable("Citas", "Scheduling");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Id).ValueGeneratedNever();
        builder.Property(c => c.ClinicaId).HasColumnName("ClinicaId").IsRequired();
        builder.Property(c => c.PacienteId).HasColumnName("PacienteId").IsRequired();
        builder.Property(c => c.DoctorId).HasColumnName("DoctorId").IsRequired();
        builder.Property(c => c.ConsultorioId).HasColumnName("ConsultorioId");
        builder.Property(c => c.FechaHoraInicio).HasColumnName("FechaHoraInicio").IsRequired();
        builder.Property(c => c.FechaHoraFin).HasColumnName("FechaHoraFin").IsRequired();

        builder.Property(c => c.Estado).HasColumnName("Estado").HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(c => c.MotivoConsulta).HasColumnName("MotivoConsulta").HasMaxLength(300);

        // La tabla Citas no trae CreadoPorUsuarioId aparte: es el mismo dato que
        // AuditableEntity.CreadoPorUsuarioId (columna DE AUDITORÍA), pero el dominio expone
        // ADEMÁS CreadoPorUsuarioId como propiedad propia de negocio (quién agendó la cita,
        // un dato de negocio, no solo de auditoría técnica) — ambas apuntan a la MISMA columna
        // física CreadoPorUsuarioId del script SQL, así que se mapea una sola vez y se ignora
        // la duplicada para no generar dos columnas.
        builder.Property(c => c.FechaCreacion).HasColumnName("FechaCreacion").IsRequired();
        builder.Property(c => c.CreadoPorUsuarioId).HasColumnName("CreadoPorUsuarioId").IsRequired();
        builder.Property(c => c.FechaModificacion).HasColumnName("FechaModificacion");
        builder.Property(c => c.ModificadoPorUsuarioId).HasColumnName("ModificadoPorUsuarioId");

        builder.Property(c => c.EstaEliminado).HasColumnName("EstaEliminado").IsRequired();
        builder.Property(c => c.FechaEliminacion).HasColumnName("FechaEliminacion");
        builder.Property(c => c.EliminadoPorUsuarioId).HasColumnName("EliminadoPorUsuarioId");

        builder.Property<byte[]>("RowVersion").IsRowVersion();

        builder.HasOne<Paciente>().WithMany().HasForeignKey(c => c.PacienteId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Doctor>().WithMany().HasForeignKey(c => c.DoctorId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Consultorio>().WithMany().HasForeignKey(c => c.ConsultorioId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(c => new { c.DoctorId, c.FechaHoraInicio });
        builder.HasIndex(c => c.PacienteId);
        builder.HasIndex(c => new { c.ClinicaId, c.FechaHoraInicio });

        builder.Ignore(c => c.DomainEvents);
    }
}
