using System.Reflection;
using CShells.Features;
using Elsa.AI.Host.ShellFeatures;
using Elsa.AI.Persistence.EFCore.ShellFeatures;
using Elsa.Persistence.EFCore;
using Elsa.Persistence.EFCore.Extensions;
using Elsa.Platform.PackageManifest.Generator.Hints;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;

namespace Elsa.AI.Persistence.EFCore.SqlServer.ShellFeatures;

[ManifestFeatureCategory("AI")]
[ManifestFeatureCategory("Persistence")]
[ShellFeature(
    DisplayName = "SQL Server AI Persistence",
    Description = "Provides SQL Server persistence for Weaver AI conversations, proposals and audit records",
    DependsOn = [typeof(AIFeature)])]
[UsedImplicitly]
[ManifestInfrastructure("sqlserver-database", "database", Reason = "Stores Weaver AI persistence data in SQL Server.", Providers = new[] { "SQL Server" }, ConfigurationKeys = new[] { "ConnectionString" })]
public class SqlServerAIPersistenceShellFeature : EFCoreAIPersistenceShellFeatureBase
{
    protected override void ConfigureProvider(DbContextOptionsBuilder builder, Assembly migrationsAssembly, string connectionString, ElsaDbContextOptions? options)
    {
        builder.UseElsaSqlServer(migrationsAssembly, connectionString, options);
    }
}
