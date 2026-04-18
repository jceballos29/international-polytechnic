using System.Security.Claims;
using Identity.Application.Auth.Queries.GetUserInfo;
using Identity.Application.Common.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Identity.API.Controllers;

/// <summary>
/// Endpoint OIDC para obtener información del usuario autenticado.
/// Requiere Access Token válido en el header Authorization: Bearer.
/// </summary>
[ApiController]
[Route("oauth")]
[Authorize]
public class UserInfoController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ISessionService _sessionService;

    public UserInfoController(IMediator mediator, ISessionService sessionService)
    {
        _mediator = mediator;
        _sessionService = sessionService;
    }

    /// <summary>
    /// Retorna información del usuario autenticado.
    ///
    /// El Access Token debe incluir el claim "client_id"
    /// para saber en qué contexto de app cargar los roles.
    /// </summary>
    [HttpGet("userinfo")]
    public async Task<IActionResult> GetUserInfo()
    {
        var sidClaim = User.FindFirst("sid")?.Value;

        if (!string.IsNullOrWhiteSpace(sidClaim))
        {
            var session = await _sessionService.GetSessionAsync(sidClaim);
            if (session is null)
            {
                return Unauthorized(
                    new
                    {
                        error = "SESSION_EXPIRED",
                        error_description = "La sesión global SSO ha caducado o fue cerrada.",
                    }
                );
            }
        }

        var subClaim =
            User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;

        // client_id es el string público ("portal"), no el UUID interno
        var clientIdString = User.FindFirst("client_id")?.Value ?? User.FindFirst("aud")?.Value;

        if (string.IsNullOrWhiteSpace(subClaim) || !Guid.TryParse(subClaim, out var userId))
            return Unauthorized(
                new
                {
                    error = "INVALID_TOKEN",
                    error_description = "El token no contiene un subject válido.",
                }
            );

        var query = new GetUserInfoQuery(userId, clientIdString ?? string.Empty);
        var result = await _mediator.Send(query);

        if (!result.IsSuccess)
            return NotFound(
                new { error = result.ErrorCode, error_description = result.ErrorMessage }
            );

        var user = result.Value!;

        return Ok(
            new
            {
                sub = user.Sub,
                email = user.Email,
                name = user.Name,
                given_name = user.GivenName,
                family_name = user.FamilyName,
                initials = user.Initials,
                tenant_id = user.TenantId,
                roles = user.Roles,
                permissions = user.Permissions,
            }
        );
    }
}
