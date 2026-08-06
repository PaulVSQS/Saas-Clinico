using System.Text.RegularExpressions;
using ClinicaSaaS.SharedKernel.Results;

namespace ClinicaSaaS.Domain.Clinical.ValueObjects;

/// <summary>
/// Value Object: huella SHA-256 de un archivo clínico. Se modela como VO (no string suelto)
/// porque su propósito es una invariante concreta —evidencia de no-alteración— y validar el
/// formato hexadecimal de 64 caracteres en la frontera del dominio evita que un valor corrupto
/// llegue a persistirse como "evidencia" válida.
/// </summary>
public sealed record HashArchivo
{
    private static readonly Regex FormatoSha256 = new("^[a-fA-F0-9]{64}$", RegexOptions.Compiled);

    public string Valor { get; }

    private HashArchivo(string valor) => Valor = valor;

    public static Result<HashArchivo> Crear(string valor)
    {
        if (string.IsNullOrWhiteSpace(valor) || !FormatoSha256.IsMatch(valor))
            return new Error("HashArchivo.Formato", "El hash debe ser SHA-256 válido (64 caracteres hexadecimales).");

        return new HashArchivo(valor.ToLowerInvariant());
    }
}
