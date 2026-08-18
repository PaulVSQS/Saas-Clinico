using ClinicaSaaS.Application.Common.Interfaces;
using ClinicaSaaS.Application.Procedimientos;
using ClinicaSaaS.Domain.Billing;
using ClinicaSaaS.Domain.Billing.Enums;
using ClinicaSaaS.Domain.Clinical;
using ClinicaSaaS.Domain.Personal;
using ClinicaSaaS.Domain.Scheduling;
using ClinicaSaaS.Domain.Scheduling.Enums;

namespace ClinicaSaaS.Application.Reportes;

public sealed class ReporteService(
    IRepositorio<Factura> repositorioFacturas,
    IRepositorio<Cita> repositorioCitas,
    IRepositorio<HistorialClinico> repositorioHistoriales,
    IRepositorio<Paciente> repositorioPacientes,
    IProcedimientoCatalogoService procedimientoCatalogoService,
    ITenantContext tenantContext) : IReporteService
{
    public async Task<ReporteIngresosDto> ObtenerReporteIngresosAsync(RangoFechasRequest request, CancellationToken cancellationToken = default)
    {
        var (desde, hasta) = ConvertirRango(request);
        var clinicaId = tenantContext.ClinicaId;

        var facturas = await repositorioFacturas.ListarAsync(
            f => f.FechaEmision >= desde && f.FechaEmision <= hasta && (clinicaId == null || f.ClinicaId == clinicaId),
            cancellationToken);

        return new ReporteIngresosDto(
            TotalFacturado: facturas.Where(f => f.Estado != EstadoFactura.Anulada).Sum(f => f.Total.Monto),
            TotalCobrado: facturas.Sum(f => f.TotalPagado.Monto),
            SaldoPendiente: facturas.Where(f => f.Estado != EstadoFactura.Anulada).Sum(f => f.SaldoPendiente.Monto),
            CantidadFacturas: facturas.Count,
            CantidadPagadas: facturas.Count(f => f.Estado == EstadoFactura.Pagada),
            CantidadParciales: facturas.Count(f => f.Estado == EstadoFactura.Parcial),
            CantidadPendientes: facturas.Count(f => f.Estado == EstadoFactura.Pendiente),
            CantidadAnuladas: facturas.Count(f => f.Estado == EstadoFactura.Anulada));
    }

    public async Task<ReporteCitasDto> ObtenerReporteCitasAsync(RangoFechasRequest request, CancellationToken cancellationToken = default)
    {
        var (desde, hasta) = ConvertirRango(request);
        var clinicaId = tenantContext.ClinicaId;

        var citas = await repositorioCitas.ListarAsync(
            c => c.FechaHoraInicio >= desde && c.FechaHoraInicio <= hasta && (clinicaId == null || c.ClinicaId == clinicaId),
            cancellationToken);

        return new ReporteCitasDto(
            Total: citas.Count,
            Programadas: citas.Count(c => c.Estado == EstadoCita.Programada),
            Confirmadas: citas.Count(c => c.Estado == EstadoCita.Confirmada),
            EnProceso: citas.Count(c => c.Estado == EstadoCita.EnProceso),
            Completadas: citas.Count(c => c.Estado == EstadoCita.Completada),
            Canceladas: citas.Count(c => c.Estado == EstadoCita.Cancelada),
            NoAsistio: citas.Count(c => c.Estado == EstadoCita.NoAsistio));
    }

    public async Task<IReadOnlyList<ReporteProcedimientoDto>> ObtenerProcedimientosMasRealizadosAsync(
        RangoFechasRequest request, CancellationToken cancellationToken = default)
    {
        var (desde, hasta) = ConvertirRango(request);
        var clinicaId = tenantContext.ClinicaId;

        // Carga los agregados HistorialClinico completos (con sus Entradas y
        // ProcedimientosRealizados, ver RepositorioHistorialClinico) porque
        // ProcedimientoRealizado no es consultable como tabla plana — vive anidado dentro del
        // agregado. Con una clínica muy grande, este es el reporte que primero necesitaría una
        // consulta SQL de agregación directa en vez de traer los agregados completos; queda
        // anotado como límite conocido, no resuelto en esta base.
        var historiales = await repositorioHistoriales.ListarAsync(
            h => clinicaId == null || h.ClinicaId == clinicaId, cancellationToken);

        var catalogo = (await procedimientoCatalogoService.ListarAsync(cancellationToken)).ToDictionary(c => c.Id);

        var procedimientosEnRango = historiales
            .SelectMany(h => h.Entradas)
            .SelectMany(e => e.ProcedimientosRealizados)
            .Where(p => p.Fecha >= desde && p.Fecha <= hasta);

        return procedimientosEnRango
            .GroupBy(p => p.ProcedimientoCatalogoId)
            .Select(grupo => new ReporteProcedimientoDto(
                grupo.Key,
                catalogo.TryGetValue(grupo.Key, out var procedimiento) ? procedimiento.Codigo : "—",
                catalogo.TryGetValue(grupo.Key, out var procedimiento2) ? procedimiento2.Nombre : "Procedimiento eliminado",
                grupo.Count(),
                grupo.Sum(p => p.PrecioAplicado.Monto)))
            .OrderByDescending(dto => dto.IngresoTotal)
            .Take(10)
            .ToList();
    }

    public async Task<ReportePacientesDto> ObtenerReportePacientesNuevosAsync(RangoFechasRequest request, CancellationToken cancellationToken = default)
    {
        var (desde, hasta) = ConvertirRango(request);
        var clinicaId = tenantContext.ClinicaId;

        var pacientes = await repositorioPacientes.ListarAsync(
            p => p.FechaCreacion >= desde && p.FechaCreacion <= hasta && (clinicaId == null || p.ClinicaId == clinicaId),
            cancellationToken);

        return new ReportePacientesDto(pacientes.Count);
    }

    private static (DateTime Desde, DateTime Hasta) ConvertirRango(RangoFechasRequest request) =>
        (request.Desde.ToDateTime(TimeOnly.MinValue), request.Hasta.ToDateTime(TimeOnly.MaxValue));
}