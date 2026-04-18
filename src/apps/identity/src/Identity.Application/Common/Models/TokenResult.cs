namespace Identity.Application.Common.Models;

/// <summary>
/// Respuesta del endpoint POST /oauth/token.
/// Contiene los tokens emitidos después de una autenticación exitosa.
///
/// access_token  → JWT firmado RS256, vida corta (15 min)
///                 Se incluye en cada request como Bearer token
/// refresh_token → string opaco, vida larga (30 días)
///                 Solo para renovar el access_token
/// token_type    → siempre "Bearer" — estándar OAuth
/// expires_in    → segundos hasta que expira el access_token
///                 El frontend lo usa para saber cuándo renovar
/// scope         → scopes concedidos (puede ser menor a los solicitados)
/// </summary>
public class TokenResult
{
    public string AccessToken { get; init; } = string.Empty;
    public string RefreshToken { get; init; } = string.Empty;
    public string TokenType { get; init; } = "Bearer";
    public int ExpiresIn { get; init; }
    public string Scope { get; init; } = string.Empty;
}
