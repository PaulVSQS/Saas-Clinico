using System.Text.RegularExpressions;
using ClinicaSaaS.SharedKernel.Results;

namespace ClinicaSaaS.Domain.Security.ValueObjects;

/// <summary>
/// Value Object: dirección de correo electrónico. Se repite en Clinicas, Usuarios y Pacientes,
/// por lo que centralizar el formato evita tres validaciones divergentes.
/// </summary>
public sealed record Email
{
    private static readonly Regex Formato = new(
        @"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled);

    public string Valor { get; }

    private Email(string valor) => Valor = valor;

    public static Result<Email> Crear(string valor)
    {
        if (string.IsNullOrWhiteSpace(valor))
            return new Error("Email.Vacio", "El correo electrónico es requerido.");

        var normalizado = valor.Trim().ToLowerInvariant();

        if (normalizado.Length > 150)
            return new Error("Email.Longitud", "El correo electrónico excede la longitud máxima.");

        if (!Formato.IsMatch(normalizado))
            return new Error("Email.Formato", "El correo electrónico no tiene un formato válido.");

        return new Email(normalizado);
    }

    public override string ToString() => Valor;
}
