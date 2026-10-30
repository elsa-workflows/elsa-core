using CShells.Features;
using Elsa.ReactStudio.Host;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.ReactStudio.Diagnostics.ConsoleLogs;

/// <summary>Contributes the independently built Diagnostics.ConsoleLogs Preview module when enabled.</summary>
[ShellFeature("ReactStudio.Diagnostics.ConsoleLogs", DependsOn = [typeof(ReactStudioPreviewFeature)])]
public sealed class ConsoleLogsFeature : IShellFeature
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(new ReactStudioModule("diagnostics-console-logs", "Diagnostics.ConsoleLogs", "Elsa.ReactStudio.Diagnostics.ConsoleLogs", false,
            typeof(ConsoleLogsFeature).Assembly, ["Elsa.Diagnostics.ConsoleLogs.ShellFeatures.ConsoleLogs"]));
    }
}
