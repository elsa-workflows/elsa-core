using Elsa.Features.Services;
using Elsa.OpenTelemetry.Features;

// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

public static class ModuleExtensions
{
    public static IModule UseOpenTelemetry(this IModule configuration, Action<OpenTelemetryFeature>? configure = null)
    {
        configuration.Configure(configure);
        return configuration;
    }
}