using Elsa.Extensions;
using Elsa.Features.Services;
using Elsa.Logging.Features;

namespace Elsa.Logging.Extensions;

public static class ModuleExtensions
{
    /// <summary>
    /// Installs the ProcessLogging module.
    /// </summary>
    public static IModule UseProcessLogging(this IModule module, Action<ProcessLoggingFeature>? configure = null)
    {
        return module.Use(configure);
    }
}