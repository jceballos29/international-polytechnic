using System.Security.Cryptography;
using System.Text;
using Identity.Application.Common.Interfaces;

namespace Identity.Infrastructure.Security;

/// <summary>
/// Implementación de IPkceService.
///
/// Verifica que BASE64URL(SHA256(code_verifier)) == code_challenge
///
/// BASE64URL difiere de Base64 estándar:
///   - Usa '-' en lugar de '+'
///   - Usa '_' en lugar de '/'
///   - Sin padding '=' al final
/// Esto lo hace seguro para usar en URLs sin encoding.
/// </summary>
public class PkceService : IPkceService
{
    public bool ValidateCodeChallenge(
        string codeVerifier,
        string codeChallenge,
        string method = "S256"
    )
    {
        if (string.IsNullOrWhiteSpace(codeVerifier) || string.IsNullOrWhiteSpace(codeChallenge))
            return false;

        // Solo soportamos S256 — "plain" es inseguro
        if (!string.Equals(method, "S256", StringComparison.OrdinalIgnoreCase))
            return false;

        // Calcular SHA256 del verifier
        var verifierBytes = Encoding.ASCII.GetBytes(codeVerifier);
        var hashBytes = SHA256.HashData(verifierBytes);

        // Convertir a BASE64URL (sin padding)
        var computedChallenge = Convert
            .ToBase64String(hashBytes)
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');

        // Comparación timing-safe para evitar timing attacks
        return CryptographicOperations.FixedTimeEquals(
            Encoding.ASCII.GetBytes(computedChallenge),
            Encoding.ASCII.GetBytes(codeChallenge)
        );
    }
}
