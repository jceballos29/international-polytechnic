using Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Identity.Infrastructure.Persistence;

/// <summary>
/// Contexto de base de datos del Identity Server.
///
/// Ciclo de vida (uno por request HTTP):
///   1. Se crea al inicio del request
///   2. Los repositorios hacen consultas y modificaciones
///   3. EF Core trackea los cambios en memoria
///   4. UnitOfWork.SaveChangesAsync() persiste todo junto
///   5. Se destruye al terminar el request
///
/// ApplyConfigurationsFromAssembly escanea el assembly y aplica
/// automáticamente todas las clases IEntityTypeConfiguration<T>
/// sin tener que registrar cada una manualmente.
/// </summary>
public class IdentityDbContext : DbContext
{
    public IdentityDbContext(DbContextOptions<IdentityDbContext> options)
        : base(options) { }

    // ── DbSets — una propiedad por tabla ───────────────────
    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<User> Users => Set<User>();
    public DbSet<ClientApplication> ClientApplications => Set<ClientApplication>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<UserApplicationRole> UserApplicationRoles => Set<UserApplicationRole>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<AuthorizationCode> AuthorizationCodes => Set<AuthorizationCode>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Aplica todas las clases Configuration automáticamente
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(IdentityDbContext).Assembly);

        // ── Convención global: snake_case ──────────────────
        //
        // PostgreSQL usa snake_case por convención.
        // EF Core usa PascalCase por defecto.
        // Este loop convierte todos los nombres automáticamente:
        //   tabla: ClientApplication → client_applications
        //   columna: PasswordHash → password_hash
        //   columna: TenantId → tenant_id
        foreach (var entity in modelBuilder.Model.GetEntityTypes())
        {
            var isOwned = entity.IsOwned();

            // Solo renombrar tabla e índices en entidades normales
            // Las owned entities (ej: PersonName) comparten tabla
            // con su dueño — si renombramos sus PKs internas
            // hay conflictos con la PK del dueño
            if (!isOwned)
            {
                var tableName = entity.GetTableName();
                if (tableName != null)
                    entity.SetTableName(ToSnakeCase(tableName));

                foreach (var key in entity.GetKeys())
                {
                    var keyName = key.GetName();
                    if (keyName != null)
                        key.SetName(ToSnakeCase(keyName));
                }

                foreach (var index in entity.GetIndexes())
                {
                    var indexName = index.GetDatabaseName();
                    if (indexName != null)
                        index.SetDatabaseName(ToSnakeCase(indexName));
                }

                foreach (var fk in entity.GetForeignKeys())
                {
                    var constraintName = fk.GetConstraintName();
                    if (constraintName != null)
                        fk.SetConstraintName(ToSnakeCase(constraintName));
                }
            }

            // Renombrar columnas — aplica a todos incluyendo owned
            foreach (var property in entity.GetProperties())
            {
                var columnName = property.GetColumnName();
                if (columnName != null)
                    property.SetColumnName(ToSnakeCase(columnName));
            }
        }
    }

    /// <summary>
    /// Convierte PascalCase a snake_case.
    ///   "PasswordHash"      → "password_hash"
    ///   "ClientApplication" → "client_application"
    ///   "TenantId"          → "tenant_id"
    ///   "CreatedAt"         → "created_at"
    /// </summary>
    private static string ToSnakeCase(string name)
    {
        if (string.IsNullOrEmpty(name))
            return name;

        var result = new System.Text.StringBuilder();
        for (int i = 0; i < name.Length; i++)
        {
            var c = name[i];
            if (char.IsUpper(c) && i > 0)
            {
                var prevIsLower = char.IsLower(name[i - 1]) || char.IsDigit(name[i - 1]);
                var nextIsLower = i + 1 < name.Length && char.IsLower(name[i + 1]);
                if (prevIsLower || nextIsLower)
                    result.Append('_');
            }
            result.Append(char.ToLowerInvariant(c));
        }
        return result.ToString();
    }
}
