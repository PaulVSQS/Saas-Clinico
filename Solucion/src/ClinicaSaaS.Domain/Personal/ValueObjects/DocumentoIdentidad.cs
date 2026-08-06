using ClinicaSaaS.Domain.Personal.Enums;
using ClinicaSaaS.SharedKernel.Results;

namespace ClinicaSaaS.Domain.Personal.ValueObjects;

/// <summary>
/// Value Object: identificación de un paciente. Se modela Tipo+Numero como una sola unidad
/// (en vez de dos propiedades sueltas en Paciente) porque el formato válido del número depende
/// del tipo — separarlos permitiría un estado inconsistente como TipoDocumento=Cedula con un
/// número de pasaporte. Es opcional a nivel de Paciente (recién nacidos sin documento aún).
/// </summary>
public sealed record DocumentoIdentidad
{
    public TipoDocumentoIdentidad Tipo { get; }
    public string Numero { get; }

    private DocumentoIdentidad(TipoDocumentoIdentidad tipo, string numero)
    {
        Tipo = tipo;
        Numero = numero;
    }

    public static Result<DocumentoIdentidad> Crear(TipoDocumentoIdentidad tipo, string numero)
    {
        if (string.IsNullOrWhiteSpace(numero))
            return new Error("DocumentoIdentidad.NumeroRequerido", "El número de documento es requerido.");

        var limpio = numero.Trim().Replace("-", "");

        if (tipo == TipoDocumentoIdentidad.Cedula && limpio.Length != 11)
            return new Error("DocumentoIdentidad.FormatoCedula", "La cédula dominicana debe tener 11 dígitos.");

        if (limpio.Length > 20)
            return new Error("DocumentoIdentidad.Longitud", "El número de documento excede la longitud máxima.");

        return new DocumentoIdentidad(tipo, limpio);
    }
}
