using CShells.Features;
using Elsa.ExternalAuthentication.Persistence.EFCore.ShellFeatures;
using Elsa.ExternalAuthentication.ShellFeatures;
using Elsa.Persistence.EFCore.Extensions;
using Elsa.Persistence.EFCore;
using Elsa.Platform.PackageManifest.Generator.Hints;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

namespace Elsa.ExternalAuthentication.Persistence.EFCore.SqlServer.ShellFeatures;

[ManifestFeatureCategory("Security")]
[ManifestFeatureCategory("Persistence")]
[ShellFeature(
    DisplayName = "SqlServer External Authentication Persistence",
    Description = "Provides SqlServer persistence for external authentication",
    DependsOn = [typeof(ExternalAuthenticationShellFeature)])]
[UsedImplicitly]
[ManifestInfrastructure("sqlserver-database", "database", Reason = "Stores external authentication connections, links, sessions and grants in SQL Server.", Providers = new[] { "SQL Server" }, ConfigurationKeys = new[] { "ConnectionString" })]
public class SqlServerExternalAuthenticationPersistenceShellFeature : EFCoreExternalAuthenticationPersistenceShellFeatureBase
{
    /// <inheritdoc />
    protected override void ConfigureProvider(DbContextOptionsBuilder builder, Assembly migrationsAssembly, string connectionString, ElsaDbContextOptions? options)
    {
        builder.UseElsaSqlServer(migrationsAssembly, connectionString, options);
    }
}
