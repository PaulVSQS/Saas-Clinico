namespace ClinicaSaaS.SharedKernel.Results;

/// <summary>
/// Resultado de validación con múltiples errores.
/// Permite acumular todos los errores de validación antes de retornar,
/// en lugar de fallar en el primer error encontrado.
/// </summary>
public sealed class ValidationResult<T> : Result<T>
{
    private ValidationResult(Error[] errores)
        : base(false, default, Error.Validacion)
    {
        Errores = errores;
    }

    public Error[] Errores { get; }

    public static ValidationResult<T> ConErrores(Error[] errores)
    {
        ArgumentNullException.ThrowIfNull(errores);
        if (errores.Length == 0)
            throw new ArgumentException("Se requiere al menos un error de validación.", nameof(errores));

        return new ValidationResult<T>(errores);
    }
}
