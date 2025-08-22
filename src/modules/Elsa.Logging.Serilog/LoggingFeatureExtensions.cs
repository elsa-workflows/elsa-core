using Elsa.Extensions;
using Elsa.Features.Services;
using Elsa.Logging.Features;
using Elsa.Logging.Serilog;

// ReSharper disable once CheckNamespace
namespace Elsa.Logging.Extensions;

public static class LoggingFeatureExtensions
{
    /// <summary>
    /// Installs the Serilog logging feature.
    /// </summary>
    public static IModule UseSerilog(this LoggingFeature feature, Action<SerilogLoggingFeature>? configure = null)
    {
        return feature.Module.Use(configure);
    }
}