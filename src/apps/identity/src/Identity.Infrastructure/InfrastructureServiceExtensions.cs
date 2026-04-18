using Identity.Application.Common.Interfaces;
using Identity.Application.Common.Models;
using Identity.Domain.Interfaces;
using Identity.Domain.Interfaces.Repositories;
using Identity.Infrastructure.Cache;
using Identity.Infrastructure.Persistence;
using Identity.Infrastructure.Persistence.Repositories;
using Identity.Infrastructure.Security;
using Identity.Infrastructure.Session;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace Identity.Infrastructure;

/// <summary>
/// Extension method para registrar todos los servicios
/// de Infrastructure en el contenedor de DI.
///
/// En Program.cs solo se llama:
///   builder.Services.AddInfrastructure(config)
///
/// Cada capa tendrá su propio extension method:
///   AddInfrastructure() → EF Core, Redis, repositorios
///   AddApplication()    → MediatR, validators (Fase 2)
/// </summary>
public static class InfrastructureServiceExtensions
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        // ── Entity Framework Core ──────────────────────────
        services.AddDbContext<IdentityDbContext>(options =>
        {
            options.UseNpgsql(
                configuration.GetConnectionString("DefaultConnection"),
                npgsql =>
                {
                    npgsql.MigrationsHistoryTable("__ef_migrations_history");

                    // Reintenta la conexión si PostgreSQL no está listo
                    // Útil en Docker donde los servicios arrancan en paralelo
                    npgsql.EnableRetryOnFailure(
                        maxRetryCount: 5,
                        maxRetryDelay: TimeSpan.FromSeconds(10),
                        errorCodesToAdd: null
                    );
                }
            );

            // Solo en Development — muestra SQL y valores de parámetros
            // NUNCA en producción — expondría passwords y tokens en logs
            if (Environment.GetEnvironmentVariable("EF_SENSITIVE_LOGGING") == "true")
            {
                options.EnableSensitiveDataLogging();
            }
        });

        // ── Unit of Work ───────────────────────────────────
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // ── Repositorios ───────────────────────────────────────
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IClientApplicationRepository, ClientApplicationRepository>();
        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IAuthorizationCodeRepository, AuthorizationCodeRepository>();
        services.AddScoped<IAuditLogRepository, AuditLogRepository>();

        // ── Redis ──────────────────────────────────────────
        var redisConnection = configuration["Redis:ConnectionString"] ?? "localhost:6379";

        services.AddSingleton<IConnectionMultiplexer>(
            ConnectionMultiplexer.Connect(redisConnection)
        );

        // ── Servicios de seguridad ─────────────────────────
        services.AddScoped<IHashService, HashService>();
        services.AddScoped<IPkceService, PkceService>();

        // ── Configuración tipada ───────────────────────────────
        var jwtSettings = new JwtSettings
        {
            Issuer = configuration["Jwt:Issuer"] ?? "http://localhost:5000",
            AccessTokenExpiryMinutes = int.Parse(
                configuration["Jwt:AccessTokenExpiryMinutes"] ?? "15"
            ),
            RefreshTokenExpiryDays = int.Parse(configuration["Jwt:RefreshTokenExpiryDays"] ?? "30"),
            PrivateKeyPath = configuration["Jwt:PrivateKeyPath"] ?? "keys/private.pem",
            PublicKeyPath = configuration["Jwt:PublicKeyPath"] ?? "keys/public.pem",
        };

        services.AddSingleton(jwtSettings);
        services.AddSingleton<IJwtService, JwtService>();

        // ── Caché y sesiones ───────────────────────────────
        services.AddScoped<ICacheService, CacheService>();
        services.AddScoped<ISessionService, SessionService>();

        // ── Seed ───────────────────────────────────────────
        services.AddScoped<DataSeeder>();

        return services;
    }
}
