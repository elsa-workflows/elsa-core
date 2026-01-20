using CShells.Features;
using Elsa.Persistence.EFCore.Modules.Labels;
using JetBrains.Annotations;
using Oracle.EntityFrameworkCore.Infrastructure;

namespace Elsa.Persistence.EFCore.Oracle.ShellFeatures.Labels;

/// <summary>
/// Configures the labels feature to use Oracle persistence.
/// </summary>
[ShellFeature(
    DisplayName = "Oracle Label Persistence",
    Description = "Provides Oracle persistence for label management")]
[UsedImplicitly]
public class OracleLabelPersistenceShellFeature
    : DatabaseProviderShellFeature<EFCoreLabelPersistenceShellFeature, LabelsElsaDbContext, OracleDbContextOptionsBuilder>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OracleLabelPersistenceShellFeature"/> class.
    /// </summary>
    public OracleLabelPersistenceShellFeature()
        : base(new OracleProviderConfigurator(typeof(OracleLabelPersistenceShellFeature).Assembly))
    {
    }
}
