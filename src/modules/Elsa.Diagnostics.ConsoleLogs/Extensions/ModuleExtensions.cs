using Elsa.Diagnostics.ConsoleLogs.Features;
using Elsa.Features.Services;

// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

public static class ModuleExtensions
{
    public static IModule UseConsoleLogs(this IModule module, Action<ConsoleLogsFeature>? configure = null)
    {
        return module.Use(configure);
    }
}
