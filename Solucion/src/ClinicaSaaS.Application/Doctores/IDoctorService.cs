using ClinicaSaaS.SharedKernel.Results;

namespace ClinicaSaaS.Application.Doctores;

/// <summary>
/// Vista de un Doctor hacia afuera de Application — igual criterio que EmpleadoDto (Fase 6,
/// Módulo 3): datos del Usuario y de la Clínica resueltos vía join en memoria con
/// IUsuarioService/IClinicaService.
/// </summary>
public sealed record DoctorDto(
    Guid Id,
    Guid UsuarioId,
    string NombreUsuario,
    string EmailUsuario,
    Guid ClinicaId,
    string NombreClinica,
    string Especialidad,
    string? NumeroExequatur,
    bool Activo);

public sealed record RegistrarDoctorRequest(Guid UsuarioId, Guid ClinicaId, string Especialidad, string? NumeroExequatur);

public sealed record ActualizarDatosDoctorRequest(string Especialidad, string? NumeroExequatur);

/// <summary>
/// Módulo 4 de Fase 6 — Doctores: el perfil profesional del doctor dentro de una clínica
/// (especialidad, número de exequátur, activo/inactivo). Mismo criterio de protección que
/// Empleados (Módulo 3): Policy "AdminClinica", con aislamiento de tenant reforzado en
/// escritura vía ITenantContext.
///
/// DELIBERADAMENTE FUERA de este módulo: la gestión de horarios del doctor
/// (Doctor.AgregarHorario/DesactivarHorario, ya implementados en el dominio desde Fase 2) —
/// eso es el Módulo 6 (Horarios) del roadmap, un módulo aparte con su propia UI de calendario/
/// bloques y su propia validación de solapamiento. Este servicio solo gestiona el perfil del
/// doctor en sí, igual que Empleado (Módulo 3) solo gestiona el perfil, no permisos ni citas.
/// </summary>
public interface IDoctorService
{
    Task<IReadOnlyList<DoctorDto>> ListarAsync(CancellationToken cancellationToken = default);

    Task<DoctorDto?> ObtenerAsync(Guid doctorId, CancellationToken cancellationToken = default);

    Task<Result<Guid>> RegistrarAsync(RegistrarDoctorRequest request, CancellationToken cancellationToken = default);

    Task<Result> ActualizarDatosAsync(Guid doctorId, ActualizarDatosDoctorRequest request, CancellationToken cancellationToken = default);

    Task<Result> DesactivarAsync(Guid doctorId, CancellationToken cancellationToken = default);

    Task<Result> ReactivarAsync(Guid doctorId, CancellationToken cancellationToken = default);
}
