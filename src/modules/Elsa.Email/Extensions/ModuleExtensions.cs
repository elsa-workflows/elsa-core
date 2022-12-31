using Elsa.Email.Features;
using Elsa.Features.Services;

// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

/// <summary>
/// Provides methods to install and configure email related features.
/// </summary>
public static class ModuleExtensions
{
    /// <summary>
    /// Adds the <see cref="EmailFeature"/> feature to the system.
    /// </summary>
    public static IModule UseEmail(this IModule configuration, Action<EmailFeature>? configure = default)
    {
        configuration.Configure(configure);
        return configuration;
    }
}