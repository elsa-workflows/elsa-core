using System.Reflection;
using CShells.Features;
using Elsa.Persistence.EFCore.Extensions;
using Elsa.Persistence.EFCore.Modules.Runtime;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Persistence.EFCore.Sqlite.ShellFeatures.Runtime;

/// <summary>
/// Configures the runtime feature to use Sqlite persistence.
/// </summary>
[ShellFeature(
    DisplayName = "Sqlite Workflow Runtime Persistence",
    Description = "Provides Sqlite persistence for workflow runtime",
    DependsOn = ["WorkflowRuntime"])]
[UsedImplicitly]
public class SqliteWorkflowRuntimePersistenceShellFeature
    : EFCoreWorkflowRuntimePersistenceShellFeatureBase
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
