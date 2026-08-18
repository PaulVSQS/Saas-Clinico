using ClinicaSaaS.Application.Common.Interfaces;
using ClinicaSaaS.Application.Usuarios;
using ClinicaSaaS.Domain.Billing;
using ClinicaSaaS.Domain.Billing.Entities;
using ClinicaSaaS.Domain.Common.ValueObjects;
using ClinicaSaaS.SharedKernel.Results;

namespace ClinicaSaaS.Application.Facturacion;

public sealed class PagoService(
    IRepositorio<Factura> repositorio,
    IUnitOfWork unitOfWork,
    IUsuarioService usuarioService,
    ICurrentUserContext currentUserContext) : IPagoService
{
    public async Task<IReadOnlyList<PagoDto>> ListarPorFacturaAsync(Guid facturaId, CancellationToken cancellationToken = default)
    {
        var factura = await repositorio.ObtenerPorIdAsync(facturaId, cancellationToken);
        if (factura is null)
            return [];

        var nombrePorUsuario = await ObtenerNombresUsuariosAsync(cancellationToken);

        return factura.Pagos
            .OrderByDescending(p => p.FechaHora)
            .Select(p => MapearADto(p, facturaId, nombrePorUsuario))
            .ToList();
    }

    public async Task<IReadOnlyList<PagoDto>> ListarPorPacienteAsync(Guid pacienteId, CancellationToken cancellationToken = default)
    {
        var facturas = await repositorio.ListarAsync(f => f.PacienteId == pacienteId, cancellationToken);
        var nombrePorUsuario = await ObtenerNombresUsuariosAsync(cancellationToken);

        return facturas
            .SelectMany(f => f.Pagos.Select(p => MapearADto(p, f.Id, nombrePorUsuario)))
            .OrderByDescending(dto => dto.FechaHora)
            .ToList();
    }

    public async Task<Result<Guid>> RegistrarAsync(RegistrarPagoRequest request, CancellationToken cancellationToken = default)
    {
        var factura = await repositorio.ObtenerPorIdAsync(request.FacturaId, cancellationToken);
        if (factura is null)
            return new Error("Pago.FacturaNoEncontrada", "La factura solicitada no existe.");

        var montoResultado = Dinero.Crear(request.Monto);
        if (montoResultado.EsFallido)
            return montoResultado.Error;

        var resultado = factura.RegistrarPago(
            Guid.NewGuid(), montoResultado.Value, DateTime.UtcNow, request.MetodoPago, request.Referencia,
            currentUserContext.UsuarioId ?? Guid.Empty);
        if (resultado.EsFallido)
            return resultado.Error;

        await unitOfWork.GuardarCambiosAsync(cancellationToken);
        return resultado.Value.Id;
    }

    public async Task<Result> AnularAsync(Guid facturaId, Guid pagoId, string motivo, CancellationToken cancellationToken = default)
    {
        var factura = await repositorio.ObtenerPorIdAsync(facturaId, cancellationToken);
        if (factura is null)
            return new Error("Pago.FacturaNoEncontrada", "La factura solicitada no existe.");

        var resultado = factura.AnularPago(pagoId, motivo, DateTime.UtcNow);
        if (resultado.EsFallido)
            return resultado;

        await unitOfWork.GuardarCambiosAsync(cancellationToken);
        return Result.Exitoso();
    }

    private async Task<Dictionary<Guid, string>> ObtenerNombresUsuariosAsync(CancellationToken cancellationToken)
    {
        var usuarios = await usuarioService.ListarAsync(cancellationToken);
        return usuarios.ToDictionary(u => u.Id, u => u.NombreCompleto);
    }

    private static PagoDto MapearADto(Pago p, Guid facturaId, IReadOnlyDictionary<Guid, string> nombrePorUsuario) => new(
        p.Id,
        facturaId,
        p.Monto.Monto,
        p.FechaHora,
        p.MetodoPago,
        p.Referencia,
        p.RegistradoPorUsuarioId,
        nombrePorUsuario.TryGetValue(p.RegistradoPorUsuarioId, out var nombre) ? nombre : p.RegistradoPorUsuarioId.ToString(),
        p.EstaAnulado,
        p.MotivoAnulacion);
}