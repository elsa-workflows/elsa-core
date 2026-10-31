using Elsa.Diagnostics.OpenTelemetry.Features;
using Elsa.Features.Services;

// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

public static class OpenTelemetryDiagnosticsModuleExtensions
{
    public static IModule UseOpenTelemetryDiagnostics(this IModule module, Action<OpenTelemetryFeature>? configure = null)
    {
        module.Configure(configure);
        return module;
    }
}
