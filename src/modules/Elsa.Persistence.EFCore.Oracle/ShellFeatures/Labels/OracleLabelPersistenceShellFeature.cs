using System.Reflection;
using CShells.Features;
using Elsa.Persistence.EFCore.Extensions;
using Elsa.Persistence.EFCore.Modules.Labels;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;

namespace Elsa.Persistence.EFCore.Oracle.ShellFeatures.Labels;

/// <summary>
/// Configures the labels feature to use Oracle persistence.
/// </summary>
[ShellFeature(
    DisplayName = "Oracle Label Persistence",
    Description = "Provides Oracle persistence for label management",
    DependsOn = ["Labels"])]
[UsedImplicitly]
public class OracleLabelPersistenceShellFeature
    : EFCoreLabelPersistenceShellFeatureBase
{
    /// <inheritdoc />
    protected override void ConfigureProvider(DbContextOptionsBuilder builder, Assembly migrationsAssembly, string connectionString, ElsaDbContextOptions? options)
    {
        builder.UseElsaOracle(migrationsAssembly, connectionString, options);
    }
}
