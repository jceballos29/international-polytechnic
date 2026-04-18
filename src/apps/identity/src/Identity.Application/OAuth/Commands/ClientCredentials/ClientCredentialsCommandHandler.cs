using Identity.Application.Common.Interfaces;
using Identity.Application.Common.Models;
using Identity.Domain.Entities;
using Identity.Domain.Enums;
using Identity.Domain.Interfaces;
using Identity.Domain.Interfaces.Repositories;
using Identity.Domain.ValueObjects;
using MediatR;

namespace Identity.Application.OAuth.Commands.ClientCredentials;

public class ClientCredentialsCommandHandler
    : IRequestHandler<ClientCredentialsCommand, Result<TokenResult>>
{
    private readonly IClientApplicationRepository _appRepo;
    private readonly IAuditLogRepository _auditRepo;
    private readonly IHashService _hashService;
    private readonly IJwtService _jwtService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly JwtSettings _jwtSettings;

    public ClientCredentialsCommandHandler(
        IClientApplicationRepository appRepo,
        IAuditLogRepository auditRepo,
        IHashService hashService,
        IJwtService jwtService,
        IUnitOfWork unitOfWork,
        JwtSettings jwtSettings
    )
    {
        _appRepo = appRepo;
        _auditRepo = auditRepo;
        _hashService = hashService;
        _jwtService = jwtService;
        _unitOfWork = unitOfWork;
        _jwtSettings = jwtSettings;
    }

    public async Task<Result<TokenResult>> Handle(
        ClientCredentialsCommand command,
        CancellationToken cancellationToken
    )
    {
        // ── Paso 1: Verificar la app ───────────────────────
        var clientId = ClientId.Create(command.ClientId);
        var app = await _appRepo.GetByClientIdAsync(clientId, cancellationToken);

        if (app is null)
            return Result<TokenResult>.Failure(
                "INVALID_CLIENT",
                "La aplicación no está registrada o no está activa."
            );

        // ── Paso 2: Verificar que soporta client_credentials
        if (!app.IsGrantTypeAllowed("client_credentials"))
            return Result<TokenResult>.Failure(
                "UNAUTHORIZED_CLIENT",
                "Esta aplicación no tiene permitido el flujo client_credentials."
            );

        // ── Paso 3: Verificar client_secret ───────────────
        if (
            app.ClientSecretHash is null
            || !_hashService.VerifyPassword(command.ClientSecret, app.ClientSecretHash)
        )
            return Result<TokenResult>.Failure("INVALID_CLIENT", "El client_secret es incorrecto.");

        // ── Paso 4: Verificar scopes ───────────────────────
        var requestedScopes = command
            .Scope.Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .ToList();

        if (!app.AreScopesAllowed(requestedScopes))
            return Result<TokenResult>.Failure(
                "INVALID_SCOPE",
                "Uno o más scopes solicitados no están permitidos."
            );

        // ── Paso 5: Emitir Access Token M2M ───────────────
        // Sin refresh token — el cliente solicita uno nuevo cuando expira
        var accessToken = _jwtService.GenerateM2MAccessToken(app, requestedScopes);

        // ── Paso 6: Audit log ──────────────────────────────
        var log = AuditLog.Create(
            tenantId: app.TenantId,
            eventType: AuditEventType.M2MTokenIssued,
            success: true,
            clientApplicationId: app.Id,
            ipAddress: command.IpAddress,
            metadata: $"{{\"scopes\":\"{command.Scope}\"}}"
        );

        await _auditRepo.AddAsync(log, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<TokenResult>.Success(
            new TokenResult
            {
                AccessToken = accessToken,
                RefreshToken = string.Empty,
                // M2M no tiene refresh token
                TokenType = "Bearer",
                ExpiresIn = _jwtSettings.AccessTokenExpiryMinutes * 60,
                Scope = command.Scope,
            }
        );
    }
}
