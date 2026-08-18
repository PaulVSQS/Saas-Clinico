namespace ClinicaSaaS.Application.Reportes;

public sealed record RangoFechasRequest(DateOnly Desde, DateOnly Hasta);

public sealed record ReporteIngresosDto(
    decimal TotalFacturado,
    decimal TotalCobrado,
    decimal SaldoPendiente,
    int CantidadFacturas,
    int CantidadPagadas,
    int CantidadParciales,
    int CantidadPendientes,
    int CantidadAnuladas);

public sealed record ReporteCitasDto(
    int Total,
    int Programadas,
    int Confirmadas,
    int EnProceso,
    int Completadas,
    int Canceladas,
    int NoAsistio);

public sealed record ReporteProcedimientoDto(
    Guid ProcedimientoCatalogoId,
    string Codigo,
    string Nombre,
    int CantidadRealizada,
    decimal IngresoTotal);

public sealed record ReportePacientesDto(int PacientesNuevos);

/// <summary>
/// Módulo 14 de Fase 6 — Reportes: vistas agregadas de solo lectura sobre datos que ya existen
/// en Facturación (Módulo 12), Citas (Módulo 8), Procedimientos (Módulo 11) y Pacientes
/// (Módulo 7). A diferencia de los 13 módulos anteriores, no hay ningún agregado ni tabla propia
/// de "reportes" — este servicio nunca escribe nada, solo consulta y agrega en memoria.
///
/// Con una clínica activa en sesión, cada reporte se limita a esa clínica; un SuperAdmin SaaS
/// (sin clínica activa) ve el agregado de todas las clínicas — mismo criterio que el resto de
/// los módulos de Fase 6 cuando no hay tenant activo.
/// </summary>
public interface IReporteService
{
    Task<ReporteIngresosDto> ObtenerReporteIngresosAsync(RangoFechasRequest request, CancellationToken cancellationToken = default);

    Task<ReporteCitasDto> ObtenerReporteCitasAsync(RangoFechasRequest request, CancellationToken cancellationToken = default);

    /// <summary>Ordenado por ingreso total descendente, top 10.</summary>
    Task<IReadOnlyList<ReporteProcedimientoDto>> ObtenerProcedimientosMasRealizadosAsync(RangoFechasRequest request, CancellationToken cancellationToken = default);

    Task<ReportePacientesDto> ObtenerReportePacientesNuevosAsync(RangoFechasRequest request, CancellationToken cancellationToken = default);
}