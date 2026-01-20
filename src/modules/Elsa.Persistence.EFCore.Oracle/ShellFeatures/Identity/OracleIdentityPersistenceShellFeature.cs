using CShells.Features;
using Elsa.Persistence.EFCore.Modules.Identity;
using JetBrains.Annotations;
using Oracle.EntityFrameworkCore.Infrastructure;

namespace Elsa.Persistence.EFCore.Oracle.ShellFeatures.Identity;

/// <summary>
/// Configures the identity feature to use Oracle persistence.
/// </summary>
[ShellFeature(
    DisplayName = "Oracle Identity Persistence",
    Description = "Provides Oracle persistence for identity management")]
[UsedImplicitly]
public class OracleIdentityPersistenceShellFeature
    : DatabaseProviderShellFeature<EFCoreIdentityPersistenceShellFeature, IdentityElsaDbContext, OracleDbContextOptionsBuilder>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OracleIdentityPersistenceShellFeature"/> class.
    /// </summary>
    public OracleIdentityPersistenceShellFeature()
        : base(new OracleProviderConfigurator(typeof(OracleIdentityPersistenceShellFeature).Assembly))
    {
    }
}
