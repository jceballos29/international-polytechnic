using Identity.Application.Common.Models;
using MediatR;

namespace Identity.Application.Auth.Commands.Login;

/// <summary>
/// Autentica un usuario.
///
/// Modo OAuth (clientId presente):
///   Valida la app, crea sesión SSO y emite authorization_code.
///   El code se intercambia por tokens en POST /oauth/token.
///
/// Modo directo (clientId ausente):
///   Solo crea sesión SSO — sin code ni redirect a app.
///   Se usa cuando el usuario accede directamente al dashboard del IdP.
/// </summary>
public record LoginCommand(
    string Email,
    string Password,
    string? ClientId, // opcional en modo directo
    string? RedirectUri, // opcional en modo directo
    string? CodeChallenge, // opcional en modo directo
    string? CodeChallengeMethod, // opcional en modo directo
    string? State,
    string? IpAddress,
    string? UserAgent
) : IRequest<Result<LoginResult>>;

public record LoginResult(
    string? AuthorizationCode, // null en modo directo
    string? RedirectUri, // null en modo directo
    string? State,
    string SessionId,
    bool IsDirect // true = modo directo → ir al dashboard
);
