using Identity.Application.Auth.Commands.Login;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;

namespace Identity.API.Controllers;

/// <summary>
/// Endpoints internos de autenticación.
/// Usados por identity-ui — no son endpoints públicos OAuth.
///
/// POST /auth/login → procesa credenciales y retorna authorization_code
/// </summary>
[ApiController]
[Route("auth")]
public class AuthController : ControllerBase
{
    private readonly IMediator _mediator;

    public AuthController(IMediator mediator) => _mediator = mediator;

    /// <summary>
    /// Autentica al usuario y retorna un authorization_code.
    /// identity-ui llama a este endpoint cuando el usuario
    /// envía el formulario de login.
    /// </summary>
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        var userAgent = Request.Headers.UserAgent.ToString();

        var command = new LoginCommand(
            Email: request.Email,
            Password: request.Password,
            ClientId: request.ClientId,
            RedirectUri: request.RedirectUri,
            CodeChallenge: request.CodeChallenge,
            CodeChallengeMethod: request.CodeChallengeMethod,
            State: request.State,
            IpAddress: ip,
            UserAgent: userAgent
        );

        var result = await _mediator.Send(command);

        if (!result.IsSuccess)
            return BadRequest(
                new { error = result.ErrorCode, error_description = result.ErrorMessage }
            );

        // Escribir cookie SSO siempre — ambos modos la necesitan
        Response.Cookies.Append(
            "idp_session",
            result.Value!.SessionId,
            new CookieOptions
            {
                HttpOnly = true,
                SameSite = SameSiteMode.Lax,
                Secure = !HttpContext
                    .RequestServices.GetRequiredService<IHostEnvironment>()
                    .IsDevelopment(),
                MaxAge = TimeSpan.FromHours(24),
                Path = "/",
            }
        );

        // Modo directo → el frontend redirige al dashboard
        if (result.Value.IsDirect)
            return Ok(new { is_direct = true });

        // Modo OAuth → retorna el code para redirigir a la app
        return Ok(
            new
            {
                is_direct = false,
                code = result.Value.AuthorizationCode,
                redirect_uri = result.Value.RedirectUri,
                state = result.Value.State,
            }
        );
    }
}

/// <summary>Request body del endpoint POST /auth/login.</summary>
public record LoginRequest(
    string Email,
    string Password,
    string? ClientId,
    string? RedirectUri,
    string? CodeChallenge,
    string? CodeChallengeMethod,
    string? State
);
