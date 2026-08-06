using System.Text.RegularExpressions;
using ClinicaSaaS.SharedKernel.Results;

namespace ClinicaSaaS.Domain.Billing.ValueObjects;

/// <summary>
/// Value Object para el tipo de comprobante (ej. "E31","E32","E34" e-CF, o "B01","B02" NCF
/// legado). DELIBERADAMENTE NO es un enum: la DGII gestiona este catálogo de forma externa y
/// puede agregar/retirar tipos por regulación (ver nota de migración e-CF/Ley 32-23 en el
/// documento de BD) — un enum obligaría a recompilar el sistema cada vez que cambie el catálogo
/// oficial. El VO solo valida la forma (una letra + 2 dígitos), no pertenencia a un catálogo
/// cerrado, dejando esa verificación a un servicio de aplicación que sí pueda mantenerse
/// actualizado contra la normativa vigente sin tocar el dominio.
/// </summary>
public sealed record CodigoComprobanteFiscal
{
    private static readonly Regex Formato = new("^[A-Z]\\d{2}$", RegexOptions.Compiled);

    public string Valor { get; }
    public bool EsElectronico => Valor.StartsWith('E');

    private CodigoComprobanteFiscal(string valor) => Valor = valor;

    public static Result<CodigoComprobanteFiscal> Crear(string valor)
    {
        if (string.IsNullOrWhiteSpace(valor))
            return new Error("CodigoComprobanteFiscal.Vacio", "El tipo de comprobante es requerido.");

        var normalizado = valor.Trim().ToUpperInvariant();

        if (!Formato.IsMatch(normalizado))
            return new Error("CodigoComprobanteFiscal.Formato", "El tipo de comprobante debe tener una letra seguida de 2 dígitos (ej. E31, B01).");

        return new CodigoComprobanteFiscal(normalizado);
    }

    public override string ToString() => Valor;
}
