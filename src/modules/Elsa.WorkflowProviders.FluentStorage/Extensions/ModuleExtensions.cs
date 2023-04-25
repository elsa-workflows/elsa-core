using Elsa.Features.Services;
using Elsa.WorkflowProviders.FluentStorage.Features;
using FluentStorage.Blobs;
using JetBrains.Annotations;

// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

/// <summary>
/// Extension methods for <see cref="IModule"/> to add the fluent storage workflow definition provider.
/// </summary>
[PublicAPI]
public static class ModuleExtensions
{
    /// <summary>
    /// Adds the fluent storage workflow definition provider.
    /// </summary>
    /// <param name="module">The module.</param>
    /// <param name="blobStorage">A callback that creates an <see cref="IBlobStorage"/>.</param>
    /// <returns>The module.</returns>
    public static IModule UseFluentStorageProvider(this IModule module, Func<IServiceProvider, IBlobStorage> blobStorage)
    {
        return module.UseFluentStorageProvider(feature => feature.BlobStorage = blobStorage);
    }
    
    /// <summary>
    /// Adds the fluent storage workflow definition provider.
    /// </summary>
    /// <param name="module">The module.</param>
    /// <param name="configure">The configuration delegate.</param>
    /// <returns>The module.</returns>
    public static IModule UseFluentStorageProvider(this IModule module, Action<FluentStorageFeature>? configure = default)
    {
        module.Use(configure);
        return module;
    }
}