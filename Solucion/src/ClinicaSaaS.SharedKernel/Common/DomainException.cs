namespace ClinicaSaaS.SharedKernel.Common;

/// <summary>
/// Excepción reservada para violaciones de invariantes que jamás deberían ocurrir si el resto
/// del dominio se usó correctamente (ej. un método interno del agregado detecta un estado
/// inconsistente). NO se usa para validación de negocio esperada (usar Result&lt;T&gt; para eso) —
/// ver comentario en Result.cs. Su propósito es hacer ruido fuerte ante bugs reales.
/// </summary>
public sealed class DomainException : Exception
{
    public string Codigo { get; }

    public DomainException(string codigo, string mensaje) : base(mensaje)
    {
        Codigo = codigo;
    }
}
