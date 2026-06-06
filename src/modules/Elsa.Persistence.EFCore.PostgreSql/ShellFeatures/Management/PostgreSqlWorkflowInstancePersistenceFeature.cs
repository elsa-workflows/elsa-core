using System.Reflection;
using CShells.Features;
using Elsa.Platform.PackageManifest.Generator.Hints;
using Elsa.Persistence.EFCore.Extensions;
using Elsa.Persistence.EFCore.Modules.Management;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Persistence.EFCore.PostgreSql.ShellFeatures.Management;

/// <summary>
/// Configures the management feature to use PostgreSql persistence.
/// </summary>
[ShellFeature(
    DisplayName = "PostgreSql Workflow Instance Persistence",
    Description = "Provides PostgreSql persistence for workflow instances",
    DependsOn = [typeof(global::Elsa.Workflows.Management.ShellFeatures.WorkflowManagementFeature), typeof(global::Elsa.Workflows.Management.ShellFeatures.WorkflowInstancesFeature)])]
[UsedImplicitly]
[ManifestInfrastructure("postgresql-database", "database", Reason = "Stores workflow instances in PostgreSQL.", Providers = new[] { "PostgreSQL" }, ConfigurationKeys = new[] { "ConnectionString" })]
public class PostgreSqlWorkflowInstancePersistenceFeature
    : EFCoreWorkflowInstancePersistenceShellFeatureBase
{
    /// <inheritdoc />
    protected override void ConfigureProvider(DbContextOptionsBuilder builder, Assembly migrationsAssembly, string connectionString, ElsaDbContextOptions? options)
    {
        builder.UseElsaPostgreSql(migrationsAssembly, connectionString, options);
    }

    /// <inheritdoc />
    protected override void OnConfiguring(IServiceCollection services)
    {
        services.AddPostgreSqlEntityModelCreatingHandlers();
        base.OnConfiguring(services);
    }
}
