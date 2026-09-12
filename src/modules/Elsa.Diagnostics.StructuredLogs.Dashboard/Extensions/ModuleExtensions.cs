using Elsa.Diagnostics.StructuredLogs.Dashboard.Features;
using Elsa.Features.Services;

// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

public static class StructuredLogsDashboardModuleExtensions
{
    public static IModule UseStructuredLogsDashboard(this IModule module, Action<StructuredLogsDashboardFeature>? configure = null)
    {
        module.Configure(configure);
        return module;
    }
}
