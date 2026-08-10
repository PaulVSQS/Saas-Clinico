namespace ClinicaSaaS.Application.Common.Interfaces;

/// <summary>Resultado de verificar una contraseña contra su hash almacenado.</summary>
public enum ResultadoVerificacionPassword
{
    /// <summary>La contraseña es correcta.</summary>
    Correcta,

    /// <summary>La contraseña es incorrecta.</summary>
    Incorrecta,

    /// <summary>
    /// La contraseña es correcta, pero el hash fue creado con una versión/parametrización más
    /// antigua del algoritmo — el llamador debería volver a hashear la contraseña con
    /// <see cref="IPasswordHasher.HashearPassword"/> y guardar el nuevo hash. Hoy el sistema
    /// solo usa una versión del algoritmo, así que este caso no ocurre en la práctica todavía,
    /// pero se modela desde ahora para no tener que cambiar la interfaz el día que sí ocurra.
    /// </summary>
    CorrectaRequiereRehash
}

/// <summary>
/// Abstracción del algoritmo de hashing de contraseñas. Application nunca ve ni compara texto
/// plano contra el hash directamente — siempre a través de esta interfaz, cuya implementación
/// real (Infrastructure) es la única capa que sabe qué algoritmo/parámetros criptográficos se
/// usan hoy.
/// </summary>
public interface IPasswordHasher
{
    /// <summary>Genera el hash a guardar en <c>Usuario.PasswordHash</c> a partir de la contraseña en texto plano.</summary>
    byte[] HashearPassword(string passwordPlano);

    /// <summary>Verifica una contraseña en texto plano contra un hash ya almacenado.</summary>
    ResultadoVerificacionPassword VerificarPassword(byte[] passwordHash, string passwordPlano);
}
