using MediatR;

namespace Identity.Application.Auth.Queries.GetUserApps;

/// <summary>
/// Retorna las aplicaciones donde el usuario tiene al menos un rol.
/// Se usa en el dashboard de identity-ui para mostrar
/// las apps habilitadas para el usuario autenticado.
/// </summary>
public record GetUserAppsQuery(Guid UserId) : IRequest<GetUserAppsResult>;

public record AppInfo(
    string ClientId,
    string Name,
    string? Description,
    string? LogoUrl,
    IReadOnlyList<string> Roles
);

public record GetUserAppsResult(IReadOnlyList<AppInfo> Apps);
