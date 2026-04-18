using Identity.Application.Common.Models;
using MediatR;

namespace Identity.Application.OAuth.Commands.ClientCredentials;

/// <summary>
/// Emite un Access Token para comunicación entre servicios (M2M).
///
/// No hay usuario involucrado — el token representa a la app.
/// Usado por gradus-api para llamar a universitas-api.
///
/// El token resultante:
///   - No tiene claim "sub" de usuario
///   - El "sub" es el client_id de la app
///   - Incluye los scopes solicitados
///   - No tiene refresh_token (se solicita uno nuevo cuando expira)
/// </summary>
public record ClientCredentialsCommand(
    string ClientId,
    string ClientSecret,
    string Scope,
    string? IpAddress
) : IRequest<Result<TokenResult>>;
