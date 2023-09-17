using System.Reflection;
using Elsa.Features.Abstractions;
using Elsa.Features.Attributes;
using Elsa.Features.Services;
using Elsa.WorkflowProviders.BlobStorage.Contracts;
using Elsa.WorkflowProviders.BlobStorage.Providers;
using Elsa.Workflows.Management.Features;
using FluentStorage;
using FluentStorage.Blobs;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.WorkflowProviders.BlobStorage.Features;

/// <summary>
/// A feature that enables the FluentStorage workflow definition provider.
/// </summary>
[DependsOn(typeof(WorkflowManagementFeature))]
[DependsOn(typeof(DslIntegrationFeature))]
public class BlobStorageFeature : FeatureBase
{
    /// <inheritdoc />
    public BlobStorageFeature(IModule module) : base(module)
    {
    }

    /// <summary>
    /// The blob storage to use.
    /// </summary>
    public Func<IServiceProvider, IBlobStorage> BlobStorage { get; set; } = _ => StorageFactory.Blobs.DirectoryFiles(GetDefaultWorkflowsDirectory());

    /// <inheritdoc />
    public override void Apply()
    {
        Services.AddSingleton<IBlobStorageProvider>(sp => new BlobStorageProvider(BlobStorage(sp)));
        Services.AddWorkflowDefinitionProvider<BlobStorageWorkflowProvider>();
    }

    /// <summary>
    /// Gets the default workflows directory.
    /// </summary>
    /// <returns>The default workflows directory.</returns>
    public static string GetDefaultWorkflowsDirectory()
    {
        var entryAssemblyDir = Path.GetDirectoryName(Assembly.GetEntryAssembly()!.Location)!;
        var directory = Path.Combine(entryAssemblyDir, "Workflows");
        return directory;
    }

}