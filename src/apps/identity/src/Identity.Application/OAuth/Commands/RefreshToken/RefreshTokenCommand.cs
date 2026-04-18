using Identity.Application.Common.Models;
using MediatR;

namespace Identity.Application.OAuth.Commands.RefreshToken;

/// <summary>
/// Renueva un Access Token usando el Refresh Token.
///
/// Rotación de Refresh Tokens:
///   El Refresh Token viejo se invalida inmediatamente.
///   Se emite un Refresh Token nuevo.
///   Si alguien roba el token viejo e intenta usarlo después
///   de que el usuario legítimo ya lo usó → falla → alerta.
/// </summary>
public record RefreshTokenCommand(
    string RefreshToken,
    string ClientId,
    string ClientSecret,
    string? IpAddress
) : IRequest<Result<TokenResult>>;
