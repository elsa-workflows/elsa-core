using Elsa.Extensions;
using Elsa.Features.Services;
using Elsa.Logging.Features;

namespace Elsa.Logging.Extensions;

public static class ModuleExtensions
{
    /// <summary>
    /// Installs the logging module.
    /// </summary>
    public static IModule UseLoggingFramework(this IModule module, Action<LoggingFeature>? configure = null)
    {
        return module.Use(configure);
    }
}