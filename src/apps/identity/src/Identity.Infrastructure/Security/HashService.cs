using System.Security.Cryptography;
using System.Text;
using Identity.Application.Common.Interfaces;

namespace Identity.Infrastructure.Security;

/// <summary>
/// Implementación de IHashService.
///
/// BCrypt para passwords y client_secrets:
///   - Work factor 11 → ~300ms por hash → fuerza bruta imposible
///   - Salt incluido automáticamente → hashes únicos aunque el input sea igual
///   - $2a$11$... → formato reconocible, longitud siempre 60 chars
///
/// SHA256 para tokens opacos (refresh tokens, auth codes):
///   - Rápido → necesitamos buscar en DB en cada request
///   - Los tokens ya son aleatorios (256 bits) → no necesitan salt
///   - Retorna hex lowercase de 64 chars → fácil de indexar en DB
/// </summary>
public class HashService : IHashService
{
    // Work factor 11 → balance entre seguridad y velocidad
    // Factor 10 = ~100ms, Factor 11 = ~200ms, Factor 12 = ~400ms
    private const int BcryptWorkFactor = 11;

    public string HashPassword(string plaintext)
    {
        if (string.IsNullOrWhiteSpace(plaintext))
            throw new ArgumentException(
                "El texto a hashear no puede estar vacío.",
                nameof(plaintext)
            );

        return BCrypt.Net.BCrypt.HashPassword(plaintext, BcryptWorkFactor);
    }

    public bool VerifyPassword(string plaintext, string hash)
    {
        if (string.IsNullOrWhiteSpace(plaintext) || string.IsNullOrWhiteSpace(hash))
            return false;

        try
        {
            return BCrypt.Net.BCrypt.Verify(plaintext, hash);
        }
        catch
        {
            // Si el hash está malformado → no coincide
            return false;
        }
    }

    public string HashToken(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            throw new ArgumentException("El token no puede estar vacío.", nameof(token));

        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));

        // Convertir a string hex lowercase
        // "d4e5f6a7..." → 64 caracteres
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
