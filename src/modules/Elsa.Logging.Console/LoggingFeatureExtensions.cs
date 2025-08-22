using Elsa.Extensions;
using Elsa.Features.Services;
using Elsa.Logging.Features;

// ReSharper disable once CheckNamespace
namespace Elsa.Logging.Extensions;

public static class LoggingFeatureExtensions
{
    /// <summary>
    /// Installs the Console logging feature.
    /// </summary>
    public static IModule UseConsole(this LoggingFeature feature, Action<ConsoleLoggingFeature>? configure = null)
    {
        return feature.Module.Use(configure);
    }
}