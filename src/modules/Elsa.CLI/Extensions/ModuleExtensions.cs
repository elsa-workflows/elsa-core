using Elsa.CLI.Features;
using Elsa.Features.Services;

// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

/// <summary>
/// Provides methods to install and configure CLI related features.
/// </summary>
public static class ModuleExtensions
{
    /// <summary>
    /// Adds the <see cref="CliFeature"/> feature to the system.
    /// </summary>
    public static IModule UseCommandLine(this IModule configuration, Action<CliFeature>? configure = default)
    {
        configuration.Configure(configure);
        return configuration;
    }
}