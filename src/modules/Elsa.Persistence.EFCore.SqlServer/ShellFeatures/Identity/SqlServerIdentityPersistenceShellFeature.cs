using CShells.Features;
using Elsa.Persistence.EFCore.Modules.Identity;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Elsa.Persistence.EFCore.SqlServer.ShellFeatures.Identity;

/// <summary>
/// Configures the identity feature to use SqlServer persistence.
/// </summary>
[ShellFeature(
    DisplayName = "SqlServer Identity Persistence",
    Description = "Provides SqlServer persistence for identity management")]
[UsedImplicitly]
public class SqlServerIdentityPersistenceShellFeature
    : DatabaseProviderShellFeature<EFCoreIdentityPersistenceShellFeature, IdentityElsaDbContext, SqlServerDbContextOptionsBuilder>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SqlServerIdentityPersistenceShellFeature"/> class.
    /// </summary>
    public SqlServerIdentityPersistenceShellFeature()
        : base(new SqlServerProviderConfigurator(typeof(SqlServerIdentityPersistenceShellFeature).Assembly))
    {
    }
}
