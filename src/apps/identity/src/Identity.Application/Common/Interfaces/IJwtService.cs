using Identity.Application.Common.Models;
using Identity.Domain.Entities;

namespace Identity.Application.Common.Interfaces;

/// <summary>
/// Contrato para generación y validación de tokens JWT.
///
/// La implementación usa RS256 (asimétrico):
///   - Firma con clave PRIVADA (solo el IdP la tiene)
///   - Verifica con clave PÚBLICA (cualquier API la puede descargar)
///
/// Esto permite que Universitas.API y Gradus.API validen tokens
/// localmente sin llamar al IdP en cada request.
/// </summary>
public interface IJwtService
{
    /// <summary>
    /// Genera un Access Token JWT firmado con RS256.
    /// Incluye: sub, aud, iss, exp, iat, jti, roles, permissions
    /// </summary>
    string GenerateAccessToken(
        User user,
        ClientApplication clientApp,
        IReadOnlyList<string> roles,
        IReadOnlyList<string> permissions,
        string sessionId
    );

    /// <summary>
    /// Genera un token opaco aleatorio para usar como Refresh Token.
    /// No es un JWT — es un string aleatorio seguro de 32 bytes.
    /// Se guarda hasheado en la DB.
    /// Retorna el token en texto plano (solo para enviarlo al cliente).
    /// </summary>
    string GenerateRefreshToken();

    /// <summary>
    /// Genera un Access Token M2M para flujo client_credentials.
    /// No tiene sub (no hay usuario) — solo identidad de la app.
    /// </summary>
    string GenerateM2MAccessToken(ClientApplication clientApp, IReadOnlyList<string> scopes);

    /// <summary>
    /// Retorna el JWKS (JSON Web Key Set) con la clave pública RSA.
    /// Se expone en /.well-known/jwks.json para que las APIs
    /// puedan verificar tokens localmente.
    /// </summary>
    string GetJwks();

    /// <summary>
    /// Extrae el jti (JWT ID) de un token sin validar la firma.
    /// Se usa al revocar un Access Token — necesitamos el jti
    /// para agregarlo a la blocklist en Redis.
    /// </summary>
    string? ExtractJti(string token);
}
