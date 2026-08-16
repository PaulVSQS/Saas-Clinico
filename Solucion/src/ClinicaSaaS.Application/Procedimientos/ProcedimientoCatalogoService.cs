using ClinicaSaaS.Application.Clinicas;
using ClinicaSaaS.Application.Common.Interfaces;
using ClinicaSaaS.Domain.Clinical;
using ClinicaSaaS.Domain.Common.ValueObjects;
using ClinicaSaaS.SharedKernel.Results;

namespace ClinicaSaaS.Application.Procedimientos;

public sealed class ProcedimientoCatalogoService(
    IRepositorio<ProcedimientoCatalogo> repositorio,
    IUnitOfWork unitOfWork,
    IClinicaService clinicaService,
    ITenantContext tenantContext) : IProcedimientoCatalogoService
{
    public async Task<IReadOnlyList<ProcedimientoCatalogoDto>> ListarAsync(CancellationToken cancellationToken = default)
    {
        var procedimientos = await repositorio.ListarAsync(_ => true, cancellationToken);
        var nombrePorClinica = await ObtenerNombresClinicasAsync(cancellationToken);

        return procedimientos
            .OrderBy(p => p.Codigo)
            .Select(p => MapearADto(p, nombrePorClinica))
            .ToList();
    }

    public async Task<ProcedimientoCatalogoDto?> ObtenerAsync(Guid procedimientoCatalogoId, CancellationToken cancellationToken = default)
    {
        var procedimiento = await repositorio.ObtenerPorIdAsync(procedimientoCatalogoId, cancellationToken);
        if (procedimiento is null)
            return null;

        var nombrePorClinica = await ObtenerNombresClinicasAsync(cancellationToken);
        return MapearADto(procedimiento, nombrePorClinica);
    }

    public async Task<Result<Guid>> RegistrarAsync(RegistrarProcedimientoCatalogoRequest request, CancellationToken cancellationToken = default)
    {
        var clinicaId = tenantContext.ClinicaId ?? request.ClinicaId;

        var duplicados = await repositorio.ListarAsync(
            p => p.ClinicaId == clinicaId && p.Codigo == request.Codigo.Trim(), cancellationToken);
        if (duplicados.Count > 0)
            return new Error("ProcedimientoCatalogo.CodigoDuplicado", "Ya existe un procedimiento con ese código en esta clínica.");

        var precioResultado = Dinero.Crear(request.PrecioBase);
        if (precioResultado.EsFallido)
            return precioResultado.Error;

        var resultado = ProcedimientoCatalogo.Crear(
            Guid.NewGuid(), clinicaId, request.Codigo, request.Nombre, request.Descripcion, precioResultado.Value);
        if (resultado.EsFallido)
            return resultado.Error;

        var procedimiento = resultado.Value;
        await repositorio.AgregarAsync(procedimiento, cancellationToken);
        await unitOfWork.GuardarCambiosAsync(cancellationToken);

        return procedimiento.Id;
    }

    public async Task<Result> ActualizarDatosAsync(
        Guid procedimientoCatalogoId, ActualizarDatosProcedimientoCatalogoRequest request, CancellationToken cancellationToken = default)
    {
        var procedimiento = await repositorio.ObtenerPorIdAsync(procedimientoCatalogoId, cancellationToken);
        if (procedimiento is null)
            return ProcedimientoNoEncontrado;

        var duplicados = await repositorio.ListarAsync(
            p => p.Id != procedimientoCatalogoId && p.ClinicaId == procedimiento.ClinicaId && p.Codigo == request.Codigo.Trim(),
            cancellationToken);
        if (duplicados.Count > 0)
            return new Error("ProcedimientoCatalogo.CodigoDuplicado", "Ya existe otro procedimiento con ese código en esta clínica.");

        var resultado = procedimiento.ActualizarDatos(request.Codigo, request.Nombre, request.Descripcion);
        if (resultado.EsFallido)
            return resultado;

        await unitOfWork.GuardarCambiosAsync(cancellationToken);
        return Result.Exitoso();
    }

    public async Task<Result> CambiarPrecioAsync(
        Guid procedimientoCatalogoId, CambiarPrecioProcedimientoCatalogoRequest request, CancellationToken cancellationToken = default)
    {
        var procedimiento = await repositorio.ObtenerPorIdAsync(procedimientoCatalogoId, cancellationToken);
        if (procedimiento is null)
            return ProcedimientoNoEncontrado;

        var precioResultado = Dinero.Crear(request.NuevoPrecio);
        if (precioResultado.EsFallido)
            return precioResultado.Error;

        var resultado = procedimiento.CambiarPrecio(precioResultado.Value);
        if (resultado.EsFallido)
            return resultado;

        await unitOfWork.GuardarCambiosAsync(cancellationToken);
        return Result.Exitoso();
    }

    public async Task<Result> DesactivarAsync(Guid procedimientoCatalogoId, CancellationToken cancellationToken = default)
    {
        var procedimiento = await repositorio.ObtenerPorIdAsync(procedimientoCatalogoId, cancellationToken);
        if (procedimiento is null)
            return ProcedimientoNoEncontrado;

        var resultado = procedimiento.Desactivar();
        if (resultado.EsFallido)
            return resultado;

        await unitOfWork.GuardarCambiosAsync(cancellationToken);
        return Result.Exitoso();
    }

    public async Task<Result> ReactivarAsync(Guid procedimientoCatalogoId, CancellationToken cancellationToken = default)
    {
        var procedimiento = await repositorio.ObtenerPorIdAsync(procedimientoCatalogoId, cancellationToken);
        if (procedimiento is null)
            return ProcedimientoNoEncontrado;

        var resultado = procedimiento.Reactivar();
        if (resultado.EsFallido)
            return resultado;

        await unitOfWork.GuardarCambiosAsync(cancellationToken);
        return Result.Exitoso();
    }

    public async Task<Result> EliminarAsync(Guid procedimientoCatalogoId, CancellationToken cancellationToken = default)
    {
        var procedimiento = await repositorio.ObtenerPorIdAsync(procedimientoCatalogoId, cancellationToken);
        if (procedimiento is null)
            return ProcedimientoNoEncontrado;

        var resultado = procedimiento.Eliminar(Guid.Empty, DateTime.UtcNow);
        if (resultado.EsFallido)
            return resultado;

        await unitOfWork.GuardarCambiosAsync(cancellationToken);
        return Result.Exitoso();
    }

    private static Error ProcedimientoNoEncontrado =>
        new("ProcedimientoCatalogo.NoEncontrado", "El procedimiento solicitado no existe.");

    private async Task<Dictionary<Guid, string>> ObtenerNombresClinicasAsync(CancellationToken cancellationToken)
    {
        var clinicas = await clinicaService.ListarAsync(cancellationToken);
        return clinicas.ToDictionary(c => c.Id, c => c.NombreComercial);
    }

    private static ProcedimientoCatalogoDto MapearADto(ProcedimientoCatalogo p, IReadOnlyDictionary<Guid, string> nombrePorClinica) => new(
        p.Id,
        p.ClinicaId,
        nombrePorClinica.TryGetValue(p.ClinicaId, out var nombreClinica) ? nombreClinica : p.ClinicaId.ToString(),
        p.Codigo,
        p.Nombre,
        p.Descripcion,
        p.PrecioBase.Monto,
        p.Activo);
}