using CShells.Features;
using Elsa.Persistence.EFCore.Modules.Identity;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Elsa.Persistence.EFCore.Sqlite.ShellFeatures.Identity;

/// <summary>
/// Configures the identity feature to use Sqlite persistence.
/// </summary>
[ShellFeature(
    DisplayName = "Sqlite Identity Persistence",
    Description = "Provides Sqlite persistence for identity management")]
[UsedImplicitly]
public class SqliteIdentityPersistenceShellFeature
    : DatabaseProviderShellFeature<EFCoreIdentityPersistenceShellFeature, IdentityElsaDbContext, SqliteDbContextOptionsBuilder>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SqliteIdentityPersistenceShellFeature"/> class.
    /// </summary>
    public SqliteIdentityPersistenceShellFeature()
        : base(new SqliteProviderConfigurator(typeof(SqliteIdentityPersistenceShellFeature).Assembly))
    {
    }
}
