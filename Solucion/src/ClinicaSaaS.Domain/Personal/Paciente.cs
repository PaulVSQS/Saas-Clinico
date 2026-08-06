using ClinicaSaaS.Domain.Personal.Enums;
using ClinicaSaaS.Domain.Personal.Events;
using ClinicaSaaS.Domain.Personal.ValueObjects;
using ClinicaSaaS.Domain.Security.ValueObjects;
using ClinicaSaaS.SharedKernel.Abstractions;
using ClinicaSaaS.SharedKernel.Common;
using ClinicaSaaS.SharedKernel.Results;

namespace ClinicaSaaS.Domain.Personal;

/// <summary>
/// Aggregate Root. Contiene exclusivamente los datos personales identificables del paciente —
/// el expediente médico vive en el agregado separado HistorialClinico (Clinical). Esta
/// separación es un requisito de negocio explícito y además facilita cumplir portabilidad o
/// rectificación de datos personales (Ley 172-13 RD) sin tocar el historial clínico legal.
/// </summary>
public sealed class Paciente : SoftDeleteEntity, ITenantEntity
{
    public Guid ClinicaId { get; private set; }
    public string Nombres { get; private set; }
    public string Apellidos { get; private set; }
    public DocumentoIdentidad? Documento { get; private set; }
    public DateOnly? FechaNacimiento { get; private set; }
    public string? Telefono { get; private set; }
    public Email? Email { get; private set; }
    public string? Direccion { get; private set; }
    public ContactoEmergencia? ContactoEmergencia { get; private set; }
    public Enums.Genero? Genero { get; private set; }

    public string NombreCompleto => $"{Nombres} {Apellidos}";

    private Paciente() { } // EF Core

    private Paciente(Guid id, Guid clinicaId, string nombres, string apellidos) : base(id)
    {
        ClinicaId = clinicaId;
        Nombres = nombres;
        Apellidos = apellidos;
    }

    public static Result<Paciente> Registrar(Guid id, Guid clinicaId, string nombres, string apellidos, Guid usuarioId, DateTime fechaUtc)
    {
        Guard.ContraGuidVacio(id, nameof(id));
        Guard.ContraGuidVacio(clinicaId, nameof(clinicaId));

        if (string.IsNullOrWhiteSpace(nombres))
            return new Error("Paciente.NombresRequeridos", "Los nombres son requeridos.");

        if (string.IsNullOrWhiteSpace(apellidos))
            return new Error("Paciente.ApellidosRequeridos", "Los apellidos son requeridos.");

        var paciente = new Paciente(id, clinicaId, nombres.Trim(), apellidos.Trim());
        paciente.RaiseEvent(new PacienteRegistradoDomainEvent(id, clinicaId, fechaUtc));
        return paciente;
    }

    public Result ActualizarDatosPersonales(
        string nombres, string apellidos, DocumentoIdentidad? documento, DateOnly? fechaNacimiento, Enums.Genero? genero)
    {
        if (string.IsNullOrWhiteSpace(nombres))
            return new Error("Paciente.NombresRequeridos", "Los nombres son requeridos.");

        if (string.IsNullOrWhiteSpace(apellidos))
            return new Error("Paciente.ApellidosRequeridos", "Los apellidos son requeridos.");

        if (fechaNacimiento is not null && fechaNacimiento > DateOnly.FromDateTime(DateTime.UtcNow))
            return new Error("Paciente.FechaNacimientoFutura", "La fecha de nacimiento no puede ser futura.");

        Nombres = nombres.Trim();
        Apellidos = apellidos.Trim();
        Documento = documento;
        FechaNacimiento = fechaNacimiento;
        Genero = genero;
        return Result.Exitoso();
    }

    public Result ActualizarDatosDeContacto(string? telefono, Email? email, string? direccion, ContactoEmergencia? contactoEmergencia)
    {
        Telefono = telefono?.Trim();
        Email = email;
        Direccion = direccion?.Trim();
        ContactoEmergencia = contactoEmergencia;
        return Result.Exitoso();
    }

    public new Result Eliminar(Guid usuarioId, DateTime fechaUtc)
    {
        if (EstaEliminado)
            return new Error("Paciente.YaEliminado", "El paciente ya está eliminado.");

        base.Eliminar(usuarioId, fechaUtc);
        return Result.Exitoso();
    }
}
