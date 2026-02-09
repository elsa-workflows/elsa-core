using System.Reflection;
using CShells.Features;
using Elsa.WorkflowProviders.BlobStorage.Contracts;
using Elsa.WorkflowProviders.BlobStorage.Handlers;
using Elsa.WorkflowProviders.BlobStorage.Providers;
using FluentStorage;
using FluentStorage.Blobs;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.WorkflowProviders.BlobStorage.ShellFeatures;

/// <summary>
/// A feature that enables the FluentStorage workflow definition provider.
/// </summary>
[ShellFeature(
    DisplayName = "BlobStorage Workflow Provider",
    Description = "Enables loading workflow definitions from blob storage (file system, Azure Blob, AWS S3, etc.)",
    DependsOn = ["WorkflowManagement"])]
public class BlobStorageFeature : IShellFeature
{
    /// <summary>
    /// The directory path where workflows are stored. Defaults to "Workflows" folder in the application directory.
    /// </summary>
    public string? WorkflowsDirectory { get; set; }

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddScoped<IBlobStorageProvider>(sp =>
        {
            var directory = WorkflowsDirectory ?? GetDefaultWorkflowsDirectory();
            var blobStorage = StorageFactory.Blobs.DirectoryFiles(directory);
            return new BlobStorageProvider(blobStorage);
        });

        // Register the JSON format handler (built-in support)
        services.AddScoped<IBlobWorkflowFormatHandler, JsonBlobWorkflowFormatHandler>();

        services.AddWorkflowsProvider<BlobStorageWorkflowsProvider>();
    }

    /// <summary>
    /// Gets the default workflows directory.
    /// </summary>
    /// <returns>The default workflows directory.</returns>
    private static string GetDefaultWorkflowsDirectory()
    {
        var entryAssemblyDir = Path.GetDirectoryName(Assembly.GetEntryAssembly()!.Location)!;
        var directory = Path.Combine(entryAssemblyDir, "Workflows");
        return directory;
    }
}
