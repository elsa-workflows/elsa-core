using CShells.Features;
using Elsa.Dsl.ElsaScript.ShellFeatures;
using Elsa.WorkflowProviders.BlobStorage.Contracts;
using Elsa.WorkflowProviders.BlobStorage.ElsaScript.Handlers;
using Elsa.WorkflowProviders.BlobStorage.ShellFeatures;
using Elsa.Platform.PackageManifest.Generator.Hints;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.WorkflowProviders.BlobStorage.ElsaScript.ShellFeatures;

/// <summary>
/// A feature that enables ElsaScript support for the BlobStorage workflow provider.
/// </summary>
[ManifestFeatureCategory(ManifestFeatureCategories.Storage)]
[ManifestFeatureCategory(ManifestFeatureCategories.Scripting)]
[ManifestFeatureCategory(ManifestFeatureCategories.Workflows)]
[ShellFeature(
    DisplayName = "ElsaScript Blob Storage",
    Description = "Provides ElsaScript format support for the BlobStorage workflow provider",
    DependsOn = [typeof(BlobStorageFeature), typeof(ElsaScriptFeature)])]
[UsedImplicitly]
public class ElsaScriptBlobStorageFeature : IShellFeature
{
    public void ConfigureServices(IServiceCollection services)
    {
        // Register the ElsaScript format handler
        services.AddScoped<IBlobWorkflowFormatHandler, ElsaScriptBlobWorkflowFormatHandler>();
    }
}

