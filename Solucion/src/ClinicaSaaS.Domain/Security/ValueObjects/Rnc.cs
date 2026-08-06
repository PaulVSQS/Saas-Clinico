using System.Text.RegularExpressions;
using ClinicaSaaS.SharedKernel.Results;

namespace ClinicaSaaS.Domain.Security.ValueObjects;

/// <summary>
/// Value Object: Registro Nacional de Contribuyente (identificación fiscal DGII de una clínica).
/// Se valida el formato numérico (9 u 11 dígitos, según sea RNC de empresa o cédula de persona
/// física con negocio propio) pero no se verifica contra el padrón DGII — eso es una integración
/// externa fuera del alcance del dominio.
/// </summary>
public sealed record Rnc
{
    private static readonly Regex Formato = new(@"^\d{9}(\d{2})?$", RegexOptions.Compiled);

    public string Valor { get; }

    private Rnc(string valor) => Valor = valor;

    public static Result<Rnc> Crear(string valor)
    {
        if (string.IsNullOrWhiteSpace(valor))
            return new Error("Rnc.Vacio", "El RNC es requerido.");

        var limpio = valor.Trim().Replace("-", "");

        if (!Formato.IsMatch(limpio))
            return new Error("Rnc.Formato", "El RNC debe tener 9 u 11 dígitos.");

        return new Rnc(limpio);
    }

    public override string ToString() => Valor;
}
