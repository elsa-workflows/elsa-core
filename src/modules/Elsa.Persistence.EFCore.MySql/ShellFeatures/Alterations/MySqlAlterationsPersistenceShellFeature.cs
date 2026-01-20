using CShells.Features;
using Elsa.Persistence.EFCore.Modules.Alterations;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Elsa.Persistence.EFCore.MySql.ShellFeatures.Alterations;

/// <summary>
/// Configures the alterations feature to use MySql persistence.
/// </summary>
[ShellFeature(
    DisplayName = "MySql Alterations Persistence",
    Description = "Provides MySql persistence for workflow alterations")]
[UsedImplicitly]
public class MySqlAlterationsPersistenceShellFeature
    : DatabaseProviderShellFeature<EFCoreAlterationsPersistenceShellFeature, AlterationsElsaDbContext, MySqlDbContextOptionsBuilder>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MySqlAlterationsPersistenceShellFeature"/> class.
    /// </summary>
    public MySqlAlterationsPersistenceShellFeature()
        : base(new MySqlProviderConfigurator(typeof(MySqlAlterationsPersistenceShellFeature).Assembly))
    {
    }
}
