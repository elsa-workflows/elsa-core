using ConsoleLogStream.Core.Options;
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

    public static IModule UseConsoleLogs(this IModule module, Action<ConsoleLogOptions> configureOptions)
    {
        return module.UseConsoleLogs(feature => feature.ConfigureOptions = configureOptions);
    }
}
