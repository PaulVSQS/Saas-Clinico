namespace ClinicaSaaS.SharedKernel.Results;

/// <summary>
/// Resultado tipado que encapsula éxito o fracaso de una operación.
/// PRINCIPIO: Las excepciones son para condiciones verdaderamente excepcionales (bugs, fallos de infraestructura).
/// El flujo de negocio esperado (validaciones, not found, acceso denegado) usa Result para
/// evitar el costoso stack unwinding de excepciones y hacer el flujo explícito y trazable.
/// </summary>
public class Result<T>
{
    protected readonly T? _value;

    protected Result(bool esExitoso, T? value, Error error)
    {
        EsExitoso = esExitoso;
        _value = value;
        Error = error;
    }

    public bool EsExitoso { get; }
    public bool EsFallido => !EsExitoso;
    public Error Error { get; }

    public T Value => EsExitoso
        ? _value!
        : throw new InvalidOperationException(
            $"No se puede acceder al valor de un resultado fallido. Error: {Error}");

    public static Result<T> Exitoso(T value) => new(true, value, Error.None);

    public static Result<T> Fallido(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);
        return new(false, default, error);
    }

    public static implicit operator Result<T>(T value) => Exitoso(value);
    public static implicit operator Result<T>(Error error) => Fallido(error);

    public TResult Match<TResult>(
        Func<T, TResult> exito,
        Func<Error, TResult> falla) =>
        EsExitoso ? exito(_value!) : falla(Error);
}

/// <summary>
/// Versión sin valor de retorno — para operaciones que solo indican éxito/fallo.
/// </summary>
public class Result
{
    protected Result(bool esExitoso, Error error)
    {
        EsExitoso = esExitoso;
        Error = error;
    }

    public bool EsExitoso { get; }
    public bool EsFallido => !EsExitoso;
    public Error Error { get; }

    public static Result Exitoso() => new(true, Error.None);
    public static Result Fallido(Error error) => new(false, error);

    public static implicit operator Result(Error error) => Fallido(error);
}
