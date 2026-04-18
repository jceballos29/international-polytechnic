using Identity.Application.Auth.Commands.Login;
using Identity.Application.OAuth.Commands.Authorize;
using Identity.Application.OAuth.Commands.ClientCredentials;
using Identity.Application.OAuth.Commands.ExchangeCode;
using Identity.Application.OAuth.Commands.RefreshToken;
using Identity.Application.OAuth.Commands.RevokeToken;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Identity.API.Controllers;

/// <summary>
/// Endpoints del protocolo OAuth 2.0 + OIDC.
///
/// GET  /oauth/authorize → inicia el flujo de login
/// POST /oauth/token     → emite tokens (todos los grant types)
/// POST /oauth/revoke    → revoca un token
/// </summary>
[ApiController]
[Route("oauth")]
public class OAuthController : ControllerBase
{
    private readonly IMediator _mediator;

    public OAuthController(IMediator mediator) => _mediator = mediator;

    /// <summary>
    /// Inicia el flujo Authorization Code.
    /// Valida los parámetros OAuth y redirige a identity-ui.
    /// </summary>
    [HttpGet("authorize")]
    public async Task<IActionResult> Authorize(
        [FromQuery] string client_id,
        [FromQuery] string redirect_uri,
        [FromQuery] string response_type,
        [FromQuery] string code_challenge,
        [FromQuery] string code_challenge_method,
        [FromQuery] string? state,
        [FromQuery] string? scope
    )
    {
        // Leer la cookie SSO del request
        var sessionId = Request.Cookies["idp_session"];

        var command = new AuthorizeCommand(
            ClientId: client_id,
            RedirectUri: redirect_uri,
            ResponseType: response_type,
            CodeChallenge: code_challenge,
            CodeChallengeMethod: code_challenge_method,
            State: state,
            Scope: scope,
            SessionId: sessionId
        ); // ← nuevo

        var result = await _mediator.Send(command);

        if (!result.IsSuccess)
            return BadRequest(
                new { error = result.ErrorCode, error_description = result.ErrorMessage }
            );

        // SSO o login — en ambos casos es un redirect
        return Redirect(result.Value!.Url);
    }

    /// <summary>
    /// Emite tokens según el grant_type recibido.
    ///
    /// grant_type=authorization_code → intercambia code por tokens
    /// grant_type=refresh_token      → renueva el access token
    /// grant_type=client_credentials → token M2M para apps
    /// </summary>
    [HttpPost("token")]
    [Consumes("application/x-www-form-urlencoded")]
    public async Task<IActionResult> Token(
        [FromForm] string grant_type,
        [FromForm] string? code,
        [FromForm] string? code_verifier,
        [FromForm] string? refresh_token,
        [FromForm] string? client_id,
        [FromForm] string? client_secret,
        [FromForm] string? redirect_uri,
        [FromForm] string? scope
    )
    {
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();

        return grant_type switch
        {
            "authorization_code" => await HandleAuthorizationCode(
                code,
                code_verifier,
                client_id,
                client_secret,
                redirect_uri,
                ip
            ),

            "refresh_token" => await HandleRefreshToken(
                refresh_token,
                client_id,
                client_secret,
                ip
            ),

            "client_credentials" => await HandleClientCredentials(
                client_id,
                client_secret,
                scope,
                ip
            ),

            _ => BadRequest(
                new
                {
                    error = "unsupported_grant_type",
                    error_description = $"El grant_type '{grant_type}' no está soportado.",
                }
            ),
        };
    }

    /// <summary>
    /// Revoca un token — logout o invalidación de seguridad.
    /// Siempre retorna 200 aunque el token no exista (RFC 7009).
    /// </summary>
    [HttpPost("revoke")]
    [Consumes("application/x-www-form-urlencoded")]
    public async Task<IActionResult> Revoke(
        [FromForm] string token,
        [FromForm] string? token_type_hint,
        [FromForm] string? client_id,
        [FromForm] string? client_secret
    )
    {
        var command = new RevokeTokenCommand(
            Token: token,
            TokenTypeHint: token_type_hint ?? "refresh_token",
            ClientId: client_id ?? string.Empty,
            ClientSecret: client_secret
        );

        await _mediator.Send(command);

        // RFC 7009: siempre 200, no revelar si el token existía
        return Ok();
    }

    // ── Helpers privados ──────────────────────────────────

    private async Task<IActionResult> HandleAuthorizationCode(
        string? code,
        string? codeVerifier,
        string? clientId,
        string? clientSecret,
        string? redirectUri,
        string? ip
    )
    {
        if (
            string.IsNullOrWhiteSpace(code)
            || string.IsNullOrWhiteSpace(codeVerifier)
            || string.IsNullOrWhiteSpace(clientId)
            || string.IsNullOrWhiteSpace(redirectUri)
        )
            return BadRequest(
                new
                {
                    error = "invalid_request",
                    error_description = "Faltan parámetros requeridos.",
                }
            );

        var command = new ExchangeCodeCommand(
            Code: code,
            CodeVerifier: codeVerifier,
            ClientId: clientId,
            ClientSecret: clientSecret ?? string.Empty,
            RedirectUri: redirectUri,
            IpAddress: ip
        );

        var result = await _mediator.Send(command);

        return result.IsSuccess
            ? Ok(result.Value)
            : BadRequest(new { error = result.ErrorCode, error_description = result.ErrorMessage });
    }

    private async Task<IActionResult> HandleRefreshToken(
        string? refreshToken,
        string? clientId,
        string? clientSecret,
        string? ip
    )
    {
        if (string.IsNullOrWhiteSpace(refreshToken) || string.IsNullOrWhiteSpace(clientId))
            return BadRequest(
                new
                {
                    error = "invalid_request",
                    error_description = "Faltan parámetros requeridos.",
                }
            );

        var command = new RefreshTokenCommand(
            RefreshToken: refreshToken,
            ClientId: clientId,
            ClientSecret: clientSecret ?? string.Empty,
            IpAddress: ip
        );

        var result = await _mediator.Send(command);

        return result.IsSuccess
            ? Ok(result.Value)
            : BadRequest(new { error = result.ErrorCode, error_description = result.ErrorMessage });
    }

    private async Task<IActionResult> HandleClientCredentials(
        string? clientId,
        string? clientSecret,
        string? scope,
        string? ip
    )
    {
        if (
            string.IsNullOrWhiteSpace(clientId)
            || string.IsNullOrWhiteSpace(clientSecret)
            || string.IsNullOrWhiteSpace(scope)
        )
            return BadRequest(
                new
                {
                    error = "invalid_request",
                    error_description = "Faltan parámetros requeridos.",
                }
            );

        var command = new ClientCredentialsCommand(
            ClientId: clientId,
            ClientSecret: clientSecret,
            Scope: scope,
            IpAddress: ip
        );

        var result = await _mediator.Send(command);

        return result.IsSuccess
            ? Ok(result.Value)
            : BadRequest(new { error = result.ErrorCode, error_description = result.ErrorMessage });
    }
}
