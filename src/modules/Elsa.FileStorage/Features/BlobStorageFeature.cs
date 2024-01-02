using Elsa.Extensions;
using Elsa.Features.Abstractions;
using Elsa.Features.Services;
using Elsa.FileStorage.Services;
using FluentStorage;
using FluentStorage.Blobs;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.FileStorage.Features;

/// <summary>
/// The Storage feature provides activities to interact with a storage provider.
/// </summary>
public class FileStorageFeature : FeatureBase
{
    /// <inheritdoc />
    public FileStorageFeature(IModule module) : base(module)
    {
    }

    /// <summary>
    /// The blob storage to use.
    /// </summary>
    public Func<IServiceProvider, IBlobStorage> BlobStorage { get; set; } = _ => StorageFactory.Blobs.DirectoryFiles(GetDefaultStorageDirectory());

    /// <inheritdoc />
    public override void Configure()
    {
        Module.AddActivitiesFrom<FileStorageFeature>();
    }

    /// <inheritdoc />
    public override void Apply()
    {
        Services.AddScoped<IBlobStorageProvider>(sp => new BlobStorageProvider(BlobStorage(sp)));
    }

    /// <summary>
    /// Gets the default workflows directory.
    /// </summary>
    /// <returns>The default workflows directory.</returns>
    public static string GetDefaultStorageDirectory()
    {
        return Path.Combine(Path.GetTempPath(), "Elsa", "Storage", "Blobs");
    }
}