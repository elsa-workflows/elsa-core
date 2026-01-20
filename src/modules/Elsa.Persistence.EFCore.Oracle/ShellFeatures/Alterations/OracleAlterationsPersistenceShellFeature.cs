using CShells.Features;
using Elsa.Persistence.EFCore.Modules.Alterations;
using JetBrains.Annotations;
using Oracle.EntityFrameworkCore.Infrastructure;

namespace Elsa.Persistence.EFCore.Oracle.ShellFeatures.Alterations;

/// <summary>
/// Configures the alterations feature to use Oracle persistence.
/// </summary>
[ShellFeature(
    DisplayName = "Oracle Alterations Persistence",
    Description = "Provides Oracle persistence for workflow alterations")]
[UsedImplicitly]
public class OracleAlterationsPersistenceShellFeature
    : DatabaseProviderShellFeature<EFCoreAlterationsPersistenceShellFeature, AlterationsElsaDbContext, OracleDbContextOptionsBuilder>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OracleAlterationsPersistenceShellFeature"/> class.
    /// </summary>
    public OracleAlterationsPersistenceShellFeature()
        : base(new OracleProviderConfigurator(typeof(OracleAlterationsPersistenceShellFeature).Assembly))
    {
    }
}
