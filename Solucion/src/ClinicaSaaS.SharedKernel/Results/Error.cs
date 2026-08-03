namespace ClinicaSaaS.SharedKernel.Results;

/// <summary>
/// Representa un error tipado con código y descripción.
/// Los códigos siguen la convención: "Entidad.TipoError" (ej: "Clinica.NoEncontrada").
/// Usar constantes en los dominios en vez de strings directos para evitar errores tipográficos.
/// </summary>
public sealed record Error(string Codigo, string Descripcion)
{
    /// <summary>Error nulo — representa ausencia de error. Usar en resultados exitosos.</summary>
    public static readonly Error None = new(string.Empty, string.Empty);

    /// <summary>Error de validación genérico.</summary>
    public static readonly Error Validacion = new("General.Validacion", "Ocurrió un error de validación.");

    /// <summary>Recurso no encontrado genérico.</summary>
    public static readonly Error NoEncontrado = new("General.NoEncontrado", "El recurso solicitado no fue encontrado.");

    /// <summary>Acceso no autorizado.</summary>
    public static readonly Error NoAutorizado = new("General.NoAutorizado", "No tiene permisos para realizar esta acción.");

    public bool EsNone => this == None;

    public override string ToString() => $"[{Codigo}] {Descripcion}";
}
