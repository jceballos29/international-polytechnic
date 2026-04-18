namespace Identity.Domain.Enums;

/// <summary>
/// Flujos OAuth 2.0 soportados por el sistema.
///
/// AuthorizationCode → para usuarios humanos.
///   El usuario se autentica en el IdP, la app recibe un code
///   temporal y lo intercambia por tokens. Siempre con PKCE.
///   Usado por: portal, admin-panel, universitas-ui, gradus-ui
///
/// ClientCredentials → para comunicación entre servicios (M2M).
///   La app se autentica con client_id + client_secret.
///   No hay usuario — el token representa a la app.
///   Usado por: gradus-api (para llamar a universitas-api)
///
/// RefreshToken → para renovar un Access Token expirado.
///   No se registra explícitamente — es implícito cuando
///   se permite AuthorizationCode.
/// </summary>
public enum GrantType
{
    AuthorizationCode,
    ClientCredentials,
    RefreshToken,
}
