using Identity.Application.Common.Interfaces;
using Identity.Application.Common.Models;
using Identity.Domain.Entities;
using Identity.Domain.Enums;
using Identity.Domain.Interfaces;
using Identity.Domain.Interfaces.Repositories;
using Identity.Domain.ValueObjects;
using MediatR;

namespace Identity.Application.Auth.Commands.Login;

public class LoginCommandHandler : IRequestHandler<LoginCommand, Result<LoginResult>>
{
    private readonly IUserRepository _userRepo;
    private readonly IClientApplicationRepository _appRepo;
    private readonly IAuthorizationCodeRepository _codeRepo;
    private readonly IAuditLogRepository _auditRepo;
    private readonly IHashService _hashService;
    private readonly ISessionService _sessionService;
    private readonly IUnitOfWork _unitOfWork;

    public LoginCommandHandler(
        IUserRepository userRepo,
        IClientApplicationRepository appRepo,
        IAuthorizationCodeRepository codeRepo,
        IAuditLogRepository auditRepo,
        IHashService hashService,
        ISessionService sessionService,
        IUnitOfWork unitOfWork
    )
    {
        _userRepo = userRepo;
        _appRepo = appRepo;
        _codeRepo = codeRepo;
        _auditRepo = auditRepo;
        _hashService = hashService;
        _sessionService = sessionService;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<LoginResult>> Handle(
        LoginCommand command,
        CancellationToken cancellationToken
    )
    {
        // ── Paso 1: Validar la app (solo modo OAuth) ───────
        ClientApplication? app = null;

        if (!string.IsNullOrWhiteSpace(command.ClientId))
        {
            ClientId clientId;
            try
            {
                clientId = ClientId.Create(command.ClientId);
            }
            catch
            {
                return Result<LoginResult>.Failure(
                    "INVALID_CLIENT",
                    "client_id tiene un formato inválido."
                );
            }

            app = await _appRepo.GetByClientIdAsync(clientId, cancellationToken);

            if (app is null)
                return Result<LoginResult>.Failure(
                    "APPLICATION_NOT_FOUND",
                    $"La aplicación '{command.ClientId}' no está registrada."
                );

            if (!app.IsRedirectUriAllowed(command.RedirectUri!))
                return Result<LoginResult>.Failure(
                    "INVALID_REDIRECT_URI",
                    "La redirect_uri no está autorizada para esta aplicación."
                );
        }

        // ── Paso 2: Determinar el tenant ───────────────────
        // Modo OAuth → usar el tenant de la app
        // Modo directo → buscar por dominio "localhost"
        var tenantId =
            app?.TenantId
            ?? await _userRepo.GetTenantIdByDomainAsync("localhost", cancellationToken)
            ?? Guid.Empty;

        if (tenantId == Guid.Empty)
            return Result<LoginResult>.Failure(
                "INVALID_CREDENTIALS",
                "Las credenciales son incorrectas."
            );

        // ── Paso 3: Buscar el usuario ──────────────────────
        Email email;
        try
        {
            email = Email.Create(command.Email);
        }
        catch
        {
            await RegisterFailedLogin(
                tenantId,
                null,
                app?.Id,
                command.IpAddress,
                command.UserAgent,
                "invalid_email_format",
                cancellationToken
            );

            return Result<LoginResult>.Failure(
                "INVALID_CREDENTIALS",
                "Las credenciales son incorrectas."
            );
        }

        var user = await _userRepo.GetByEmailAsync(tenantId, email, cancellationToken);

        // ── Paso 4: Verificar cuenta ───────────────────────
        if (user is null || !user.IsActive)
        {
            await RegisterFailedLogin(
                tenantId,
                null,
                app?.Id,
                command.IpAddress,
                command.UserAgent,
                "user_not_found_or_inactive",
                cancellationToken
            );

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result<LoginResult>.Failure(
                "INVALID_CREDENTIALS",
                "Las credenciales son incorrectas."
            );
        }

        if (user.IsLocked())
        {
            var minutes = (int)Math.Ceiling(user.LockTimeRemaining()!.Value.TotalMinutes);

            await RegisterFailedLogin(
                tenantId,
                user.Id,
                app?.Id,
                command.IpAddress,
                command.UserAgent,
                "account_locked",
                cancellationToken
            );

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result<LoginResult>.Failure(
                "INVALID_CREDENTIALS",
                $"Cuenta bloqueada. Intenta de nuevo en {minutes} minuto(s)."
            );
        }

        // ── Paso 5: Verificar password ─────────────────────
        var passwordValid = _hashService.VerifyPassword(command.Password, user.PasswordHash);

        if (!passwordValid)
        {
            user.RegisterFailedLogin();
            await _userRepo.UpdateAsync(user, cancellationToken);

            await RegisterFailedLogin(
                tenantId,
                user.Id,
                app?.Id,
                command.IpAddress,
                command.UserAgent,
                "invalid_password",
                cancellationToken
            );

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result<LoginResult>.Failure(
                "INVALID_CREDENTIALS",
                "Las credenciales son incorrectas."
            );
        }

        // ── Paso 6: Login exitoso ──────────────────────────
        user.RegisterSuccessfulLogin();
        await _userRepo.UpdateAsync(user, cancellationToken);

        // ── Paso 7: Crear sesión SSO ───────────────────────
        var sessionId = await _sessionService.CreateSessionAsync(
            userId: user.Id,
            tenantId: tenantId,
            email: user.Email.Value,
            ct: cancellationToken
        );

        // ── Paso 8: ¿Modo directo o modo OAuth? ───────────
        var isDirect = string.IsNullOrWhiteSpace(command.ClientId);

        if (isDirect)
        {
            // Solo sesión SSO — sin authorization_code
            var directLog = AuditLog.Create(
                tenantId: tenantId,
                eventType: AuditEventType.UserLogin,
                success: true,
                userId: user.Id,
                ipAddress: command.IpAddress,
                userAgent: command.UserAgent,
                metadata: "{\"source\":\"direct\"}"
            );

            await _auditRepo.AddAsync(directLog, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result<LoginResult>.Success(
                new LoginResult(
                    AuthorizationCode: null,
                    RedirectUri: null,
                    State: null,
                    SessionId: sessionId,
                    IsDirect: true
                )
            );
        }

        // ── Paso 9: Emitir authorization_code (modo OAuth) ─
        var codeValue = GenerateSecureCode();
        var codeHash = _hashService.HashToken(codeValue);

        var authCode = AuthorizationCode.Create(
            userId: user.Id,
            clientApplicationId: app!.Id,
            codeHash: codeHash,
            redirectUri: command.RedirectUri!,
            scopes: ["openid", "profile", "email"],
            codeChallenge: command.CodeChallenge!,
            sessionId: sessionId,
            state: command.State
        );

        await _codeRepo.AddAsync(authCode, cancellationToken);

        var oauthLog = AuditLog.Create(
            tenantId: tenantId,
            eventType: AuditEventType.UserLogin,
            success: true,
            userId: user.Id,
            clientApplicationId: app.Id,
            ipAddress: command.IpAddress,
            userAgent: command.UserAgent,
            metadata: "{\"source\":\"oauth\"}"
        );

        await _auditRepo.AddAsync(oauthLog, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<LoginResult>.Success(
            new LoginResult(
                AuthorizationCode: codeValue,
                RedirectUri: command.RedirectUri,
                State: command.State,
                SessionId: sessionId,
                IsDirect: false
            )
        );
    }

    // ── Helpers privados ──────────────────────────────────

    private static string GenerateSecureCode()
    {
        var bytes = new byte[32];
        System.Security.Cryptography.RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes).Replace('+', '-').Replace('/', '_').TrimEnd('=');
    }

    private async Task RegisterFailedLogin(
        Guid tenantId,
        Guid? userId,
        Guid? clientApplicationId,
        string? ipAddress,
        string? userAgent,
        string reason,
        CancellationToken ct
    )
    {
        var log = AuditLog.Create(
            tenantId: tenantId,
            eventType: AuditEventType.UserLogin,
            success: false,
            userId: userId,
            clientApplicationId: clientApplicationId,
            ipAddress: ipAddress,
            userAgent: userAgent,
            metadata: $"{{\"reason\":\"{reason}\"}}"
        );

        await _auditRepo.AddAsync(log, ct);
    }
}
