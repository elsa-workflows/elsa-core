using System.Reflection;
using Elsa.Features.Abstractions;
using Elsa.Features.Attributes;
using Elsa.Features.Services;
using Elsa.WorkflowProviders.FluentStorage.Contracts;
using Elsa.WorkflowProviders.FluentStorage.Services;
using Elsa.Workflows.Management.Features;
using FluentStorage;
using FluentStorage.Blobs;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.WorkflowProviders.FluentStorage.Features;

/// <summary>
/// A feature that enables the FluentStorage workflow definition provider.
/// </summary>
[DependsOn(typeof(WorkflowManagementFeature))]
[PublicAPI]
public class FluentStorageFeature : FeatureBase
{
    /// <inheritdoc />
    public FluentStorageFeature(IModule module) : base(module)
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
        Services.AddWorkflowDefinitionProvider<FluentStorageWorkflowDefinitionProvider>();
    }

    private static string GetDefaultWorkflowsDirectory()
    {
        var entryAssemblyDir = Path.GetDirectoryName(Assembly.GetEntryAssembly()!.Location)!;
        var directory = Path.Combine(entryAssemblyDir, "Workflows");
        return directory;
    }

}