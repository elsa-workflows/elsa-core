using System.Reflection;
using CShells.Features;
using Elsa.Persistence.EFCore.Extensions;
using Elsa.Persistence.EFCore.Modules.Alterations;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;

namespace Elsa.Persistence.EFCore.Oracle.ShellFeatures.Alterations;

/// <summary>
/// Configures the alterations feature to use Oracle persistence.
/// </summary>
[ShellFeature(
    DisplayName = "Oracle Alterations Persistence",
    Description = "Provides Oracle persistence for workflow alterations",
    DependsOn = ["Alterations"])]
[UsedImplicitly]
public class OracleAlterationsPersistenceShellFeature
    : EFCoreAlterationsPersistenceShellFeatureBase
{
    /// <inheritdoc />
    protected override void ConfigureProvider(DbContextOptionsBuilder builder, Assembly migrationsAssembly, string connectionString, ElsaDbContextOptions? options)
    {
        builder.UseElsaOracle(migrationsAssembly, connectionString, options);
    }
}
