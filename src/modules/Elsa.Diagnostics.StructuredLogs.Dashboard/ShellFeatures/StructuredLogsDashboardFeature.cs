using CShells.Features;
using Elsa.Dashboard.Api.ShellFeatures;
using Elsa.Diagnostics.StructuredLogs.Dashboard.Extensions;
using Elsa.Diagnostics.StructuredLogs.ShellFeatures;
using Elsa.Platform.PackageManifest.Generator.Hints;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Diagnostics.StructuredLogs.Dashboard.ShellFeatures;

[ManifestFeatureCategory(ManifestFeatureCategories.Diagnostics)]
[ManifestFeatureCategory(ManifestFeatureCategories.Dashboard)]
[ShellFeature(
    DisplayName = "Structured Logs Dashboard",
    Description = "Provides structured log dashboard contributions",
    DependsOn = [typeof(StructuredLogsFeature), typeof(DashboardApiFeature)])]
[UsedImplicitly]
public class StructuredLogsDashboardFeature : IShellFeature
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddStructuredLogsDashboard();
    }
}
