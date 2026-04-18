using Identity.Domain.Entities;
using Identity.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Identity.Infrastructure.Persistence;

public class DataSeeder
{
    private readonly IdentityDbContext _context;
    private readonly ILogger<DataSeeder> _logger;

    public const string DefaultTenantDomain = "localhost";
    public const string SuperAdminEmail = "admin@localhost.com";
    public const string SuperAdminPassword = "Admin1234!";
    public const string AdminPanelClientId = "admin-panel";
    public const string PortalClientId = "portal";

    public DataSeeder(IdentityDbContext context, ILogger<DataSeeder> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task SeedAsync()
    {
        var strategy = _context.Database.CreateExecutionStrategy();

        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var tenant = await SeedTenantAsync();
                var (adminApp, portalApp) = await SeedApplicationsAsync(tenant);

                // Roles en admin-panel
                var (adminSuperAdmin, _) = await SeedAdminRolesAsync(tenant, adminApp);

                // Roles en portal
                var portalSuperAdmin = await SeedPortalRolesAsync(tenant, portalApp);

                var adminUser = await SeedAdminUserAsync(tenant);

                // Asignar super_admin en admin-panel
                await SeedUserRolesAsync(adminUser, adminApp, adminSuperAdmin);

                // Asignar super_admin en portal
                await SeedUserRolesAsync(adminUser, portalApp, portalSuperAdmin);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation(
                    "Seed completado. Tenant: {Domain}, Admin: {Email}",
                    tenant.Domain,
                    adminUser.Email
                );
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error durante el seed. Se revirtieron los cambios.");
                throw;
            }
        });
    }

    private async Task<Tenant> SeedTenantAsync()
    {
        var existing = await _context.Tenants.FirstOrDefaultAsync(t =>
            t.Domain == DefaultTenantDomain
        );

        if (existing is not null)
        {
            _logger.LogDebug("Tenant '{Domain}' ya existe.", DefaultTenantDomain);
            return existing;
        }

        var tenant = Tenant.Create("International Polytechnic", DefaultTenantDomain);
        await _context.Tenants.AddAsync(tenant);
        _logger.LogInformation("Tenant '{Domain}' creado.", DefaultTenantDomain);
        return tenant;
    }

    private async Task<(
        ClientApplication AdminApp,
        ClientApplication PortalApp
    )> SeedApplicationsAsync(Tenant tenant)
    {
        var adminApp = await SeedApplicationAsync(
            tenant,
            "Admin Panel",
            AdminPanelClientId,
            ["http://localhost:3001/api/auth/callback"],
            ["openid", "profile", "email"],
            ["authorization_code", "refresh_token"],
            BCrypt.Net.BCrypt.HashPassword("admin-panel-secret-change-me"),
            "Panel de administración del IdP"
        );

        var portalApp = await SeedApplicationAsync(
            tenant,
            "Portal",
            PortalClientId,
            ["http://localhost:3002/api/auth/callback"],
            ["openid", "profile", "email"],
            ["authorization_code", "refresh_token"],
            BCrypt.Net.BCrypt.HashPassword("portal-secret-change-me"),
            "App de prueba del flujo OAuth"
        );

        return (adminApp, portalApp);
    }

    private async Task<ClientApplication> SeedApplicationAsync(
        Tenant tenant,
        string name,
        string clientId,
        List<string> redirectUris,
        List<string> allowedScopes,
        List<string> grantTypes,
        string clientSecretHash,
        string description
    )
    {
        var clientIdVO = ClientId.Create(clientId);
        var existing = await _context.ClientApplications.FirstOrDefaultAsync(a =>
            a.ClientId == clientIdVO
        );

        if (existing is not null)
        {
            _logger.LogDebug("App '{ClientId}' ya existe.", clientId);
            return existing;
        }

        var app = ClientApplication.Create(
            tenantId: tenant.Id,
            name: name,
            clientId: clientId,
            redirectUris: redirectUris,
            allowedScopes: allowedScopes,
            grantTypes: grantTypes,
            clientSecretHash: clientSecretHash,
            description: description,
            isDevelopment: true
        );

        await _context.ClientApplications.AddAsync(app);
        _logger.LogInformation("App '{ClientId}' creada.", clientId);
        return app;
    }

    private async Task<(Role SuperAdmin, Role TenantAdmin)> SeedAdminRolesAsync(
        Tenant tenant,
        ClientApplication adminApp
    )
    {
        var superAdmin = await SeedRoleAsync(
            tenant,
            adminApp,
            "super_admin",
            "Acceso total al sistema"
        );

        var tenantAdmin = await SeedRoleAsync(
            tenant,
            adminApp,
            "tenant_admin",
            "Administración del tenant"
        );

        return (superAdmin, tenantAdmin);
    }

    private async Task<Role> SeedPortalRolesAsync(Tenant tenant, ClientApplication portalApp)
    {
        return await SeedRoleAsync(tenant, portalApp, "super_admin", "Acceso total en portal");
    }

    private async Task<Role> SeedRoleAsync(
        Tenant tenant,
        ClientApplication app,
        string name,
        string description
    )
    {
        var existing = await _context.Roles.FirstOrDefaultAsync(r =>
            r.ClientApplicationId == app.Id && r.Name == name
        );

        if (existing is not null)
        {
            _logger.LogDebug("Rol '{Name}' en '{App}' ya existe.", name, app.ClientId);
            return existing;
        }

        var role = Role.Create(
            tenantId: tenant.Id,
            clientApplicationId: app.Id,
            name: name,
            description: description
        );

        await _context.Roles.AddAsync(role);
        _logger.LogInformation("Rol '{Name}' creado en '{App}'.", name, app.ClientId);
        return role;
    }

    private async Task<User> SeedAdminUserAsync(Tenant tenant)
    {
        var email = Email.Create(SuperAdminEmail);
        var existing = await _context.Users.FirstOrDefaultAsync(u =>
            u.TenantId == tenant.Id && u.Email == email
        );

        if (existing is not null)
        {
            _logger.LogDebug("Usuario '{Email}' ya existe.", SuperAdminEmail);
            return existing;
        }

        var passwordHash = BCrypt.Net.BCrypt.HashPassword(SuperAdminPassword);
        var name = PersonName.Create("Super", "Admin");
        var user = User.Create(tenant.Id, email, passwordHash, name);

        await _context.Users.AddAsync(user);
        _logger.LogInformation("Usuario '{Email}' creado.", SuperAdminEmail);
        return user;
    }

    private async Task SeedUserRolesAsync(User user, ClientApplication app, Role role)
    {
        var existing = await _context.UserApplicationRoles.FirstOrDefaultAsync(r =>
            r.UserId == user.Id && r.ClientApplicationId == app.Id && r.RoleId == role.Id
        );

        if (existing is not null)
        {
            _logger.LogDebug("Rol '{Role}' en '{App}' ya asignado.", role.Name, app.ClientId);
            return;
        }

        var userRole = UserApplicationRole.Create(
            userId: user.Id,
            clientApplicationId: app.Id,
            roleId: role.Id
        );

        await _context.UserApplicationRoles.AddAsync(userRole);
        _logger.LogInformation(
            "Rol '{Role}' asignado a '{Email}' en '{App}'.",
            role.Name,
            user.Email,
            app.ClientId
        );
    }
}
