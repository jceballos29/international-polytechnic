namespace Identity.Application.Common.Interfaces;

/// <summary>
/// Contrato para verificación PKCE (Proof Key for Code Exchange).
///
/// PKCE protege el flujo Authorization Code contra intercepción.
///
/// Flujo:
///   Cliente genera: code_verifier (string aleatorio, 43-128 chars)
///   Cliente calcula: code_challenge = BASE64URL(SHA256(code_verifier))
///   Cliente envía code_challenge al iniciar el flujo
///   Cliente envía code_verifier al intercambiar el code por tokens
///   IdP verifica: SHA256(code_verifier) == code_challenge guardado
///
/// Solo soportamos S256 — "plain" es inseguro y está deprecado en OAuth 2.1
/// </summary>
public interface IPkceService
{
    /// <summary>
    /// Verifica que SHA256(verifier) == challenge.
    /// Retorna true si la verificación es exitosa.
    /// </summary>
    bool ValidateCodeChallenge(string codeVerifier, string codeChallenge, string method = "S256");
}
