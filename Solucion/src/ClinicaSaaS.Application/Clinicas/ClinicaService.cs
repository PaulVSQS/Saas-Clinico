using ClinicaSaaS.Application.Common.Interfaces;
using ClinicaSaaS.Domain.Security;
using ClinicaSaaS.Domain.Security.Enums;
using ClinicaSaaS.Domain.Security.ValueObjects;
using ClinicaSaaS.SharedKernel.Results;

namespace ClinicaSaaS.Application.Clinicas;

public sealed class ClinicaService(IRepositorio<Clinica> repositorio, IUnitOfWork unitOfWork) : IClinicaService
{
    public async Task<IReadOnlyList<ClinicaDto>> ListarAsync(CancellationToken cancellationToken = default)
    {
        // ListarAsync(predicado) es la única forma que Application tiene de traer "todas" —
        // ver el comentario de IRepositorio sobre por qué no hay Consultar().ToListAsync() aquí
        // (Application no referencia EF Core). Ordenar en memoria es aceptable: el número de
        // clínicas de un SaaS es pequeño, no un dataset paginable.
        var clinicas = await repositorio.ListarAsync(_ => true, cancellationToken);

        return clinicas
            .OrderBy(c => c.NombreComercial)
            .Select(MapearADto)
            .ToList();
    }

    public async Task<ClinicaDto?> ObtenerAsync(Guid clinicaId, CancellationToken cancellationToken = default)
    {
        var clinica = await repositorio.ObtenerPorIdAsync(clinicaId, cancellationToken);
        return clinica is null ? null : MapearADto(clinica);
    }

    public async Task<Result<Guid>> RegistrarAsync(RegistrarClinicaRequest request, CancellationToken cancellationToken = default)
    {
        var rncResultado = Rnc.Crear(request.Rnc);
        if (rncResultado.EsFallido)
            return rncResultado.Error;

        // El índice único de Rnc (Fase 3) protege esto a nivel de base de datos también, pero
        // se valida aquí primero para devolver un mensaje de negocio claro en vez de dejar que
        // reviente como una excepción de violación de índice único sin manejar.
        var yaExiste = await repositorio.PrimeroOPredeterminadoAsync(c => c.Rnc == rncResultado.Value, cancellationToken);
        if (yaExiste is not null)
            return new Error("Clinica.RncDuplicado", "Ya existe una clínica registrada con ese RNC.");

        var clinicaResultado = Clinica.Registrar(Guid.NewGuid(), request.RazonSocial, request.NombreComercial, rncResultado.Value);
        if (clinicaResultado.EsFallido)
            return clinicaResultado.Error;

        var clinica = clinicaResultado.Value;
        await repositorio.AgregarAsync(clinica, cancellationToken);
        await unitOfWork.GuardarCambiosAsync(cancellationToken);

        return clinica.Id;
    }

    public async Task<Result> ActualizarContactoAsync(
        Guid clinicaId, ActualizarContactoClinicaRequest request, CancellationToken cancellationToken = default)
    {
        var clinica = await repositorio.ObtenerPorIdAsync(clinicaId, cancellationToken);
        if (clinica is null)
            return ClinicaNoEncontrada;

        Email? email = null;
        if (!string.IsNullOrWhiteSpace(request.Email))
        {
            var emailResultado = Email.Crear(request.Email);
            if (emailResultado.EsFallido)
                return emailResultado.Error;

            email = emailResultado.Value;
        }

        var resultado = clinica.ActualizarDatosDeContacto(request.Direccion, request.Telefono, email);
        if (resultado.EsFallido)
            return resultado;

        await unitOfWork.GuardarCambiosAsync(cancellationToken);
        return Result.Exitoso();
    }

    public async Task<Result> CambiarPlanAsync(Guid clinicaId, PlanSuscripcion nuevoPlan, CancellationToken cancellationToken = default)
    {
        var clinica = await repositorio.ObtenerPorIdAsync(clinicaId, cancellationToken);
        if (clinica is null)
            return ClinicaNoEncontrada;

        var resultado = clinica.CambiarPlan(nuevoPlan);
        if (resultado.EsFallido)
            return resultado;

        await unitOfWork.GuardarCambiosAsync(cancellationToken);
        return Result.Exitoso();
    }

    public async Task<Result> DesactivarAsync(Guid clinicaId, CancellationToken cancellationToken = default)
    {
        var clinica = await repositorio.ObtenerPorIdAsync(clinicaId, cancellationToken);
        if (clinica is null)
            return ClinicaNoEncontrada;

        var resultado = clinica.Desactivar();
        if (resultado.EsFallido)
            return resultado;

        await unitOfWork.GuardarCambiosAsync(cancellationToken);
        return Result.Exitoso();
    }

    public async Task<Result> ReactivarAsync(Guid clinicaId, CancellationToken cancellationToken = default)
    {
        var clinica = await repositorio.ObtenerPorIdAsync(clinicaId, cancellationToken);
        if (clinica is null)
            return ClinicaNoEncontrada;

        var resultado = clinica.Reactivar();
        if (resultado.EsFallido)
            return resultado;

        await unitOfWork.GuardarCambiosAsync(cancellationToken);
        return Result.Exitoso();
    }

    private static Error ClinicaNoEncontrada =>
        new("Clinica.NoEncontrada", "La clínica solicitada no existe.");

    private static ClinicaDto MapearADto(Clinica c) => new(
        c.Id, c.RazonSocial, c.NombreComercial, c.Rnc.Valor,
        c.Direccion, c.Telefono, c.Email?.Valor, c.PlanSuscripcion, c.Activo);
}
