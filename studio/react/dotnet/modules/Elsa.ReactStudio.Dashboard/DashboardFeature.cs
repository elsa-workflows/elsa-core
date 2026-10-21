using CShells.Features;
using Elsa.ReactStudio.Host;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.ReactStudio.Dashboard;

/// <summary>Contributes the independently built Dashboard Preview module when enabled.</summary>
[ShellFeature("ReactStudio.Dashboard", DependsOn = [typeof(ReactStudioPreviewFeature)])]
public sealed class DashboardFeature : IShellFeature
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(new ReactStudioModule("dashboard", "Dashboard", "Elsa.ReactStudio.Dashboard", true,
            typeof(DashboardFeature).Assembly, []));
    }
}
