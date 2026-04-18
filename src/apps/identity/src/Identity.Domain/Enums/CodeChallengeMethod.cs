namespace Identity.Domain.Enums;

/// <summary>
/// Método de transformación del code_verifier para PKCE.
///
/// ¿Qué es PKCE?
/// Proof Key for Code Exchange — protege el flujo Authorization Code
/// contra ataques de intercepción del authorization_code.
///
/// Cómo funciona:
///   1. El cliente genera un string aleatorio: code_verifier
///   2. Calcula: code_challenge = BASE64URL(SHA256(code_verifier))
///   3. Envía code_challenge al iniciar el flujo
///   4. Al intercambiar el code, envía el code_verifier original
///   5. El IdP verifica: SHA256(verifier) == challenge guardado
///
/// S256 → único método permitido en nuestra implementación.
///   code_challenge = BASE64URL(SHA256(code_verifier))
///   Seguro porque SHA256 es irreversible — aunque alguien
///   intercepte el challenge, no puede calcular el verifier.
///
/// Plain → PROHIBIDO. Inseguro porque challenge == verifier.
///   Si alguien intercepta el challenge, tiene el verifier.
///   OAuth 2.1 lo depreca — nunca lo implementamos.
/// </summary>
public enum CodeChallengeMethod
{
    S256,
    // Plain intencionalmente omitido — es inseguro
}
