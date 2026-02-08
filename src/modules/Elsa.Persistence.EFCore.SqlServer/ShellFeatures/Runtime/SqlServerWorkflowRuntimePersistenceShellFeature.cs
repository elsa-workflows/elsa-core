using System.Reflection;
using CShells.Features;
using Elsa.Persistence.EFCore.Extensions;
using Elsa.Persistence.EFCore.Modules.Runtime;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;

namespace Elsa.Persistence.EFCore.SqlServer.ShellFeatures.Runtime;

/// <summary>
/// Configures the runtime feature to use SqlServer persistence.
/// </summary>
[ShellFeature(
    DisplayName = "SqlServer Workflow Runtime Persistence",
    Description = "Provides SqlServer persistence for workflow runtime",
    DependsOn = ["WorkflowRuntime"])]
[UsedImplicitly]
public class SqlServerWorkflowRuntimePersistenceShellFeature
    : EFCoreWorkflowRuntimePersistenceShellFeatureBase
{
    /// <inheritdoc />
    protected override void ConfigureProvider(DbContextOptionsBuilder builder, Assembly migrationsAssembly, string connectionString, ElsaDbContextOptions? options)
    {
        builder.UseElsaSqlServer(migrationsAssembly, connectionString, options);
    }
}
