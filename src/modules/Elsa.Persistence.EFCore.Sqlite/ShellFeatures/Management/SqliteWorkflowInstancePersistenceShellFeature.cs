using System.Reflection;
using CShells.Features;
using Elsa.Persistence.EFCore.Extensions;
using Elsa.Persistence.EFCore.Modules.Management;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Persistence.EFCore.Sqlite.ShellFeatures.Management;

/// <summary>
/// Configures the management feature to use Sqlite persistence.
/// </summary>
[ShellFeature(
    DisplayName = "Sqlite Workflow Instance Persistence",
    Description = "Provides Sqlite persistence for workflow instances",
    DependsOn = ["WorkflowManagement", "WorkflowInstances"])]
[UsedImplicitly]
public class SqliteWorkflowInstancePersistenceShellFeature
    : EFCoreWorkflowInstancePersistenceShellFeatureBase
{
    /// <inheritdoc />
    protected override void ConfigureProvider(DbContextOptionsBuilder builder, Assembly migrationsAssembly, string connectionString, ElsaDbContextOptions? options)
    {
        builder.UseElsaSqlite(migrationsAssembly, connectionString, options);
    }

    /// <inheritdoc />
    protected override void OnConfiguring(IServiceCollection services)
    {
        services.AddScoped<IEntityModelCreatingHandler, SetupForSqlite>();
        base.OnConfiguring(services);
    }
}
