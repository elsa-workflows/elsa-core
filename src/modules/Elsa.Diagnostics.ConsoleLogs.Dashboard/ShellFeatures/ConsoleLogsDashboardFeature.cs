using CShells.Features;
using Elsa.Dashboard.Api.ShellFeatures;
using Elsa.Diagnostics.ConsoleLogs.Dashboard.Extensions;
using Elsa.Diagnostics.ConsoleLogs.ShellFeatures;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Diagnostics.ConsoleLogs.Dashboard.ShellFeatures;

[ShellFeature(
    DisplayName = "Console Logs Dashboard",
    Description = "Provides console log dashboard contributions",
    DependsOn = [typeof(ConsoleLogsFeature), typeof(DashboardApiFeature)])]
[UsedImplicitly]
public class ConsoleLogsDashboardFeature : IShellFeature
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddConsoleLogsDashboard();
    }
}
