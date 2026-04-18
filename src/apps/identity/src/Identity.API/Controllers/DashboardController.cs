using System.Security.Claims;
using Identity.Application.Auth.Queries.GetUserApps;
using Identity.Application.Common.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Identity.API.Controllers;

/// <summary>
/// Endpoints para el dashboard de identity-ui.
/// Requieren sesión SSO válida (cookie idp_session).
/// </summary>
[ApiController]
[Route("dashboard")]
public class DashboardController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ISessionService _sessionService;

    public DashboardController(IMediator mediator, ISessionService sessionService)
    {
        _mediator = mediator;
        _sessionService = sessionService;
    }

    /// <summary>
    /// Retorna las apps habilitadas para el usuario con sesión activa.
    /// identity-ui llama a este endpoint al mostrar el dashboard.
    /// </summary>
    [HttpGet("apps")]
    public async Task<IActionResult> GetUserApps()
    {
        // Leer la cookie idp_session
        var sessionId = Request.Cookies["idp_session"];

        if (string.IsNullOrWhiteSpace(sessionId))
            return Unauthorized(
                new { error = "NO_SESSION", error_description = "No hay sesión activa." }
            );

        // Verificar la sesión en Redis
        var session = await _sessionService.GetSessionAsync(sessionId);

        if (session is null)
            return Unauthorized(
                new
                {
                    error = "SESSION_EXPIRED",
                    error_description = "La sesión ha expirado. Inicia sesión de nuevo.",
                }
            );

        var result = await _mediator.Send(new GetUserAppsQuery(session.UserId));

        return Ok(
            new
            {
                user = new { email = session.Email, user_id = session.UserId },
                apps = result.Apps.Select(a => new
                {
                    client_id = a.ClientId,
                    name = a.Name,
                    description = a.Description,
                    logo_url = a.LogoUrl,
                    roles = a.Roles,
                }),
            }
        );
    }

    /// <summary>
    /// Cierra la sesión SSO — logout del IdP.
    /// Después de esto el usuario necesita autenticarse de nuevo
    /// en cualquier app del ecosistema.
    /// </summary>
    [AcceptVerbs("GET", "POST")]
    [Route("logout")]
    public async Task<IActionResult> Logout([FromQuery] string? post_logout_redirect_uri)
    {
        var sessionId = Request.Cookies["idp_session"];

        if (!string.IsNullOrWhiteSpace(sessionId))
            await _sessionService.DestroySessionAsync(sessionId);

        Response.Cookies.Delete(
            "idp_session",
            new CookieOptions { Path = "/", SameSite = SameSiteMode.Lax }
        );

        // Si viene redirect_uri → redirigir después del logout
        if (!string.IsNullOrWhiteSpace(post_logout_redirect_uri))
            return Redirect(post_logout_redirect_uri);

        return Ok(new { message = "Sesión cerrada correctamente." });
    }
}
