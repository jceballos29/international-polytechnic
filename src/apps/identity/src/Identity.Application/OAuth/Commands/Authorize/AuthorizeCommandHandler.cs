using Identity.Application.Common.Interfaces;
using Identity.Application.Common.Models;
using Identity.Domain.Entities;
using Identity.Domain.Interfaces;
using Identity.Domain.Interfaces.Repositories;
using Identity.Domain.ValueObjects;
using MediatR;

namespace Identity.Application.OAuth.Commands.Authorize;

public class AuthorizeCommandHandler : IRequestHandler<AuthorizeCommand, Result<AuthorizeResult>>
{
    private readonly IClientApplicationRepository _appRepo;
    private readonly IUserRepository _userRepo;
    private readonly IRoleRepository _roleRepo;
    private readonly IAuthorizationCodeRepository _codeRepo;
    private readonly IAuditLogRepository _auditRepo;
    private readonly ISessionService _sessionService;
    private readonly IHashService _hashService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly string _identityUiBaseUrl;

    public AuthorizeCommandHandler(
        IClientApplicationRepository appRepo,
        IUserRepository userRepo,
        IRoleRepository roleRepo,
        IAuthorizationCodeRepository codeRepo,
        IAuditLogRepository auditRepo,
        ISessionService sessionService,
        IHashService hashService,
        IUnitOfWork unitOfWork,
        JwtSettings jwtSettings
    )
    {
        _appRepo = appRepo;
        _userRepo = userRepo;
        _roleRepo = roleRepo;
        _codeRepo = codeRepo;
        _auditRepo = auditRepo;
        _sessionService = sessionService;
        _hashService = hashService;
        _unitOfWork = unitOfWork;
        _identityUiBaseUrl = "http://localhost:3000";
    }

    public async Task<Result<AuthorizeResult>> Handle(
        AuthorizeCommand command,
        CancellationToken cancellationToken
    )
    {
        // ── Paso 1: Validar formato del client_id ──────────
        ClientId clientId;
        try
        {
            clientId = ClientId.Create(command.ClientId);
        }
        catch
        {
            return Result<AuthorizeResult>.Failure(
                "INVALID_CLIENT",
                "client_id tiene un formato inválido."
            );
        }

        // ── Paso 2: Verificar la app ───────────────────────
        var app = await _appRepo.GetByClientIdAsync(clientId, cancellationToken);

        if (app is null)
            return Result<AuthorizeResult>.Failure(
                "INVALID_CLIENT",
                $"La aplicación '{command.ClientId}' no está registrada."
            );

        if (!app.IsRedirectUriAllowed(command.RedirectUri))
            return Result<AuthorizeResult>.Failure(
                "INVALID_REDIRECT_URI",
                "La redirect_uri no está autorizada."
            );

        if (!app.IsGrantTypeAllowed("authorization_code"))
            return Result<AuthorizeResult>.Failure(
                "UNAUTHORIZED_CLIENT",
                "Esta aplicación no permite el flujo authorization_code."
            );

        // ── Paso 3: Verificar sesión SSO ───────────────────
        if (!string.IsNullOrWhiteSpace(command.SessionId))
        {
            var session = await _sessionService.GetSessionAsync(
                command.SessionId,
                cancellationToken
            );

            if (session is not null)
            {
                // ✅ Sesión válida — SSO automático sin mostrar login
                var ssoResult = await IssueSsoCode(session.UserId, app, command, cancellationToken);

                if (ssoResult is not null)
                    return Result<AuthorizeResult>.Success(
                        new AuthorizeResult(ssoResult, RequiresLogin: false)
                    );
            }
        }

        // ── Paso 4: Sin sesión → redirigir al login ────────
        var loginUrl = BuildLoginUrl(command);
        return Result<AuthorizeResult>.Success(new AuthorizeResult(loginUrl, RequiresLogin: true));
    }

    /// <summary>
    /// Emite un authorization_code para el usuario con sesión activa.
    /// Retorna la URL de callback con el code ya incluido.
    /// </summary>
    private async Task<string?> IssueSsoCode(
        Guid userId,
        ClientApplication app,
        AuthorizeCommand command,
        CancellationToken ct
    )
    {
        var user = await _userRepo.GetByIdAsync(userId, ct);
        if (user is null || !user.IsActive)
            return null;

        var codeValue = GenerateSecureCode();
        var codeHash = _hashService.HashToken(codeValue);

        var authCode = AuthorizationCode.Create(
            userId: user.Id,
            clientApplicationId: app.Id,
            codeHash: codeHash,
            redirectUri: command.RedirectUri,
            scopes: ["openid", "profile", "email"],
            codeChallenge: command.CodeChallenge,
            sessionId: command.SessionId!,
            state: command.State
        );

        await _codeRepo.AddAsync(authCode, ct);

        var log = AuditLog.Create(
            tenantId: app.TenantId,
            eventType: Domain.Enums.AuditEventType.AuthCodeIssued,
            success: true,
            userId: user.Id,
            clientApplicationId: app.Id,
            metadata: "{\"source\":\"sso\"}"
        );

        await _auditRepo.AddAsync(log, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        // Construir URL de callback con el code
        var callbackUri = new UriBuilder(command.RedirectUri);
        var separator = string.IsNullOrEmpty(callbackUri.Query) ? "?" : "&";
        callbackUri.Query = callbackUri.Query.TrimStart('?');

        var queryParams = $"code={Uri.EscapeDataString(codeValue)}";
        if (command.State is not null)
            queryParams += $"&state={Uri.EscapeDataString(command.State)}";

        return $"{callbackUri.Uri.GetLeftPart(UriPartial.Path)}?{queryParams}";
    }

    private string BuildLoginUrl(AuthorizeCommand command)
    {
        var url =
            $"{_identityUiBaseUrl}/login"
            + $"?client_id={Uri.EscapeDataString(command.ClientId)}"
            + $"&redirect_uri={Uri.EscapeDataString(command.RedirectUri)}"
            + $"&code_challenge={Uri.EscapeDataString(command.CodeChallenge)}"
            + $"&code_challenge_method={Uri.EscapeDataString(command.CodeChallengeMethod)}";

        if (!string.IsNullOrWhiteSpace(command.State))
            url += $"&state={Uri.EscapeDataString(command.State)}";

        return url;
    }

    private static string GenerateSecureCode()
    {
        var bytes = new byte[32];
        System.Security.Cryptography.RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes).Replace('+', '-').Replace('/', '_').TrimEnd('=');
    }
}
