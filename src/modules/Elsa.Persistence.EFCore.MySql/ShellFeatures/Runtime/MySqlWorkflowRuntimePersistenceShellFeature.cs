using System.Reflection;
using CShells.Features;
using Elsa.Persistence.EFCore.Extensions;
using Elsa.Persistence.EFCore.Modules.Runtime;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;

namespace Elsa.Persistence.EFCore.MySql.ShellFeatures.Runtime;

/// <summary>
/// Configures the runtime feature to use MySql persistence.
/// </summary>
[ShellFeature(
    DisplayName = "MySql Workflow Runtime Persistence",
    Description = "Provides MySql persistence for workflow runtime",
    DependsOn = ["WorkflowRuntime"])]
[UsedImplicitly]
public class MySqlWorkflowRuntimePersistenceShellFeature
    : EFCoreWorkflowRuntimePersistenceShellFeatureBase
{
    /// <inheritdoc />
    protected override void ConfigureProvider(DbContextOptionsBuilder builder, Assembly migrationsAssembly, string connectionString, ElsaDbContextOptions? options)
    {
        builder.UseElsaMySql(migrationsAssembly, connectionString, options);
    }
}
