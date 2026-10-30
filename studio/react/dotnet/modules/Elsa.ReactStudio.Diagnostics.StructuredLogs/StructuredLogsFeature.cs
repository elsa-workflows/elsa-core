using CShells.Features;
using Elsa.ReactStudio.Host;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.ReactStudio.Diagnostics.StructuredLogs;

/// <summary>Contributes the independently built Diagnostics.StructuredLogs Preview module when enabled.</summary>
[ShellFeature("ReactStudio.Diagnostics.StructuredLogs", DependsOn = [typeof(ReactStudioPreviewFeature)])]
public sealed class StructuredLogsFeature : IShellFeature
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(new ReactStudioModule("diagnostics-structured-logs", "Diagnostics.StructuredLogs", "Elsa.ReactStudio.Diagnostics.StructuredLogs", false,
            typeof(StructuredLogsFeature).Assembly, ["Elsa.Diagnostics.StructuredLogs.ShellFeatures.StructuredLogs"]));
    }
}
