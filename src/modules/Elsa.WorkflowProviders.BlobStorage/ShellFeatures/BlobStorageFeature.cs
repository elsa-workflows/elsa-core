using System.Reflection;
using CShells.Features;
using Elsa.WorkflowProviders.BlobStorage.Contracts;
using Elsa.WorkflowProviders.BlobStorage.Handlers;
using Elsa.WorkflowProviders.BlobStorage.Providers;
using Elsa.Workflows.Runtime;
using FluentStorage;
using FluentStorage.Blobs;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.WorkflowProviders.BlobStorage.ShellFeatures;

/// <summary>
/// A feature that enables the FluentStorage workflow definition provider.
/// </summary>
[ShellFeature(
    DisplayName = "Blob Storage Workflow Provider",
    Description = "Provides workflow definitions from blob storage",
    DependsOn = ["WorkflowManagement"])]
[UsedImplicitly]
public class BlobStorageFeature : IShellFeature
{
    /// <summary>
    /// The blob storage to use.
    /// </summary>
    public Func<IServiceProvider, IBlobStorage> BlobStorage { get; set; } = _ => StorageFactory.Blobs.DirectoryFiles(GetDefaultWorkflowsDirectory());

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddScoped<IBlobStorageProvider>(sp => new BlobStorageProvider(BlobStorage(sp)));

        // Register the JSON format handler (built-in support)
        services.AddScoped<IBlobWorkflowFormatHandler, JsonBlobWorkflowFormatHandler>();

        services.AddScoped<BlobStorageWorkflowsProvider>();
        services.AddScoped<IWorkflowsProvider>(sp => sp.GetRequiredService<BlobStorageWorkflowsProvider>());
    }

    /// <summary>
    /// Gets the default workflows directory.
    /// </summary>
    public static string GetDefaultWorkflowsDirectory()
    {
        var entryAssembly = Assembly.GetEntryAssembly();
        var entryAssemblyDir = entryAssembly != null 
            ? Path.GetDirectoryName(entryAssembly.Location) 
            : AppContext.BaseDirectory;
        var directory = Path.Combine(entryAssemblyDir!, "Workflows");
        return directory;
    }
}

