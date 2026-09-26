using Elsa.Features.Services;
using Elsa.Storage.Files.Features;
using FluentStorage.Blobs;
using JetBrains.Annotations;

// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

/// <summary>
/// Extension methods for <see cref="IModule"/> to install the Storage feature.
/// </summary>
[PublicAPI]
public static class ModuleExtensions
{
    /// <summary>
    /// Installs the Storage feature.
    /// </summary>
    public static IModule UseFileStorage(this IModule module, Func<IServiceProvider, IBlobStorage> blobStorage)
    {
        return module.UseFileStorage(feature => feature.BlobStorage = blobStorage);
    }
    
    /// <summary>
    /// Installs the Storage feature.
    /// </summary>
    public static IModule UseFileStorage(this IModule module, Action<FileStorageFeature>? configure = null)
    {
        module.Use(configure);
        return module;
    }
}