using ClinicaSaaS.Domain.Personal;
using ClinicaSaaS.Domain.Personal.Entities;
using ClinicaSaaS.Domain.Personal.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClinicaSaaS.Persistence.Configurations.Personal;

public sealed class DoctorConfiguration : IEntityTypeConfiguration<Doctor>
{
    public void Configure(EntityTypeBuilder<Doctor> builder)
    {
        builder.ToTable("Doctores", "Personal");
        builder.HasKey(d => d.Id);

        builder.Property(d => d.Id).ValueGeneratedNever();
        builder.Property(d => d.UsuarioId).HasColumnName("UsuarioId").IsRequired();
        builder.Property(d => d.ClinicaId).HasColumnName("ClinicaId").IsRequired();
        builder.Property(d => d.Especialidad).HasColumnName("Especialidad").HasMaxLength(150).IsRequired();
        builder.Property(d => d.NumeroExequatur).HasColumnName("NumeroExequatur").HasMaxLength(50);
        builder.Property(d => d.Activo).HasColumnName("Activo").IsRequired();

        builder.Property(d => d.FechaCreacion).HasColumnName("FechaCreacion").IsRequired();
        builder.Property(d => d.CreadoPorUsuarioId).HasColumnName("CreadoPorUsuarioId").IsRequired();
        builder.Property(d => d.FechaModificacion).HasColumnName("FechaModificacion");
        builder.Property(d => d.ModificadoPorUsuarioId).HasColumnName("ModificadoPorUsuarioId");

        builder.Property(d => d.EstaEliminado).HasColumnName("EstaEliminado").IsRequired();
        builder.Property(d => d.FechaEliminacion).HasColumnName("FechaEliminacion");
        builder.Property(d => d.EliminadoPorUsuarioId).HasColumnName("EliminadoPorUsuarioId");

        builder.HasIndex(d => new { d.UsuarioId, d.ClinicaId }).IsUnique();

        // HorarioDoctor: Entity interna del agregado, tabla propia en el esquema Scheduling
        // (el diseño de BD la ubica ahí, no en Personal, aunque pertenezca al agregado Doctor
        // en el dominio — un mapeo perfectamente válido en EF Core, la tabla física no tiene
        // por qué coincidir con la organización de carpetas del código).
        builder.HasMany(d => d.Horarios)
            .WithOne()
            .HasForeignKey("DoctorId")
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(d => d.Horarios).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Ignore(d => d.DomainEvents);
    }
}

public sealed class HorarioDoctorConfiguration : IEntityTypeConfiguration<HorarioDoctor>
{
    public void Configure(EntityTypeBuilder<HorarioDoctor> builder)
    {
        builder.ToTable("HorariosDoctor", "Scheduling");
        builder.HasKey(h => h.Id);

        builder.Property(h => h.Id).ValueGeneratedNever();
        builder.Property<Guid>("DoctorId").HasColumnName("DoctorId").IsRequired();
        builder.Property(h => h.ConsultorioId).HasColumnName("ConsultorioId");

        builder.Property(h => h.TipoHorario).HasColumnName("TipoHorario").HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(h => h.DiaSemana)
            .HasColumnName("DiaSemana")
            .HasConversion(d => d == null ? (byte?)null : (byte)d.Value, b => b == null ? (DayOfWeek?)null : (DayOfWeek)b.Value);

        builder.OwnsOne(h => h.Bloque, b =>
        {
            b.Property(x => x.HoraInicio).HasColumnName("HoraInicio").IsRequired();
            b.Property(x => x.HoraFin).HasColumnName("HoraFin").IsRequired();
        });

        builder.OwnsOne(h => h.Vigencia, v =>
        {
            v.Property(x => x.FechaInicio).HasColumnName("FechaInicioVigencia").IsRequired();
            v.Property(x => x.FechaFin).HasColumnName("FechaFinVigencia");
        });

        builder.Property(h => h.Activo).HasColumnName("Activo").IsRequired();

        // EstaEliminado existe en la tabla física (HorariosDoctor.EstaEliminado, ver script SQL)
        // pero HorarioDoctor (Entity interna, no Aggregate Root) no implementa ISoftDeletable en
        // el dominio — su ciclo de vida se controla con Activo, no con soft-delete propio (ver
        // justificación en HorarioDoctor.cs de Fase 2). Se mapea igual como shadow property para
        // no perder la columna de la BD aprobada, fijada siempre en 0 desde este contexto.
        builder.Property<bool>("EstaEliminado").HasColumnName("EstaEliminado").HasDefaultValue(false);

        builder.HasIndex("DoctorId");
        builder.HasIndex(h => h.ConsultorioId);
    }
}
