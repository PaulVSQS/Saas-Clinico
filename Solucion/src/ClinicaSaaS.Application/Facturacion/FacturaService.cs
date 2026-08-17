using ClinicaSaaS.Application.Clinicas;
using ClinicaSaaS.Application.Common.Interfaces;
using ClinicaSaaS.Application.Pacientes;
using ClinicaSaaS.Application.Procedimientos;
using ClinicaSaaS.Domain.Billing;
using ClinicaSaaS.Domain.Billing.Entities;
using ClinicaSaaS.Domain.Billing.Enums;
using ClinicaSaaS.Domain.Common.ValueObjects;
using ClinicaSaaS.SharedKernel.Results;

namespace ClinicaSaaS.Application.Facturacion;

public sealed class FacturaService(
    IRepositorio<Factura> repositorio,
    IUnitOfWork unitOfWork,
    IClinicaService clinicaService,
    IPacienteService pacienteService,
    IProcedimientoRealizadoService procedimientoRealizadoService,
    ISecuenciaComprobanteService secuenciaComprobanteService,
    ITenantContext tenantContext,
    ICurrentUserContext currentUserContext) : IFacturaService
{
    public async Task<IReadOnlyList<FacturaDto>> ListarPorPacienteAsync(Guid pacienteId, CancellationToken cancellationToken = default)
    {
        var facturas = await repositorio.ListarAsync(f => f.PacienteId == pacienteId, cancellationToken);
        var (nombrePorClinica, nombrePorPaciente) = await ObtenerJoinsAsync(cancellationToken);

        return facturas
            .OrderByDescending(f => f.FechaEmision)
            .Select(f => MapearADto(f, nombrePorClinica, nombrePorPaciente))
            .ToList();
    }

    public async Task<FacturaDto?> ObtenerAsync(Guid facturaId, CancellationToken cancellationToken = default)
    {
        var factura = await repositorio.ObtenerPorIdAsync(facturaId, cancellationToken);
        if (factura is null)
            return null;

        var (nombrePorClinica, nombrePorPaciente) = await ObtenerJoinsAsync(cancellationToken);
        return MapearADto(factura, nombrePorClinica, nombrePorPaciente);
    }

    public async Task<Result<Guid>> EmitirAsync(EmitirFacturaRequest request, CancellationToken cancellationToken = default)
    {
        var paciente = await pacienteService.ObtenerAsync(request.PacienteId, cancellationToken);
        if (paciente is null)
            return new Error("Factura.PacienteNoEncontrado", "El paciente solicitado no existe.");

        // AISLAMIENTO DE TENANT EN ESCRITURA — mismo criterio que los módulos anteriores: la
        // factura siempre hereda la clínica del paciente, nunca una elegida aparte por quien
        // emite (a diferencia de Doctor/Empleado/Consultorio/Paciente, aquí no hace falta
        // selector de clínica porque el paciente ya la determina).
        var resultado = Factura.Emitir(
            Guid.NewGuid(), paciente.ClinicaId, request.PacienteId, request.TipoFactura,
            currentUserContext.UsuarioId ?? Guid.Empty, DateTime.UtcNow);
        if (resultado.EsFallido)
            return resultado.Error;

        var factura = resultado.Value;
        await repositorio.AgregarAsync(factura, cancellationToken);
        await unitOfWork.GuardarCambiosAsync(cancellationToken);

        return factura.Id;
    }

    public async Task<Result<Guid>> AgregarDetalleAsync(Guid facturaId, AgregarDetalleFacturaRequest request, CancellationToken cancellationToken = default)
    {
        var factura = await repositorio.ObtenerPorIdAsync(facturaId, cancellationToken);
        if (factura is null)
            return FacturaNoEncontrada;

        string descripcion;
        decimal precioUnitario;

        if (request.ProcedimientoRealizadoId is Guid procedimientoRealizadoId)
        {
            // Snapshot desde el procedimiento realizado — nunca texto libre cuando hay
            // trazabilidad clínica, mismo criterio que el propio comentario del dominio sobre
            // FacturaDetalle.Descripcion.
            var procedimientos = await procedimientoRealizadoService.ListarPorPacienteAsync(factura.PacienteId, cancellationToken);
            var procedimiento = procedimientos.FirstOrDefault(p => p.Id == procedimientoRealizadoId);
            if (procedimiento is null)
                return new Error("FacturaDetalle.ProcedimientoInvalido", "El procedimiento realizado no pertenece a este paciente.");

            // Evita facturar dos veces el mismo procedimiento: revisa los detalles de TODAS las
            // facturas no anuladas de este paciente.
            var facturasDelPaciente = await repositorio.ListarAsync(f => f.PacienteId == factura.PacienteId && !f.EstaAnulada, cancellationToken);
            var yaFacturado = facturasDelPaciente.Any(f => f.Detalles.Any(d => d.ProcedimientoRealizadoId == procedimientoRealizadoId));
            if (yaFacturado)
                return new Error("FacturaDetalle.ProcedimientoYaFacturado", "Ese procedimiento ya fue incluido en otra factura de este paciente.");

            descripcion = $"{procedimiento.CodigoProcedimiento} — {procedimiento.NombreProcedimiento}";
            precioUnitario = procedimiento.PrecioAplicado;
        }
        else
        {
            if (string.IsNullOrWhiteSpace(request.DescripcionManual))
                return new Error("FacturaDetalle.DescripcionRequerida", "La descripción es requerida para un detalle sin procedimiento asociado.");

            if (request.PrecioUnitarioManual is null)
                return new Error("FacturaDetalle.PrecioRequerido", "El precio unitario es requerido para un detalle sin procedimiento asociado.");

            descripcion = request.DescripcionManual;
            precioUnitario = request.PrecioUnitarioManual.Value;
        }

        var precioResultado = Dinero.Crear(precioUnitario);
        if (precioResultado.EsFallido)
            return precioResultado.Error;

        var resultado = factura.AgregarDetalle(Guid.NewGuid(), request.ProcedimientoRealizadoId, descripcion, request.Cantidad, precioResultado.Value);
        if (resultado.EsFallido)
            return resultado.Error;

        await unitOfWork.GuardarCambiosAsync(cancellationToken);
        return resultado.Value.Id;
    }

    public async Task<Result> AplicarDescuentoAsync(Guid facturaId, AplicarDescuentoFacturaRequest request, CancellationToken cancellationToken = default)
    {
        var factura = await repositorio.ObtenerPorIdAsync(facturaId, cancellationToken);
        if (factura is null)
            return FacturaNoEncontrada;

        var montoResultado = Dinero.Crear(request.Monto);
        if (montoResultado.EsFallido)
            return montoResultado.Error;

        var resultado = factura.AplicarDescuento(montoResultado.Value);
        if (resultado.EsFallido)
            return resultado;

        await unitOfWork.GuardarCambiosAsync(cancellationToken);
        return Result.Exitoso();
    }

    public async Task<Result> AplicarImpuestosAsync(Guid facturaId, AplicarImpuestosFacturaRequest request, CancellationToken cancellationToken = default)
    {
        var factura = await repositorio.ObtenerPorIdAsync(facturaId, cancellationToken);
        if (factura is null)
            return FacturaNoEncontrada;

        var montoResultado = Dinero.Crear(request.Monto);
        if (montoResultado.EsFallido)
            return montoResultado.Error;

        var resultado = factura.AplicarImpuestos(montoResultado.Value);
        if (resultado.EsFallido)
            return resultado;

        await unitOfWork.GuardarCambiosAsync(cancellationToken);
        return Result.Exitoso();
    }

    public async Task<Result> AsignarNumeroComprobanteAsync(Guid facturaId, Guid secuenciaComprobanteId, CancellationToken cancellationToken = default)
    {
        var factura = await repositorio.ObtenerPorIdAsync(facturaId, cancellationToken);
        if (factura is null)
            return FacturaNoEncontrada;

        var numeroResultado = await secuenciaComprobanteService.TomarSiguienteNumeroAsync(secuenciaComprobanteId, cancellationToken);
        if (numeroResultado.EsFallido)
            return numeroResultado.Error;

        var (numero, tipoComprobante) = numeroResultado.Value;

        var resultado = factura.AsignarNumeroComprobante(tipoComprobante, numero);
        if (resultado.EsFallido)
            return resultado;

        await unitOfWork.GuardarCambiosAsync(cancellationToken);
        return Result.Exitoso();
    }

    public async Task<Result> ActualizarEstadoDGIIAsync(Guid facturaId, EstadoDGII nuevoEstado, CancellationToken cancellationToken = default)
    {
        var factura = await repositorio.ObtenerPorIdAsync(facturaId, cancellationToken);
        if (factura is null)
            return FacturaNoEncontrada;

        var resultado = factura.ActualizarEstadoDGII(nuevoEstado);
        if (resultado.EsFallido)
            return resultado;

        await unitOfWork.GuardarCambiosAsync(cancellationToken);
        return Result.Exitoso();
    }

    public async Task<Result> AnularAsync(Guid facturaId, string motivo, CancellationToken cancellationToken = default)
    {
        var factura = await repositorio.ObtenerPorIdAsync(facturaId, cancellationToken);
        if (factura is null)
            return FacturaNoEncontrada;

        var resultado = factura.Anular(motivo);
        if (resultado.EsFallido)
            return resultado;

        await unitOfWork.GuardarCambiosAsync(cancellationToken);
        return Result.Exitoso();
    }

    public async Task<Result> EliminarAsync(Guid facturaId, CancellationToken cancellationToken = default)
    {
        var factura = await repositorio.ObtenerPorIdAsync(facturaId, cancellationToken);
        if (factura is null)
            return FacturaNoEncontrada;

        var resultado = factura.Eliminar(currentUserContext.UsuarioId ?? Guid.Empty, DateTime.UtcNow);
        if (resultado.EsFallido)
            return resultado;

        await unitOfWork.GuardarCambiosAsync(cancellationToken);
        return Result.Exitoso();
    }

    private static Error FacturaNoEncontrada =>
        new("Factura.NoEncontrada", "La factura solicitada no existe.");

    private async Task<(Dictionary<Guid, string> NombrePorClinica, Dictionary<Guid, string> NombrePorPaciente)> ObtenerJoinsAsync(CancellationToken cancellationToken)
    {
        var clinicas = await clinicaService.ListarAsync(cancellationToken);
        var pacientes = await pacienteService.ListarAsync(cancellationToken: cancellationToken);

        return (
            clinicas.ToDictionary(c => c.Id, c => c.NombreComercial),
            pacientes.ToDictionary(p => p.Id, p => p.NombreCompleto));
    }

    private static FacturaDto MapearADto(
        Factura f, IReadOnlyDictionary<Guid, string> nombrePorClinica, IReadOnlyDictionary<Guid, string> nombrePorPaciente) => new(
            f.Id,
            f.ClinicaId,
            nombrePorClinica.TryGetValue(f.ClinicaId, out var nombreClinica) ? nombreClinica : f.ClinicaId.ToString(),
            f.PacienteId,
            nombrePorPaciente.TryGetValue(f.PacienteId, out var nombrePaciente) ? nombrePaciente : f.PacienteId.ToString(),
            f.TipoFactura,
            f.NumeroComprobante,
            f.EstadoDGII,
            f.FechaEmision,
            f.Subtotal.Monto,
            f.Descuento.Monto,
            f.Impuestos.Monto,
            f.Total.Monto,
            f.TotalPagado.Monto,
            f.SaldoPendiente.Monto,
            f.Estado,
            f.EstaAnulada,
            f.MotivoAnulacion,
            f.Detalles.Select(MapearDetalleADto).ToList());

    private static FacturaDetalleDto MapearDetalleADto(FacturaDetalle d) => new(
        d.Id, d.ProcedimientoRealizadoId, d.Descripcion, d.Cantidad, d.PrecioUnitario.Monto, d.Subtotal.Monto);
}