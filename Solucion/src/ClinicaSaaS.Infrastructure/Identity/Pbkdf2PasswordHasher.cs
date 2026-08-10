using ClinicaSaaS.Application.Common.Interfaces;
using ClinicaSaaS.Domain.Security;
using Microsoft.AspNetCore.Identity;

namespace ClinicaSaaS.Infrastructure.Identity;

/// <summary>
/// Implementación real de <see cref="IPasswordHasher"/>. NO usamos ASP.NET Core Identity
/// completo (UserManager/SignInManager/stores con sus propias tablas AspNetUsers) — el dominio
/// ya tiene su propio Aggregate Root Usuario (Fase 2) con su propio PasswordHash, y duplicar
/// eso con las tablas de Identity sería tener dos fuentes de verdad de usuarios. En cambio,
/// reutilizamos <see cref="PasswordHasher{TUser}"/> — la clase de utilidad de Identity Core que
/// SOLO hace hashing PBKDF2 (RFC 2898), sin ningún store ni tabla propia — para no reinventar
/// la parte criptográfica a mano.
/// <c>PasswordHasher&lt;TUser&gt;</c> no lee ninguna propiedad del objeto que se le pasa (su
/// parámetro "user" es vestigial, solo existe por la firma genérica de su interfaz); pasar
/// <c>null!</c> ahí es seguro y es el patrón usual cuando se usa esta clase de forma aislada.
/// Se pasa POSICIONAL (no con nombre de parámetro): el parámetro real de la API de Microsoft
/// se llama "user", en inglés — nombrarlo "usuario:" en la llamada rompe la compilación.
/// </summary>
public sealed class Pbkdf2PasswordHasher : IPasswordHasher
{
    private readonly PasswordHasher<Usuario> _hasher = new();

    public byte[] HashearPassword(string passwordPlano)
    {
        var hashBase64 = _hasher.HashPassword(null!, passwordPlano);
        return Convert.FromBase64String(hashBase64);
    }

    public ResultadoVerificacionPassword VerificarPassword(byte[] passwordHash, string passwordPlano)
    {
        var hashBase64 = Convert.ToBase64String(passwordHash);
        var resultado = _hasher.VerifyHashedPassword(null!, hashBase64, passwordPlano);

        return resultado switch
        {
            PasswordVerificationResult.Success => ResultadoVerificacionPassword.Correcta,
            PasswordVerificationResult.SuccessRehashNeeded => ResultadoVerificacionPassword.CorrectaRequiereRehash,
            _ => ResultadoVerificacionPassword.Incorrecta
        };
    }
}