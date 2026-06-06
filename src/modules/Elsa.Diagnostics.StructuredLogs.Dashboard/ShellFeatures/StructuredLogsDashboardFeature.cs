using CShells.Features;
using Elsa.Diagnostics.StructuredLogs.Dashboard.Extensions;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Diagnostics.StructuredLogs.Dashboard.ShellFeatures;

[ShellFeature(
    DisplayName = "Structured Logs Dashboard",
    Description = "Provides structured log dashboard contributions",
    DependsOn = [typeof(global::Elsa.Diagnostics.StructuredLogs.ShellFeatures.StructuredLogsFeature), typeof(global::Elsa.Dashboard.Api.ShellFeatures.DashboardApiFeature)])]
[UsedImplicitly]
public class StructuredLogsDashboardFeature : IShellFeature
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddStructuredLogsDashboard();
    }
}
