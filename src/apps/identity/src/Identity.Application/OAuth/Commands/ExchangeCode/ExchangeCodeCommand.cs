using Identity.Application.Common.Models;
using MediatR;

namespace Identity.Application.OAuth.Commands.ExchangeCode;

/// <summary>
/// Intercambia un authorization_code por Access Token + Refresh Token.
///
/// Este es el paso 2 del flujo Authorization Code:
///   1. Usuario se autentica → IdP emite authorization_code
///   2. App envía el code + code_verifier → IdP emite tokens ← AQUÍ
///
/// Validaciones críticas:
///   - El code existe y no está expirado (max 2 min)
///   - El code no fue usado antes (un solo uso)
///   - El client_id y redirect_uri coinciden con los del paso 1
///   - PKCE: SHA256(code_verifier) == code_challenge guardado
/// </summary>
public record ExchangeCodeCommand(
    string Code,
    string CodeVerifier,
    string ClientId,
    string ClientSecret,
    string RedirectUri,
    string? IpAddress
) : IRequest<Result<TokenResult>>;
