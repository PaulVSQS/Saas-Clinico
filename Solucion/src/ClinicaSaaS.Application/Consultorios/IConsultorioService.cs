using ClinicaSaaS.SharedKernel.Results;

namespace ClinicaSaaS.Application.Consultorios;

/// <summary>
/// Vista de un Consultorio hacia afuera de Application — mismo criterio que DoctorDto/EmpleadoDto
/// (Fase 6, Módulos 3 y 4): el nombre de la clínica se resuelve vía join en memoria con
/// IClinicaService. A diferencia de Doctor/Empleado, Consultorio no está ligado a un Usuario —
/// es un espacio físico de la clínica, no un perfil de persona, así que no hay campos de usuario.
/// </summary>
public sealed record ConsultorioDto(
    Guid Id,
    Guid ClinicaId,
    string NombreClinica,
    string Nombre,
    string? Piso,
    bool Activo);

public sealed record RegistrarConsultorioRequest(Guid ClinicaId, string Nombre, string? Piso);

public sealed record ActualizarDatosConsultorioRequest(string Nombre, string? Piso);

/// <summary>
/// Módulo 5 de Fase 6 — Consultorios: el espacio físico de la clínica (nombre, piso,
/// activo/inactivo) donde luego se agendan horarios de doctores (Módulo 6) y citas (Módulo 8).
/// Mismo criterio de protección que Empleados/Doctores (Módulos 3 y 4): Policy "AdminClinica",
/// con aislamiento de tenant reforzado en escritura vía ITenantContext.
///
/// DELIBERADAMENTE FUERA de este módulo: la gestión de Equipamiento
/// (Scheduling.Equipamiento, ya modelado en el dominio desde Fase 2, ligado opcionalmente a un
/// Consultorio) — el roadmap oficial (fases-del-proyecto.md) no lo lista como uno de los 14
/// módulos de Fase 6, así que queda pendiente de una decisión de alcance futura, igual que
/// Horarios quedó explícitamente fuera de Doctores (Módulo 4).
/// </summary>
public interface IConsultorioService
{
    Task<IReadOnlyList<ConsultorioDto>> ListarAsync(CancellationToken cancellationToken = default);

    Task<ConsultorioDto?> ObtenerAsync(Guid consultorioId, CancellationToken cancellationToken = default);

    Task<Result<Guid>> RegistrarAsync(RegistrarConsultorioRequest request, CancellationToken cancellationToken = default);

    Task<Result> ActualizarDatosAsync(Guid consultorioId, ActualizarDatosConsultorioRequest request, CancellationToken cancellationToken = default);

    Task<Result> DesactivarAsync(Guid consultorioId, CancellationToken cancellationToken = default);

    Task<Result> ReactivarAsync(Guid consultorioId, CancellationToken cancellationToken = default);
}
