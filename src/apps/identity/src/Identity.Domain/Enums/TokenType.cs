namespace Identity.Domain.Enums;

/// <summary>
/// Tipos de tokens que el sistema emite o procesa.
///
/// AccessToken → JWT firmado con RS256. Vida corta (15 min).
///   Se incluye en cada request como Bearer token.
///   Las APIs lo validan localmente con la clave pública del IdP.
///   Nunca necesitan llamar al IdP para validarlo.
///
/// RefreshToken → string aleatorio opaco (NO es un JWT).
///   Vida larga (30 días). Se guarda hasheado en PostgreSQL.
///   Solo sirve para obtener nuevos Access Tokens.
///   Se invalida en cada uso — se emite uno nuevo (rotación).
///
/// AuthorizationCode → string aleatorio opaco. Vida muy corta (2 min).
///   Viaja en la URL de redirect después del login.
///   Un solo uso — se invalida al intercambiarse por tokens.
///
/// ¿Por qué necesitamos diferenciarlos?
/// El endpoint POST /oauth/revoke acepta tanto AccessToken como
/// RefreshToken — necesitamos saber cuál es para aplicar la
/// lógica de revocación correcta para cada tipo.
/// </summary>
public enum TokenType
{
    AccessToken,
    RefreshToken,
    AuthorizationCode,
}
