using Identity.Application.Common.Models;
using Identity.Domain.Interfaces.Repositories;
using Identity.Domain.ValueObjects;
using MediatR;

namespace Identity.Application.Auth.Queries.GetUserInfo;

public class GetUserInfoQueryHandler : IRequestHandler<GetUserInfoQuery, Result<UserInfoResult>>
{
    private readonly IUserRepository _userRepo;
    private readonly IRoleRepository _roleRepo;
    private readonly IClientApplicationRepository _appRepo;

    public GetUserInfoQueryHandler(
        IUserRepository userRepo,
        IRoleRepository roleRepo,
        IClientApplicationRepository appRepo
    )
    {
        _userRepo = userRepo;
        _roleRepo = roleRepo;
        _appRepo = appRepo;
    }

    public async Task<Result<UserInfoResult>> Handle(
        GetUserInfoQuery query,
        CancellationToken cancellationToken
    )
    {
        var user = await _userRepo.GetByIdAsync(query.UserId, cancellationToken);

        if (user is null || !user.IsActive)
            return Result<UserInfoResult>.Failure(
                "USER_NOT_FOUND",
                "El usuario no existe o no está activo."
            );

        // Buscar la app por su ClientId público ("portal", "admin-panel", etc.)
        Guid clientApplicationId = Guid.Empty;

        if (!string.IsNullOrWhiteSpace(query.ClientId))
        {
            try
            {
                var clientId = ClientId.Create(query.ClientId);
                var app = await _appRepo.GetByClientIdAsync(clientId, cancellationToken);
                if (app is not null)
                    clientApplicationId = app.Id;
            }
            catch
            {
                // ClientId inválido → sin roles (token M2M probablemente)
            }
        }

        var roles = await _roleRepo.GetUserRolesInApplicationAsync(
            query.UserId,
            clientApplicationId,
            includePermissions: true,
            ct: cancellationToken
        );

        var roleNames = roles.Select(r => r.Name).ToList();
        var permissions = roles
            .SelectMany(r => r.Permissions)
            .Select(p => p.Name)
            .Distinct()
            .ToList();

        return Result<UserInfoResult>.Success(
            new UserInfoResult
            {
                Sub = user.Id.ToString(),
                Email = user.Email.Value,
                Name = user.Name?.FullName,
                GivenName = user.Name?.FirstName,
                FamilyName = user.Name?.LastNames,
                Initials = user.Initials,
                TenantId = user.TenantId.ToString(),
                Roles = roleNames,
                Permissions = permissions,
            }
        );
    }
}
