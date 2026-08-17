using Elsa.Diagnostics.ConsoleLogs.Dashboard.Features;
using Elsa.Features.Services;

// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

public static class ConsoleLogsDashboardModuleExtensions
{
    public static IModule UseConsoleLogsDashboard(this IModule module, Action<ConsoleLogsDashboardFeature>? configure = null)
    {
        module.Configure(configure);
        return module;
    }
}
