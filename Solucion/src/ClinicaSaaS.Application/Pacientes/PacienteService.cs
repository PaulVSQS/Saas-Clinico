using System.Linq.Expressions;
using ClinicaSaaS.Application.Clinicas;
using ClinicaSaaS.Application.Common.Interfaces;
using ClinicaSaaS.Domain.Personal;
using ClinicaSaaS.Domain.Personal.ValueObjects;
using ClinicaSaaS.Domain.Security.ValueObjects;
using ClinicaSaaS.SharedKernel.Results;

namespace ClinicaSaaS.Application.Pacientes;

public sealed class PacienteService(
    IRepositorio<Paciente> repositorio,
    IUnitOfWork unitOfWork,
    IClinicaService clinicaService,
    ITenantContext tenantContext,
    ICurrentUserContext currentUserContext) : IPacienteService
{
    public async Task<IReadOnlyList<PacienteDto>> ListarAsync(string? filtro = null, CancellationToken cancellationToken = default)
    {
        Expression<Func<Paciente, bool>> predicado = string.IsNullOrWhiteSpace(filtro)
            ? (_ => true)
            : ConstruirPredicadoBusqueda(filtro.Trim());

        var pacientes = await repositorio.ListarAsync(predicado, cancellationToken);
        var nombrePorClinica = await ObtenerNombresClinicasAsync(cancellationToken);

        return pacientes
            .OrderBy(p => p.Apellidos)
            .ThenBy(p => p.Nombres)
            .Select(p => MapearADto(p, nombrePorClinica))
            .ToList();
    }

    public async Task<PacienteDto?> ObtenerAsync(Guid pacienteId, CancellationToken cancellationToken = default)
    {
        var paciente = await repositorio.ObtenerPorIdAsync(pacienteId, cancellationToken);
        if (paciente is null)
            return null;

        var nombrePorClinica = await ObtenerNombresClinicasAsync(cancellationToken);
        return MapearADto(paciente, nombrePorClinica);
    }

    public async Task<Result<Guid>> RegistrarAsync(RegistrarPacienteRequest request, CancellationToken cancellationToken = default)
    {
        // AISLAMIENTO DE TENANT EN ESCRITURA — mismo criterio que Doctor/Empleado/Consultorio
        // (Módulos 3, 4 y 5): si el usuario que llama tiene una clínica activa en su sesión
        // (AdminClinica normal), esa es SIEMPRE la ClinicaId real. Solo un SuperAdmin SaaS
        // (sin clínica activa) puede especificar cualquier clínica.
        var clinicaId = tenantContext.ClinicaId ?? request.ClinicaId;

        var resultado = Paciente.Registrar(
            Guid.NewGuid(), clinicaId, request.Nombres, request.Apellidos,
            currentUserContext.UsuarioId ?? Guid.Empty, DateTime.UtcNow);
        if (resultado.EsFallido)
            return resultado.Error;

        var paciente = resultado.Value;
        await repositorio.AgregarAsync(paciente, cancellationToken);
        await unitOfWork.GuardarCambiosAsync(cancellationToken);

        return paciente.Id;
    }

    public async Task<Result> ActualizarDatosPersonalesAsync(
        Guid pacienteId, ActualizarDatosPersonalesPacienteRequest request, CancellationToken cancellationToken = default)
    {
        var paciente = await repositorio.ObtenerPorIdAsync(pacienteId, cancellationToken);
        if (paciente is null)
            return PacienteNoEncontrado;

        DocumentoIdentidad? documento = null;
        if (request.TipoDocumento is { } tipo && !string.IsNullOrWhiteSpace(request.NumeroDocumento))
        {
            var documentoResultado = DocumentoIdentidad.Crear(tipo, request.NumeroDocumento);
            if (documentoResultado.EsFallido)
                return documentoResultado.Error;

            documento = documentoResultado.Value;
        }

        var resultado = paciente.ActualizarDatosPersonales(
            request.Nombres, request.Apellidos, documento, request.FechaNacimiento, request.Genero);
        if (resultado.EsFallido)
            return resultado;

        await unitOfWork.GuardarCambiosAsync(cancellationToken);
        return Result.Exitoso();
    }

    public async Task<Result> ActualizarDatosDeContactoAsync(
        Guid pacienteId, ActualizarDatosDeContactoPacienteRequest request, CancellationToken cancellationToken = default)
    {
        var paciente = await repositorio.ObtenerPorIdAsync(pacienteId, cancellationToken);
        if (paciente is null)
            return PacienteNoEncontrado;

        Email? email = null;
        if (!string.IsNullOrWhiteSpace(request.Email))
        {
            var emailResultado = Email.Crear(request.Email);
            if (emailResultado.EsFallido)
                return emailResultado.Error;

            email = emailResultado.Value;
        }

        ContactoEmergencia? contacto = null;
        if (!string.IsNullOrWhiteSpace(request.ContactoEmergenciaNombre) || !string.IsNullOrWhiteSpace(request.ContactoEmergenciaTelefono))
        {
            var contactoResultado = ContactoEmergencia.Crear(
                request.ContactoEmergenciaNombre ?? string.Empty, request.ContactoEmergenciaTelefono ?? string.Empty);
            if (contactoResultado.EsFallido)
                return contactoResultado.Error;

            contacto = contactoResultado.Value;
        }

        var resultado = paciente.ActualizarDatosDeContacto(request.Telefono, email, request.Direccion, contacto);
        if (resultado.EsFallido)
            return resultado;

        await unitOfWork.GuardarCambiosAsync(cancellationToken);
        return Result.Exitoso();
    }

    public async Task<Result> EliminarAsync(Guid pacienteId, CancellationToken cancellationToken = default)
    {
        var paciente = await repositorio.ObtenerPorIdAsync(pacienteId, cancellationToken);
        if (paciente is null)
            return PacienteNoEncontrado;

        var resultado = paciente.Eliminar(currentUserContext.UsuarioId ?? Guid.Empty, DateTime.UtcNow);
        if (resultado.EsFallido)
            return resultado;

        await unitOfWork.GuardarCambiosAsync(cancellationToken);
        return Result.Exitoso();
    }

    private static Error PacienteNoEncontrado =>
        new("Paciente.NoEncontrado", "El paciente solicitado no existe.");

    private static Expression<Func<Paciente, bool>> ConstruirPredicadoBusqueda(string texto) =>
        p => p.Nombres.Contains(texto) || p.Apellidos.Contains(texto) ||
             (p.Documento != null && p.Documento.Numero.Contains(texto));

    private async Task<Dictionary<Guid, string>> ObtenerNombresClinicasAsync(CancellationToken cancellationToken)
    {
        var clinicas = await clinicaService.ListarAsync(cancellationToken);
        return clinicas.ToDictionary(c => c.Id, c => c.NombreComercial);
    }

    private static PacienteDto MapearADto(Paciente p, IReadOnlyDictionary<Guid, string> nombrePorClinica) => new(
        p.Id,
        p.ClinicaId,
        nombrePorClinica.TryGetValue(p.ClinicaId, out var nombreClinica) ? nombreClinica : p.ClinicaId.ToString(),
        p.Nombres,
        p.Apellidos,
        p.NombreCompleto,
        p.Documento?.Tipo,
        p.Documento?.Numero,
        p.FechaNacimiento,
        p.Genero,
        p.Telefono,
        p.Email?.Valor,
        p.Direccion,
        p.ContactoEmergencia?.Nombre,
        p.ContactoEmergencia?.Telefono);
}