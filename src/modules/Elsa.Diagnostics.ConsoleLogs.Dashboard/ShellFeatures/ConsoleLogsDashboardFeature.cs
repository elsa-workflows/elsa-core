using CShells.Features;
using Elsa.Diagnostics.ConsoleLogs.Dashboard.Extensions;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Diagnostics.ConsoleLogs.Dashboard.ShellFeatures;

[ShellFeature(
    DisplayName = "Console Logs Dashboard",
    Description = "Provides console log dashboard contributions",
    DependsOn = [typeof(global::Elsa.Diagnostics.ConsoleLogs.ShellFeatures.ConsoleLogsFeature), typeof(global::Elsa.Dashboard.Api.ShellFeatures.DashboardApiFeature)])]
[UsedImplicitly]
public class ConsoleLogsDashboardFeature : IShellFeature
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddConsoleLogsDashboard();
    }
}
