using Identity.Application.Common.Interfaces;
using Identity.Application.Common.Models;
using Identity.Domain.Entities;
using Identity.Domain.Enums;
using Identity.Domain.Interfaces;
using Identity.Domain.Interfaces.Repositories;
using Identity.Domain.ValueObjects;
using MediatR;
using RefreshTokenEntity = Identity.Domain.Entities.RefreshToken;

namespace Identity.Application.OAuth.Commands.ExchangeCode;

public class ExchangeCodeCommandHandler : IRequestHandler<ExchangeCodeCommand, Result<TokenResult>>
{
    private readonly IAuthorizationCodeRepository _codeRepo;
    private readonly IClientApplicationRepository _appRepo;
    private readonly IUserRepository _userRepo;
    private readonly IRoleRepository _roleRepo;
    private readonly IRefreshTokenRepository _refreshTokenRepo;
    private readonly IAuditLogRepository _auditRepo;
    private readonly IHashService _hashService;
    private readonly IPkceService _pkceService;
    private readonly IJwtService _jwtService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly int _refreshTokenExpiryDays;

    public ExchangeCodeCommandHandler(
        IAuthorizationCodeRepository codeRepo,
        IClientApplicationRepository appRepo,
        IUserRepository userRepo,
        IRoleRepository roleRepo,
        IRefreshTokenRepository refreshTokenRepo,
        IAuditLogRepository auditRepo,
        IHashService hashService,
        IPkceService pkceService,
        IJwtService jwtService,
        IUnitOfWork unitOfWork,
        JwtSettings jwtSettings
    )
    {
        _codeRepo = codeRepo;
        _appRepo = appRepo;
        _userRepo = userRepo;
        _roleRepo = roleRepo;
        _refreshTokenRepo = refreshTokenRepo;
        _auditRepo = auditRepo;
        _hashService = hashService;
        _pkceService = pkceService;
        _jwtService = jwtService;
        _unitOfWork = unitOfWork;
        _refreshTokenExpiryDays = jwtSettings.RefreshTokenExpiryDays;
    }

    public async Task<Result<TokenResult>> Handle(
        ExchangeCodeCommand command,
        CancellationToken cancellationToken
    )
    {
        // ── Paso 1: Buscar el authorization_code ───────────
        var codeHash = _hashService.HashToken(command.Code);
        var authCode = await _codeRepo.GetByCodeHashAsync(codeHash, cancellationToken);

        if (authCode is null || !authCode.IsValid())
            return Result<TokenResult>.Failure(
                "INVALID_GRANT",
                "El authorization_code es inválido o ha expirado."
            );

        // ── Paso 2: Verificar client_id y redirect_uri ─────
        var clientId = ClientId.Create(command.ClientId);
        var app = await _appRepo.GetByClientIdAsync(clientId, cancellationToken);

        if (app is null)
            return Result<TokenResult>.Failure(
                "APPLICATION_NOT_FOUND",
                "La aplicación no está registrada."
            );

        // Verificar que el code pertenece a esta app
        if (authCode.ClientApplicationId != app.Id)
            return Result<TokenResult>.Failure(
                "INVALID_GRANT",
                "El code no corresponde a esta aplicación."
            );

        // Verificar redirect_uri — debe ser exactamente igual
        if (authCode.RedirectUri != command.RedirectUri)
            return Result<TokenResult>.Failure(
                "INVALID_GRANT",
                "La redirect_uri no coincide con la del authorization request."
            );

        // Verificar client_secret (apps confidenciales)
        if (app.ClientSecretHash is not null)
        {
            if (
                string.IsNullOrWhiteSpace(command.ClientSecret)
                || !_hashService.VerifyPassword(command.ClientSecret, app.ClientSecretHash)
            )
                return Result<TokenResult>.Failure(
                    "INVALID_CLIENT",
                    "El client_secret es incorrecto."
                );
        }

        // ── Paso 3: Verificar PKCE ─────────────────────────
        var pkceValid = _pkceService.ValidateCodeChallenge(
            command.CodeVerifier,
            authCode.CodeChallenge,
            authCode.CodeChallengeMethod
        );

        if (!pkceValid)
            return Result<TokenResult>.Failure(
                "PKCE_VERIFICATION_FAILED",
                "La verificación PKCE falló."
            );

        // ── Paso 4: Marcar el code como usado (atómico) ────
        var marked = await _codeRepo.MarkAsUsedAsync(authCode.Id, cancellationToken);

        if (!marked)
            return Result<TokenResult>.Failure(
                "CODE_ALREADY_USED",
                "El authorization_code ya fue utilizado."
            );

        // ── Paso 5: Cargar usuario y sus roles ─────────────
        var user = await _userRepo.GetByIdAsync(authCode.UserId, cancellationToken);

        if (user is null || !user.IsActive)
            return Result<TokenResult>.Failure(
                "INVALID_GRANT",
                "El usuario no existe o no está activo."
            );

        var roles = await _roleRepo.GetUserRolesInApplicationAsync(
            user.Id,
            app.Id,
            includePermissions: true,
            ct: cancellationToken
        );

        var roleNames = roles.Select(r => r.Name).ToList();
        var permissions = roles
            .SelectMany(r => r.Permissions)
            .Select(p => p.Name)
            .Distinct()
            .ToList();

        // ── Paso 6: Emitir tokens ──────────────────────────
        var accessToken = _jwtService.GenerateAccessToken(user, app, roleNames, permissions, authCode.SessionId);

        var refreshTokenValue = _jwtService.GenerateRefreshToken();
        var refreshTokenHash = _hashService.HashToken(refreshTokenValue);

        var refreshToken = RefreshTokenEntity.Create(
            userId: user.Id,
            clientApplicationId: app.Id,
            tokenHash: refreshTokenHash,
            scopes: authCode.Scopes,
            sessionId: authCode.SessionId,
            expiryDays: _refreshTokenExpiryDays,
            issuedFromIp: command.IpAddress
        );

        await _refreshTokenRepo.AddAsync(refreshToken, cancellationToken);

        // ── Paso 7: Audit log ──────────────────────────────
        var log = AuditLog.Create(
            tenantId: app.TenantId,
            eventType: AuditEventType.TokenIssued,
            success: true,
            userId: user.Id,
            clientApplicationId: app.Id,
            ipAddress: command.IpAddress,
            metadata: $"{{\"grant_type\":\"authorization_code\"}}"
        );

        await _auditRepo.AddAsync(log, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var expiryMinutes = 15;
        return Result<TokenResult>.Success(
            new TokenResult
            {
                AccessToken = accessToken,
                RefreshToken = refreshTokenValue,
                TokenType = "Bearer",
                ExpiresIn = expiryMinutes * 60,
                Scope = string.Join(" ", authCode.Scopes),
            }
        );
    }
}
