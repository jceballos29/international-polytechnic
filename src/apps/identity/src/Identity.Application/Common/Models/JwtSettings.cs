namespace Identity.Application.Common.Models;

/// <summary>
/// Configuración de JWT disponible en la capa Application.
///
/// ¿Por qué una clase de settings y no IConfiguration directamente?
/// Application no debe saber que existe IConfiguration — eso es
/// un detalle de infraestructura. En cambio, definimos un objeto
/// tipado que Infrastructure popula desde IConfiguration.
///
/// Esto también hace los tests más simples — puedes crear
/// un JwtSettings directamente sin simular IConfiguration.
/// </summary>
public class JwtSettings
{
    public string Issuer { get; init; } = string.Empty;
    public int AccessTokenExpiryMinutes { get; init; } = 15;
    public int RefreshTokenExpiryDays { get; init; } = 30;
    public string PrivateKeyPath { get; init; } = string.Empty;
    public string PublicKeyPath { get; init; } = string.Empty;
}
