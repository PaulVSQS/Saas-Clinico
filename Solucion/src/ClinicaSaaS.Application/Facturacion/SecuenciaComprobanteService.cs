using ClinicaSaaS.Application.Clinicas;
using ClinicaSaaS.Application.Common.Interfaces;
using ClinicaSaaS.Domain.Billing;
using ClinicaSaaS.Domain.Billing.ValueObjects;
using ClinicaSaaS.SharedKernel.Results;

namespace ClinicaSaaS.Application.Facturacion;

public sealed class SecuenciaComprobanteService(
    IRepositorio<SecuenciaComprobante> repositorio,
    IUnitOfWork unitOfWork,
    IClinicaService clinicaService,
    ITenantContext tenantContext) : ISecuenciaComprobanteService
{
    public async Task<IReadOnlyList<SecuenciaComprobanteDto>> ListarAsync(CancellationToken cancellationToken = default)
    {
        var secuencias = await repositorio.ListarAsync(_ => true, cancellationToken);
        var nombrePorClinica = await ObtenerNombresClinicasAsync(cancellationToken);

        return secuencias
            .OrderBy(s => s.TipoComprobante.Valor)
            .Select(s => MapearADto(s, nombrePorClinica))
            .ToList();
    }

    public async Task<Result<Guid>> AutorizarAsync(AutorizarSecuenciaComprobanteRequest request, CancellationToken cancellationToken = default)
    {
        var clinicaId = tenantContext.ClinicaId ?? request.ClinicaId;

        var tipoResultado = CodigoComprobanteFiscal.Crear(request.TipoComprobante);
        if (tipoResultado.EsFallido)
            return tipoResultado.Error;

        var duplicados = await repositorio.ListarAsync(
            s => s.ClinicaId == clinicaId && s.TipoComprobante == tipoResultado.Value, cancellationToken);
        if (duplicados.Count > 0)
            return new Error("SecuenciaComprobante.Duplicada", "Ya existe una secuencia autorizada para este tipo de comprobante en esta clínica.");

        var resultado = SecuenciaComprobante.Autorizar(
            Guid.NewGuid(), clinicaId, tipoResultado.Value, request.SecuenciaDesde, request.SecuenciaHasta, request.FechaVencimiento);
        if (resultado.EsFallido)
            return resultado.Error;

        var secuencia = resultado.Value;
        await repositorio.AgregarAsync(secuencia, cancellationToken);
        await unitOfWork.GuardarCambiosAsync(cancellationToken);

        return secuencia.Id;
    }

    public async Task<Result> DesactivarAsync(Guid secuenciaComprobanteId, CancellationToken cancellationToken = default)
    {
        var secuencia = await repositorio.ObtenerPorIdAsync(secuenciaComprobanteId, cancellationToken);
        if (secuencia is null)
            return SecuenciaNoEncontrada;

        var resultado = secuencia.Desactivar();
        if (resultado.EsFallido)
            return resultado;

        await unitOfWork.GuardarCambiosAsync(cancellationToken);
        return Result.Exitoso();
    }

    public async Task<Result<(string Numero, CodigoComprobanteFiscal TipoComprobante)>> TomarSiguienteNumeroAsync(
        Guid secuenciaComprobanteId, CancellationToken cancellationToken = default)
    {
        var secuencia = await repositorio.ObtenerPorIdAsync(secuenciaComprobanteId, cancellationToken);
        if (secuencia is null)
            return SecuenciaNoEncontrada;

        var numeroResultado = secuencia.TomarSiguienteNumero(DateOnly.FromDateTime(DateTime.UtcNow));
        if (numeroResultado.EsFallido)
            return numeroResultado.Error;

        // DELIBERADAMENTE NO se guarda aquí: SecuenciaComprobante y Factura son agregados
        // distintos pero comparten el mismo DbContext dentro de este scope/circuito (Blazor
        // Server) — FacturaService.AsignarNumeroComprobanteAsync es quien llama a
        // GuardarCambiosAsync() DESPUÉS de asignarle el número a la Factura, así ambas
        // mutaciones (consumir el número + asignarlo) se confirman juntas en UNA sola
        // transacción. Guardar aquí mismo dejaría el número "quemado" en la secuencia si la
        // asignación a la Factura fallara después.
        return (numeroResultado.Value, secuencia.TipoComprobante);
    }

    private static Error SecuenciaNoEncontrada =>
        new("SecuenciaComprobante.NoEncontrada", "La secuencia de comprobantes solicitada no existe.");

    private async Task<Dictionary<Guid, string>> ObtenerNombresClinicasAsync(CancellationToken cancellationToken)
    {
        var clinicas = await clinicaService.ListarAsync(cancellationToken);
        return clinicas.ToDictionary(c => c.Id, c => c.NombreComercial);
    }

    private static SecuenciaComprobanteDto MapearADto(SecuenciaComprobante s, IReadOnlyDictionary<Guid, string> nombrePorClinica) => new(
        s.Id,
        s.ClinicaId,
        nombrePorClinica.TryGetValue(s.ClinicaId, out var nombreClinica) ? nombreClinica : s.ClinicaId.ToString(),
        s.TipoComprobante.Valor,
        s.EsElectronico,
        s.SecuenciaActual,
        s.SecuenciaDesde,
        s.SecuenciaHasta,
        s.FechaVencimiento,
        s.Activo);
}