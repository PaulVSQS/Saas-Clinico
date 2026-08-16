using ClinicaSaaS.SharedKernel.Results;

namespace ClinicaSaaS.Application.Procedimientos;

public sealed record ProcedimientoCatalogoDto(
    Guid Id,
    Guid ClinicaId,
    string NombreClinica,
    string Codigo,
    string Nombre,
    string? Descripcion,
    decimal PrecioBase,
    bool Activo);

public sealed record RegistrarProcedimientoCatalogoRequest(Guid ClinicaId, string Codigo, string Nombre, string? Descripcion, decimal PrecioBase);

public sealed record ActualizarDatosProcedimientoCatalogoRequest(string Codigo, string Nombre, string? Descripcion);

public sealed record CambiarPrecioProcedimientoCatalogoRequest(decimal NuevoPrecio);

/// <summary>
/// Módulo 11 de Fase 6 — catálogo de procedimientos de la clínica (qué se ofrece y a qué precio
/// base). Es un agregado independiente, sin relación de contención con HistorialClinico —
/// ProcedimientoRealizado (ver IProcedimientoRealizadoService) solo lo referencia por Id, nunca
/// por navegación, para que repreciar un procedimiento hoy nunca reescriba un registro histórico
/// ya tomado.
///
/// Mismo criterio de protección que los módulos anteriores: Policy "AdminClinica", con
/// aislamiento de tenant en escritura vía ITenantContext, y validación de código único por
/// clínica (mismo criterio que la restricción UQ_Procedimiento_Codigo_Clinica de la base de
/// datos, verificada aquí también para dar un error claro antes de llegar a SQL Server).
/// </summary>
public interface IProcedimientoCatalogoService
{
    Task<IReadOnlyList<ProcedimientoCatalogoDto>> ListarAsync(CancellationToken cancellationToken = default);

    Task<ProcedimientoCatalogoDto?> ObtenerAsync(Guid procedimientoCatalogoId, CancellationToken cancellationToken = default);

    Task<Result<Guid>> RegistrarAsync(RegistrarProcedimientoCatalogoRequest request, CancellationToken cancellationToken = default);

    Task<Result> ActualizarDatosAsync(Guid procedimientoCatalogoId, ActualizarDatosProcedimientoCatalogoRequest request, CancellationToken cancellationToken = default);

    Task<Result> CambiarPrecioAsync(Guid procedimientoCatalogoId, CambiarPrecioProcedimientoCatalogoRequest request, CancellationToken cancellationToken = default);

    Task<Result> DesactivarAsync(Guid procedimientoCatalogoId, CancellationToken cancellationToken = default);

    Task<Result> ReactivarAsync(Guid procedimientoCatalogoId, CancellationToken cancellationToken = default);

    Task<Result> EliminarAsync(Guid procedimientoCatalogoId, CancellationToken cancellationToken = default);
}