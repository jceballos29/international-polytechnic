using Identity.Application.Common.Interfaces;
using Identity.Application.OAuth.Queries.GetDiscoveryDocument;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Identity.API.Controllers;

/// <summary>
/// Endpoints de descubrimiento OIDC.
/// Públicos — no requieren autenticación.
///
/// /.well-known/openid-configuration → Discovery Document
///   Describe todos los endpoints y capacidades del IdP.
///   Las apps lo leen automáticamente al configurar JwtBearer.
///
/// /.well-known/jwks.json → JSON Web Key Set
///   Expone la clave pública RSA para verificar tokens.
///   Las APIs la descargan y cachean para validar tokens localmente.
/// </summary>
[ApiController]
[Route(".well-known")]
public class WellKnownController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IJwtService _jwtService;

    public WellKnownController(IMediator mediator, IJwtService jwtService)
    {
        _mediator = mediator;
        _jwtService = jwtService;
    }

    /// <summary>
    /// OpenID Connect Discovery Document.
    /// Sigue el estándar RFC 8414 y OpenID Connect Discovery 1.0.
    /// </summary>
    [HttpGet("openid-configuration")]
    [ResponseCache(Duration = 3600)] // cachear 1 hora en el browser
    public async Task<IActionResult> GetDiscoveryDocument()
    {
        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        var result = await _mediator.Send(new GetDiscoveryDocumentQuery(baseUrl));

        // Serializar con nombres en snake_case — estándar OIDC
        return Ok(
            new
            {
                issuer = result.Issuer,
                authorization_endpoint = result.AuthorizationEndpoint,
                token_endpoint = result.TokenEndpoint,
                userinfo_endpoint = result.UserInfoEndpoint,
                jwks_uri = result.JwksUri,
                revocation_endpoint = result.RevocationEndpoint,
                response_types_supported = result.ResponseTypesSupported,
                grant_types_supported = result.GrantTypesSupported,
                subject_types_supported = result.SubjectTypesSupported,
                id_token_signing_alg_values_supported = result.IdTokenSigningAlgValuesSupported,
                scopes_supported = result.ScopesSupported,
            }
        );
    }

    /// <summary>
    /// JSON Web Key Set — clave pública RSA del IdP.
    /// Las APIs la descargan para verificar tokens localmente.
    /// </summary>
    [HttpGet("jwks.json")]
    [ResponseCache(Duration = 3600)]
    public IActionResult GetJwks()
    {
        var jwks = _jwtService.GetJwks();
        return Content(jwks, "application/json");
    }
}
