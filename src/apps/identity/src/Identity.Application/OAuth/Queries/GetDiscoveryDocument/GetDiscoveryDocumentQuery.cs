using MediatR;

namespace Identity.Application.OAuth.Queries.GetDiscoveryDocument;

/// <summary>
/// Retorna el OpenID Connect Discovery Document.
///
/// Este documento es el "mapa" del IdP — describe todos sus
/// endpoints y capacidades. Las apps clientes lo descargan
/// automáticamente para configurarse:
///
///   new JwtBearer { Authority = "http://localhost:5000" }
///   → descarga http://localhost:5000/.well-known/openid-configuration
///   → lee los endpoints automáticamente
///   → descarga jwks_uri para obtener las claves públicas
///
/// Se cachea agresivamente — cambia muy raramente.
/// </summary>
public record GetDiscoveryDocumentQuery(string BaseUrl) : IRequest<DiscoveryDocumentResult>;

public record DiscoveryDocumentResult(
    string Issuer,
    string AuthorizationEndpoint,
    string TokenEndpoint,
    string UserInfoEndpoint,
    string JwksUri,
    string RevocationEndpoint,
    IReadOnlyList<string> ResponseTypesSupported,
    IReadOnlyList<string> GrantTypesSupported,
    IReadOnlyList<string> SubjectTypesSupported,
    IReadOnlyList<string> IdTokenSigningAlgValuesSupported,
    IReadOnlyList<string> ScopesSupported
);
