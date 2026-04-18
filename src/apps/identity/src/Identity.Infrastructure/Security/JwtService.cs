using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;
using Identity.Application.Common.Interfaces;
using Identity.Application.Common.Models;
using Identity.Domain.Entities;
using Microsoft.IdentityModel.Tokens;

namespace Identity.Infrastructure.Security;

/// <summary>
/// Implementación de IJwtService usando RS256.
///
/// RS256 = RSA Signature with SHA-256
///   - Firma con clave privada RSA (solo el IdP la tiene)
///   - Verifica con clave pública (cualquier API puede descargarla)
///
/// El par de claves se lee de archivos .pem al iniciar.
/// La clave privada NUNCA sale del IdP.
/// La clave pública se expone en /.well-known/jwks.json
/// </summary>
public class JwtService : IJwtService
{
    private readonly RsaSecurityKey _privateKey;
    private readonly RsaSecurityKey _publicKey;
    private readonly SigningCredentials _signingCredentials;
    private readonly string _issuer;
    private readonly int _accessTokenExpiryMinutes;

    public JwtService(JwtSettings settings)
    {
        _issuer = settings.Issuer;
        _accessTokenExpiryMinutes = settings.AccessTokenExpiryMinutes;

        var privateRsa = RSA.Create();
        privateRsa.ImportFromPem(File.ReadAllText(settings.PrivateKeyPath));
        _privateKey = new RsaSecurityKey(privateRsa) { KeyId = "identity-key-v1" };

        var publicRsa = RSA.Create();
        publicRsa.ImportFromPem(File.ReadAllText(settings.PublicKeyPath));
        _publicKey = new RsaSecurityKey(publicRsa) { KeyId = "identity-key-v1" };

        _signingCredentials = new SigningCredentials(_privateKey, SecurityAlgorithms.RsaSha256);
    }

    public string GenerateAccessToken(
        User user,
        ClientApplication clientApp,
        IReadOnlyList<string> roles,
        IReadOnlyList<string> permissions,
        string sessionId
    )
    {
        var claims = BuildUserClaims(user, clientApp, roles, permissions, sessionId);
        return CreateToken(
            claims,
            clientApp.ClientId.Value,
            TimeSpan.FromMinutes(_accessTokenExpiryMinutes)
        );
    }

    public string GenerateM2MAccessToken(ClientApplication clientApp, IReadOnlyList<string> scopes)
    {
        var claims = new List<Claim>
        {
            // En M2M no hay 'sub' de usuario — el subject es el client_id
            new(JwtRegisteredClaimNames.Sub, clientApp.ClientId.Value),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new("client_id", clientApp.ClientId.Value),
            new("scope", string.Join(" ", scopes)),
        };

        return CreateToken(
            claims,
            clientApp.ClientId.Value,
            TimeSpan.FromMinutes(_accessTokenExpiryMinutes)
        );
    }

    public string GenerateRefreshToken()
    {
        // 32 bytes aleatorios → 256 bits de entropía
        // Suficientemente grande para ser único y no predecible
        var bytes = new byte[32];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes).Replace('+', '-').Replace('/', '_').TrimEnd('=');
        // BASE64URL → seguro para cookies y headers
    }

    public string GetJwks()
    {
        // JWKS = JSON Web Key Set
        // Formato estándar para exponer claves públicas
        var parameters = _publicKey.Rsa.ExportParameters(false);
        // false → solo parámetros públicos, nunca los privados

        var jwks = new
        {
            keys = new[]
            {
                new
                {
                    kty = "RSA", // Key Type
                    use = "sig", // Key Use: signing
                    alg = "RS256", // Algorithm
                    kid = _publicKey.KeyId, // Key ID
                    // n y e son los parámetros públicos RSA
                    // n = modulus, e = exponent
                    n = Base64UrlEncode(parameters.Modulus!),
                    e = Base64UrlEncode(parameters.Exponent!),
                },
            },
        };

        return JsonSerializer.Serialize(jwks);
    }

    public string? ExtractJti(string token)
    {
        try
        {
            var handler = new JwtSecurityTokenHandler();
            var jwt = handler.ReadJwtToken(token);
            return jwt.Id; // El claim 'jti'
        }
        catch
        {
            return null;
        }
    }

    // ── Helpers privados ──────────────────────────────────

    private List<Claim> BuildUserClaims(
        User user,
        ClientApplication clientApp,
        IReadOnlyList<string> roles,
        IReadOnlyList<string> permissions,
        string sessionId
    )
    {
        var claims = new List<Claim>
        {
            // Claims estándar OIDC/JWT
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email.Value),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new("tenant_id", user.TenantId.ToString()),
            new("client_id", clientApp.ClientId.Value),
            new("sid", sessionId),
        };

        // Nombre del usuario si existe
        if (user.Name is not null)
        {
            claims.Add(new(JwtRegisteredClaimNames.Name, user.Name.FullName));
            claims.Add(new(JwtRegisteredClaimNames.GivenName, user.Name.FirstName));
            claims.Add(new(JwtRegisteredClaimNames.FamilyName, user.Name.LastNames));
        }

        // Roles — pueden ser múltiples
        foreach (var role in roles)
            claims.Add(new(ClaimTypes.Role, role));

        // Permisos granulares
        foreach (var permission in permissions)
            claims.Add(new("permission", permission));

        return claims;
    }

    private string CreateToken(List<Claim> claims, string audience, TimeSpan expiry)
    {
        var now = DateTime.UtcNow;
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Issuer = _issuer,
            Audience = audience,
            IssuedAt = now,
            NotBefore = now,
            Expires = now.Add(expiry),
            SigningCredentials = _signingCredentials,
        };

        var handler = new JwtSecurityTokenHandler();
        var token = handler.CreateToken(tokenDescriptor);
        return handler.WriteToken(token);
    }

    private static string Base64UrlEncode(byte[] bytes) =>
        Convert.ToBase64String(bytes).Replace('+', '-').Replace('/', '_').TrimEnd('=');
}
