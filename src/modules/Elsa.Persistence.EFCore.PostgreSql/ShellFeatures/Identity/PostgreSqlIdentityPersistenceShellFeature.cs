using CShells.Features;
using Elsa.Persistence.EFCore.Modules.Identity;
using JetBrains.Annotations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Infrastructure;

namespace Elsa.Persistence.EFCore.PostgreSql.ShellFeatures.Identity;

/// <summary>
/// Configures the identity feature to use PostgreSql persistence.
/// </summary>
[ShellFeature(
    DisplayName = "PostgreSql Identity Persistence",
    Description = "Provides PostgreSql persistence for identity management")]
[UsedImplicitly]
public class PostgreSqlIdentityPersistenceShellFeature
    : DatabaseProviderShellFeature<EFCoreIdentityPersistenceShellFeature, IdentityElsaDbContext, NpgsqlDbContextOptionsBuilder>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PostgreSqlIdentityPersistenceShellFeature"/> class.
    /// </summary>
    public PostgreSqlIdentityPersistenceShellFeature()
        : base(new PostgreSqlProviderConfigurator(typeof(PostgreSqlIdentityPersistenceShellFeature).Assembly))
    {
    }
}
