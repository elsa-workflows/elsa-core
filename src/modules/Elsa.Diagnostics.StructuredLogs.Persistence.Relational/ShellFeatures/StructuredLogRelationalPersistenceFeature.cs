using CShells.Features;
using Elsa.Diagnostics.StructuredLogs.ShellFeatures;
using Elsa.Platform.PackageManifest.Generator.Hints;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Diagnostics.StructuredLogs.Persistence.Relational.ShellFeatures;

/// <summary>
/// Provides shared relational persistence services for diagnostics structured logs.
/// </summary>
[ManifestFeatureCategory(ManifestFeatureCategories.Diagnostics)]
[ManifestFeatureCategory(ManifestFeatureCategories.Persistence)]
[ShellFeature(
    DisplayName = "Structured Log Relational Persistence",
    Description = "Provides shared relational persistence services for diagnostics structured logs",
    DependsOn = [typeof(StructuredLogsFeature)])]
[UsedImplicitly]
public class StructuredLogRelationalPersistenceFeature : IShellFeature
{
    public void ConfigureServices(IServiceCollection services)
    {
    }
}
