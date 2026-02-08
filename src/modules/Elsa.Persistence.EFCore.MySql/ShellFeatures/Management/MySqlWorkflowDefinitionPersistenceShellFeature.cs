using System.Reflection;
using CShells.Features;
using Elsa.Persistence.EFCore.Extensions;
using Elsa.Persistence.EFCore.Modules.Management;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;

namespace Elsa.Persistence.EFCore.MySql.ShellFeatures.Management;

/// <summary>
/// Configures the management feature to use MySql persistence.
/// </summary>
[ShellFeature(
    DisplayName = "MySql Workflow Definition Persistence",
    Description = "Provides MySql persistence for workflow definitions",
    DependsOn = ["WorkflowManagement", "WorkflowDefinitions"])]
[UsedImplicitly]
public class MySqlWorkflowDefinitionPersistenceShellFeature
    : EFCoreWorkflowDefinitionPersistenceShellFeatureBase
{
    /// <inheritdoc />
    protected override void ConfigureProvider(DbContextOptionsBuilder builder, Assembly migrationsAssembly, string connectionString, ElsaDbContextOptions? options)
    {
        builder.UseElsaMySql(migrationsAssembly, connectionString, options);
    }
}
