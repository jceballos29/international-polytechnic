using Identity.Domain.Interfaces.Repositories;
using MediatR;

namespace Identity.Application.Auth.Queries.GetUserApps;

public class GetUserAppsQueryHandler : IRequestHandler<GetUserAppsQuery, GetUserAppsResult>
{
    private readonly IRoleRepository _roleRepo;
    private readonly IClientApplicationRepository _appRepo;

    public GetUserAppsQueryHandler(IRoleRepository roleRepo, IClientApplicationRepository appRepo)
    {
        _roleRepo = roleRepo;
        _appRepo = appRepo;
    }

    public async Task<GetUserAppsResult> Handle(
        GetUserAppsQuery query,
        CancellationToken cancellationToken
    )
    {
        // Obtener todas las asignaciones de roles del usuario
        // agrupadas por app
        var appIds = await GetUserAppIdsAsync(query.UserId, cancellationToken);

        var apps = new List<AppInfo>();

        foreach (var appId in appIds)
        {
            var app = await _appRepo.GetByIdAsync(appId, cancellationToken);
            if (app is null || !app.IsActive)
                continue;

            var roles = await _roleRepo.GetUserRolesInApplicationAsync(
                query.UserId,
                appId,
                includePermissions: false,
                ct: cancellationToken
            );

            apps.Add(
                new AppInfo(
                    ClientId: app.ClientId.Value,
                    Name: app.Name,
                    Description: app.Description,
                    LogoUrl: app.LogoUrl,
                    Roles: roles.Select(r => r.Name).ToList()
                )
            );
        }

        return new GetUserAppsResult(apps);
    }

    private async Task<IReadOnlyList<Guid>> GetUserAppIdsAsync(Guid userId, CancellationToken ct)
    {
        // Usamos el RoleRepository para obtener los ClientApplicationIds
        // donde el usuario tiene al menos un rol
        // Necesitamos agregar un método al repositorio
        return await _roleRepo.GetUserApplicationIdsAsync(userId, ct);
    }
}
