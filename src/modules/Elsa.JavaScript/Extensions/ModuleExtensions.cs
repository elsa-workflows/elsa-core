using Elsa.Features.Services;
using Elsa.JavaScript.Features;
using Elsa.JavaScript.Options;

// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

/// <summary>
/// Adds extensions to <see cref="IModule"/> that installs the <see cref="JavaScriptFeature"/> feature.
/// </summary>
public static class ModuleExtensions
{
    /// <summary>
    /// Setup the <see cref="JavaScriptFeature"/> feature.
    /// </summary>
    public static IModule UseJavaScript(this IModule module, Action<JavaScriptFeature>? configure = default)
    {
        module.Configure(configure);
        return module;
    }

    /// <summary>
    /// Setup the <see cref="JavaScriptFeature"/> feature.
    /// </summary>
    public static IModule UseJavaScript(this IModule module, Action<JintOptions> configureJintOptions)
    {
        return module.UseJavaScript(javaScript => javaScript.JintOptions = configureJintOptions);
    }
}