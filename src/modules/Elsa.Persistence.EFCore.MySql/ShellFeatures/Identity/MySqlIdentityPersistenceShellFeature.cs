using CShells.Features;
using Elsa.Persistence.EFCore.Modules.Identity;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Elsa.Persistence.EFCore.MySql.ShellFeatures.Identity;

/// <summary>
/// Configures the identity feature to use MySql persistence.
/// </summary>
[ShellFeature(
    DisplayName = "MySql Identity Persistence",
    Description = "Provides MySql persistence for identity management")]
[UsedImplicitly]
public class MySqlIdentityPersistenceShellFeature
    : DatabaseProviderShellFeature<EFCoreIdentityPersistenceShellFeature, IdentityElsaDbContext, MySqlDbContextOptionsBuilder>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MySqlIdentityPersistenceShellFeature"/> class.
    /// </summary>
    public MySqlIdentityPersistenceShellFeature()
        : base(new MySqlProviderConfigurator(typeof(MySqlIdentityPersistenceShellFeature).Assembly))
    {
    }
}
