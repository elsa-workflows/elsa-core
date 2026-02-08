using System.Reflection;
using CShells.Features;
using Elsa.Persistence.EFCore.Extensions;
using Elsa.Persistence.EFCore.Modules.Runtime;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;

namespace Elsa.Persistence.EFCore.Oracle.ShellFeatures.Runtime;

/// <summary>
/// Configures the runtime feature to use Oracle persistence.
/// </summary>
[ShellFeature(
    DisplayName = "Oracle Workflow Runtime Persistence",
    Description = "Provides Oracle persistence for workflow runtime",
    DependsOn = ["WorkflowRuntime"])]
[UsedImplicitly]
public class OracleWorkflowRuntimePersistenceShellFeature
    : EFCoreWorkflowRuntimePersistenceShellFeatureBase
{
    /// <inheritdoc />
    protected override void ConfigureProvider(DbContextOptionsBuilder builder, Assembly migrationsAssembly, string connectionString, ElsaDbContextOptions? options)
    {
        builder.UseElsaOracle(migrationsAssembly, connectionString, options);
    }
}
