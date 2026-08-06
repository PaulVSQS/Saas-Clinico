namespace ClinicaSaaS.SharedKernel.Common;

/// <summary>
/// Precondiciones de programación (no reglas de negocio). Si una Guard falla, es un bug del
/// código que llama al dominio incorrectamente (ej. pasar un Guid.Empty a un constructor privado
/// desde el propio ensamblado), no un caso de negocio esperado. Por eso lanza excepciones en vez
/// de devolver Result — las reglas de negocio (formato de RNC, cita en el pasado, etc.) usan
/// Result&lt;T&gt; en los factory methods de entidades y Value Objects, nunca Guard.
/// </summary>
public static class Guard
{
    public static void ContraNulo<T>(T? valor, string nombreParametro) where T : class
    {
        if (valor is null)
            throw new ArgumentNullException(nombreParametro);
    }

    public static void ContraVacio(string? valor, string nombreParametro)
    {
        if (string.IsNullOrWhiteSpace(valor))
            throw new ArgumentException("El valor no puede estar vacío.", nombreParametro);
    }

    public static void ContraGuidVacio(Guid valor, string nombreParametro)
    {
        if (valor == Guid.Empty)
            throw new ArgumentException("El Id no puede ser un GUID vacío.", nombreParametro);
    }

    public static void ContraNegativo(decimal valor, string nombreParametro)
    {
        if (valor < 0)
            throw new ArgumentException("El valor no puede ser negativo.", nombreParametro);
    }
}
