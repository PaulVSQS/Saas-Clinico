using ClinicaSaaS.Domain.Security.Enums;
using ClinicaSaaS.SharedKernel.Results;

namespace ClinicaSaaS.Application.Clinicas;

/// <summary>
/// Vista de una Clinica hacia afuera de Application — deliberadamente NO se expone el
/// Aggregate Root Clinica directamente a Web: este DTO es lo único que Web (Blazor) conoce,
/// así los Value Objects (Rnc, Email) y el resto de invariantes del dominio quedan encapsulados
/// dentro de Application/Domain. Se reutiliza el mismo DTO para lista y detalle (no hay
/// suficiente diferencia de campos entre ambos casos como para justificar dos clases).
/// </summary>
public sealed record ClinicaDto(
    Guid Id,
    string RazonSocial,
    string NombreComercial,
    string Rnc,
    string? Direccion,
    string? Telefono,
    string? Email,
    PlanSuscripcion PlanSuscripcion,
    bool Activo);

public sealed record RegistrarClinicaRequest(string RazonSocial, string NombreComercial, string Rnc);

public sealed record ActualizarContactoClinicaRequest(string? Direccion, string? Telefono, string? Email);

/// <summary>
/// Módulo 1 de Fase 6 (Módulos del Negocio) — el primero según el orden estricto del roadmap,
/// porque ClinicaId es el tenant raíz del que depende el aislamiento multi-tenant de
/// prácticamente todos los demás agregados. Gestionar qué clínicas existen en la plataforma es
/// una operación de nivel SaaS (no de una clínica individual) — por eso, en Web, todas las
/// páginas de este módulo se protegen con la Policy SuperAdminSaaS (Fase 5), no con un rol de
/// clínica.
/// </summary>
public interface IClinicaService
{
    Task<IReadOnlyList<ClinicaDto>> ListarAsync(CancellationToken cancellationToken = default);

    Task<ClinicaDto?> ObtenerAsync(Guid clinicaId, CancellationToken cancellationToken = default);

    Task<Result<Guid>> RegistrarAsync(RegistrarClinicaRequest request, CancellationToken cancellationToken = default);

    Task<Result> ActualizarContactoAsync(Guid clinicaId, ActualizarContactoClinicaRequest request, CancellationToken cancellationToken = default);

    Task<Result> CambiarPlanAsync(Guid clinicaId, PlanSuscripcion nuevoPlan, CancellationToken cancellationToken = default);

    Task<Result> DesactivarAsync(Guid clinicaId, CancellationToken cancellationToken = default);

    Task<Result> ReactivarAsync(Guid clinicaId, CancellationToken cancellationToken = default);
}
