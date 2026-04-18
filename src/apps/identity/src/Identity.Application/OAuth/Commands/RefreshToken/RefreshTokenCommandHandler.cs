using Identity.Application.Common.Interfaces;
using Identity.Application.Common.Models;
using Identity.Domain.Entities;
using Identity.Domain.Enums;
using Identity.Domain.Interfaces;
using Identity.Domain.Interfaces.Repositories;
using Identity.Domain.ValueObjects;
using MediatR;
using RefreshTokenEntity = Identity.Domain.Entities.RefreshToken;

namespace Identity.Application.OAuth.Commands.RefreshToken;

public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, Result<TokenResult>>
{
    private readonly IRefreshTokenRepository _refreshTokenRepo;
    private readonly IClientApplicationRepository _appRepo;
    private readonly IUserRepository _userRepo;
    private readonly IRoleRepository _roleRepo;
    private readonly IAuditLogRepository _auditRepo;
    private readonly IHashService _hashService;
    private readonly IJwtService _jwtService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ISessionService _sessionService;
    private readonly JwtSettings _jwtSettings;

    public RefreshTokenCommandHandler(
        IRefreshTokenRepository refreshTokenRepo,
        IClientApplicationRepository appRepo,
        IUserRepository userRepo,
        IRoleRepository roleRepo,
        IAuditLogRepository auditRepo,
        IHashService hashService,
        IJwtService jwtService,
        ISessionService sessionService,
        IUnitOfWork unitOfWork,
        JwtSettings jwtSettings
    )
    {
        _refreshTokenRepo = refreshTokenRepo;
        _appRepo = appRepo;
        _userRepo = userRepo;
        _roleRepo = roleRepo;
        _auditRepo = auditRepo;
        _hashService = hashService;
        _jwtService = jwtService;
        _sessionService = sessionService;
        _unitOfWork = unitOfWork;
        _jwtSettings = jwtSettings;
    }

    public async Task<Result<TokenResult>> Handle(
        RefreshTokenCommand command,
        CancellationToken cancellationToken
    )
    {
        // ── Paso 1: Buscar el refresh token ────────────────
        var tokenHash = _hashService.HashToken(command.RefreshToken);
        var refreshToken = await _refreshTokenRepo.GetByTokenHashAsync(
            tokenHash,
            cancellationToken
        );

        if (refreshToken is null || !refreshToken.IsActive())
            return Result<TokenResult>.Failure(
                "INVALID_GRANT",
                "El refresh_token es inválido, ha expirado o fue revocado."
            );

        var session = await _sessionService.GetSessionAsync(refreshToken.SessionId, cancellationToken);
        if (session is null)
            return Result<TokenResult>.Failure(
                "INVALID_GRANT",
                "La sesión SSO ha expirado o ha sido cerrada."
            );

        // ── Paso 2: Verificar la app ───────────────────────
        var clientId = ClientId.Create(command.ClientId);
        var app = await _appRepo.GetByClientIdAsync(clientId, cancellationToken);

        if (app is null)
            return Result<TokenResult>.Failure(
                "APPLICATION_NOT_FOUND",
                "La aplicación no está registrada."
            );

        // El token debe pertenecer a esta app
        if (refreshToken.ClientApplicationId != app.Id)
            return Result<TokenResult>.Failure(
                "INVALID_GRANT",
                "El token no pertenece a esta aplicación."
            );

        // Verificar client_secret
        if (app.ClientSecretHash is not null && !string.IsNullOrWhiteSpace(command.ClientSecret))
        {
            if (!_hashService.VerifyPassword(command.ClientSecret, app.ClientSecretHash))
                return Result<TokenResult>.Failure(
                    "INVALID_CLIENT",
                    "El client_secret es incorrecto."
                );
        }

        // ── Paso 3: Verificar usuario ──────────────────────
        var user = await _userRepo.GetByIdAsync(refreshToken.UserId, cancellationToken);

        if (user is null || !user.IsActive)
            return Result<TokenResult>.Failure(
                "INVALID_GRANT",
                "El usuario no existe o no está activo."
            );

        // ── Paso 4: Rotar el Refresh Token ─────────────────
        // Nuevo refresh token
        var newRefreshTokenValue = _jwtService.GenerateRefreshToken();
        var newRefreshTokenHash = _hashService.HashToken(newRefreshTokenValue);

        var newRefreshToken = RefreshTokenEntity.Create(
            userId: user.Id,
            clientApplicationId: app.Id,
            tokenHash: newRefreshTokenHash,
            scopes: refreshToken.Scopes,
            sessionId: refreshToken.SessionId,
            expiryDays: _jwtSettings.RefreshTokenExpiryDays,
            issuedFromIp: command.IpAddress
        );

        await _refreshTokenRepo.AddAsync(newRefreshToken, cancellationToken);

        // Revocar el token anterior — registra quién lo reemplazó
        refreshToken.Revoke(newRefreshToken.Id);
        await _refreshTokenRepo.UpdateAsync(refreshToken, cancellationToken);

        // ── Paso 5: Emitir nuevo Access Token ──────────────
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

        var accessToken = _jwtService.GenerateAccessToken(user, app, roleNames, permissions, refreshToken.SessionId);

        // ── Paso 6: Audit log ──────────────────────────────
        var log = AuditLog.Create(
            tenantId: app.TenantId,
            eventType: AuditEventType.TokenRefreshed,
            success: true,
            userId: user.Id,
            clientApplicationId: app.Id,
            ipAddress: command.IpAddress
        );

        await _auditRepo.AddAsync(log, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<TokenResult>.Success(
            new TokenResult
            {
                AccessToken = accessToken,
                RefreshToken = newRefreshTokenValue,
                TokenType = "Bearer",
                ExpiresIn = _jwtSettings.AccessTokenExpiryMinutes * 60,
                Scope = string.Join(" ", refreshToken.Scopes),
            }
        );
    }
}
