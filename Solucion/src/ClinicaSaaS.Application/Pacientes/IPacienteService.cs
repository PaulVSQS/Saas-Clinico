using ClinicaSaaS.Domain.Personal.Enums;
using ClinicaSaaS.SharedKernel.Results;

namespace ClinicaSaaS.Application.Pacientes;

/// <summary>
/// Vista de un Paciente hacia afuera de Application — mismo criterio de join en memoria con
/// IClinicaService que Doctor/Empleado/Consultorio. TipoDocumento/NumeroDocumento van separados
/// (no como un solo VO serializado) porque el formulario de edición necesita tocarlos por
/// separado; el dominio los reúne de nuevo en DocumentoIdentidad al escribir.
/// </summary>
public sealed record PacienteDto(
    Guid Id,
    Guid ClinicaId,
    string NombreClinica,
    string Nombres,
    string Apellidos,
    string NombreCompleto,
    TipoDocumentoIdentidad? TipoDocumento,
    string? NumeroDocumento,
    DateOnly? FechaNacimiento,
    Genero? Genero,
    string? Telefono,
    string? Email,
    string? Direccion,
    string? ContactoEmergenciaNombre,
    string? ContactoEmergenciaTelefono);

/// <summary>
/// Solo Nombres/Apellidos al registrar — mismo criterio que Paciente.Registrar en el dominio: el
/// resto de los datos (documento, contacto) se completan después vía ActualizarDatosPersonales/
/// ActualizarDatosDeContacto, nunca al alta.
/// </summary>
public sealed record RegistrarPacienteRequest(Guid ClinicaId, string Nombres, string Apellidos);

public sealed record ActualizarDatosPersonalesPacienteRequest(
    string Nombres,
    string Apellidos,
    TipoDocumentoIdentidad? TipoDocumento,
    string? NumeroDocumento,
    DateOnly? FechaNacimiento,
    Genero? Genero);

public sealed record ActualizarDatosDeContactoPacienteRequest(
    string? Telefono,
    string? Email,
    string? Direccion,
    string? ContactoEmergenciaNombre,
    string? ContactoEmergenciaTelefono);

/// <summary>
/// Módulo 7 de Fase 6 — Pacientes: datos personales identificables del paciente (nunca el
/// expediente médico, que vive en el agregado separado HistorialClinico de Clinical — Módulo 9).
/// Mismo criterio de protección que Empleados/Doctores/Consultorios/Horarios: Policy
/// "AdminClinica", con aislamiento de tenant en escritura vía ITenantContext.
///
/// A diferencia de esos módulos, Paciente no tiene Activo/Desactivar/Reactivar — el dominio
/// (Fase 2) nunca modeló ese estado para un paciente, solo EstaEliminado (ver Eliminar).
///
/// DELIBERADAMENTE FUERA de este módulo: historia clínica, citas, archivos médicos — todos
/// agregados separados que referencian a Paciente pero no viven aquí (Módulos 8, 9, 10).
/// </summary>
public interface IPacienteService
{
    /// <summary>
    /// filtro busca por nombres, apellidos o número de documento — se traduce a un WHERE en SQL
    /// Server (ver PacienteService), no trae la tabla completa a memoria para filtrar.
    /// </summary>
    Task<IReadOnlyList<PacienteDto>> ListarAsync(string? filtro = null, CancellationToken cancellationToken = default);

    Task<PacienteDto?> ObtenerAsync(Guid pacienteId, CancellationToken cancellationToken = default);

    Task<Result<Guid>> RegistrarAsync(RegistrarPacienteRequest request, CancellationToken cancellationToken = default);

    Task<Result> ActualizarDatosPersonalesAsync(Guid pacienteId, ActualizarDatosPersonalesPacienteRequest request, CancellationToken cancellationToken = default);

    Task<Result> ActualizarDatosDeContactoAsync(Guid pacienteId, ActualizarDatosDeContactoPacienteRequest request, CancellationToken cancellationToken = default);

    Task<Result> EliminarAsync(Guid pacienteId, CancellationToken cancellationToken = default);
}