using Identity.Application.Common.Interfaces;
using Identity.Application.Common.Models;
using Identity.Domain.Entities;
using Identity.Domain.Enums;
using Identity.Domain.Interfaces;
using Identity.Domain.Interfaces.Repositories;
using Identity.Domain.ValueObjects;
using MediatR;

namespace Identity.Application.OAuth.Commands.RevokeToken;

public class RevokeTokenCommandHandler : IRequestHandler<RevokeTokenCommand, Result>
{
    private readonly IRefreshTokenRepository _refreshTokenRepo;
    private readonly IClientApplicationRepository _appRepo;
    private readonly IAuditLogRepository _auditRepo;
    private readonly IHashService _hashService;
    private readonly IJwtService _jwtService;
    private readonly ICacheService _cacheService;
    private readonly IUnitOfWork _unitOfWork;

    public RevokeTokenCommandHandler(
        IRefreshTokenRepository refreshTokenRepo,
        IClientApplicationRepository appRepo,
        IAuditLogRepository auditRepo,
        IHashService hashService,
        IJwtService jwtService,
        ICacheService cacheService,
        IUnitOfWork unitOfWork
    )
    {
        _refreshTokenRepo = refreshTokenRepo;
        _appRepo = appRepo;
        _auditRepo = auditRepo;
        _hashService = hashService;
        _jwtService = jwtService;
        _cacheService = cacheService;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(
        RevokeTokenCommand command,
        CancellationToken cancellationToken
    )
    {
        // Verificar la app — siempre requerido
        var clientId = ClientId.Create(command.ClientId);
        var app = await _appRepo.GetByClientIdAsync(clientId, cancellationToken);

        if (app is null)
            return Result.Failure("INVALID_CLIENT", "La aplicación no está registrada.");

        // RFC 7009: la revocación siempre retorna 200
        // aunque el token no exista — no revelamos si existía
        var isRefreshToken = command.TokenTypeHint != "access_token";

        if (isRefreshToken)
            await RevokeRefreshToken(command, app.Id, app.TenantId, cancellationToken);
        else
            await RevokeAccessToken(command, app.Id, app.TenantId, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    private async Task RevokeRefreshToken(
        RevokeTokenCommand command,
        Guid appId,
        Guid tenantId,
        CancellationToken ct
    )
    {
        var tokenHash = _hashService.HashToken(command.Token);
        var token = await _refreshTokenRepo.GetByTokenHashAsync(tokenHash, ct);

        if (token is null || !token.IsActive())
            return;

        token.RevokeWithoutReplacement();
        await _refreshTokenRepo.UpdateAsync(token, ct);

        var log = AuditLog.Create(
            tenantId: tenantId,
            eventType: AuditEventType.TokenRevoked,
            success: true,
            userId: token.UserId,
            clientApplicationId: appId,
            metadata: "{\"token_type\":\"refresh_token\"}"
        );

        await _auditRepo.AddAsync(log, ct);
    }

    private async Task RevokeAccessToken(
        RevokeTokenCommand command,
        Guid appId,
        Guid tenantId,
        CancellationToken ct
    )
    {
        // Extraer el jti del JWT para agregarlo a la blocklist
        var jti = _jwtService.ExtractJti(command.Token);
        if (string.IsNullOrWhiteSpace(jti))
            return;

        // TTL = 15 minutos (vida máxima del access token)
        // Después de eso el token ya expiró y no necesita blocklist
        await _cacheService.SetAsync(
            key: $"blocklist:{jti}",
            value: "revoked",
            ttl: TimeSpan.FromMinutes(15),
            ct: ct
        );

        var log = AuditLog.Create(
            tenantId: tenantId,
            eventType: AuditEventType.TokenRevoked,
            success: true,
            clientApplicationId: appId,
            metadata: "{\"token_type\":\"access_token\"}"
        );

        await _auditRepo.AddAsync(log, ct);
    }
}
