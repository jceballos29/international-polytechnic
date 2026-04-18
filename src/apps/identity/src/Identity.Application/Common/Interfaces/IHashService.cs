namespace Identity.Application.Common.Interfaces;

/// <summary>
/// Contrato para operaciones de hashing seguro.
///
/// Dos tipos de hash con propósitos distintos:
///
/// BCrypt → para passwords y client_secrets
///   Lento deliberadamente → fuerza bruta imposible
///   Incluye salt → dos passwords iguales tienen hashes distintos
///
/// SHA256 → para tokens opacos (refresh tokens, auth codes)
///   Rápido → necesitamos buscar en DB en cada request
///   No necesita ser lento porque el token ya es aleatorio (256 bits)
///   Sin salt → necesitamos poder calcular el mismo hash para buscar
/// </summary>
public interface IHashService
{
    // ── BCrypt — para passwords y client_secrets ───────────

    string HashPassword(string plaintext);
    bool VerifyPassword(string plaintext, string hash);

    // ── SHA256 — para tokens opacos ────────────────────────

    /// <summary>
    /// Calcula SHA256 del token y lo retorna en formato hex.
    /// "abc123..." → "d4e5f6..." (64 chars hex)
    /// Se usa para guardar y buscar refresh tokens y auth codes.
    /// </summary>
    string HashToken(string token);
}
