using CShells.Features;
using Elsa.Extensions;
using Elsa.Storage.Files.Services;
using FluentStorage;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Storage.Files.ShellFeatures;

/// <summary>
/// Shell feature for file-based blob storage.
/// </summary>
[ShellFeature(
    DisplayName = "File Storage",
    Description = "Provides file-based blob storage activities for workflows")]
[UsedImplicitly]
public class FileStorageShellFeature : IShellFeature
{
    private readonly string _storageDirectory = GetDefaultStorageDirectory();

    public void ConfigureServices(IServiceCollection services)
    {
        var blobStorage = StorageFactory.Blobs.DirectoryFiles(_storageDirectory);
        services.AddScoped<IBlobStorageProvider>(sp => new BlobStorageProvider(blobStorage));
    }

    /// <summary>
    /// Gets the default storage directory.
    /// </summary>
    /// <returns>The default storage directory path.</returns>
    public static string GetDefaultStorageDirectory()
    {
        return Path.Combine(Path.GetTempPath(), "Elsa", "Storage", "Blobs");
    }
}

