using Identity.Application.Common.Models;
using MediatR;

namespace Identity.Application.OAuth.Commands.RevokeToken;

/// <summary>
/// Revoca un token — logout explícito o invalidación de seguridad.
///
/// Acepta dos tipos de token:
///
/// RefreshToken → se marca como revocado en PostgreSQL
///   El Access Token asociado sigue válido hasta expirar (máx 15 min)
///   pero el usuario no podrá renovarlo.
///
/// AccessToken → su jti se agrega a la blocklist en Redis
///   TTL = tiempo restante hasta que expire el token
///   Las APIs verifican la blocklist en cada request.
///   Después del TTL se elimina automáticamente de Redis.
/// </summary>
public record RevokeTokenCommand(
    string Token,
    string TokenTypeHint,
    string ClientId,
    string? ClientSecret
) : IRequest<Result>;
