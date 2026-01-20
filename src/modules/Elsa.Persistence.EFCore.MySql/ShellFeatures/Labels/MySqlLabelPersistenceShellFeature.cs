using CShells.Features;
using Elsa.Persistence.EFCore.Modules.Labels;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Elsa.Persistence.EFCore.MySql.ShellFeatures.Labels;

/// <summary>
/// Configures the labels feature to use MySql persistence.
/// </summary>
[ShellFeature(
    DisplayName = "MySql Label Persistence",
    Description = "Provides MySql persistence for label management")]
[UsedImplicitly]
public class MySqlLabelPersistenceShellFeature
    : DatabaseProviderShellFeature<EFCoreLabelPersistenceShellFeature, LabelsElsaDbContext, MySqlDbContextOptionsBuilder>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MySqlLabelPersistenceShellFeature"/> class.
    /// </summary>
    public MySqlLabelPersistenceShellFeature()
        : base(new MySqlProviderConfigurator(typeof(MySqlLabelPersistenceShellFeature).Assembly))
    {
    }
}
