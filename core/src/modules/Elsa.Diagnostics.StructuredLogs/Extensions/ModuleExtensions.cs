using Elsa.Diagnostics.StructuredLogs.Features;
using Elsa.Features.Services;

// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

public static class StructuredLogsModuleExtensions
{
    public static IModule UseStructuredLogs(this IModule module, Action<StructuredLogsFeature>? configure = null)
    {
        module.Configure(configure);
        return module;
    }
}
