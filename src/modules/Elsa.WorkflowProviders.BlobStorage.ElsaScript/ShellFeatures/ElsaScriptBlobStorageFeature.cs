using CShells.Features;
using Elsa.WorkflowProviders.BlobStorage.Contracts;
using Elsa.WorkflowProviders.BlobStorage.ElsaScript.Handlers;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.WorkflowProviders.BlobStorage.ElsaScript.ShellFeatures;

/// <summary>
/// A feature that enables ElsaScript support for the BlobStorage workflow provider.
/// </summary>
[ShellFeature(
    DisplayName = "ElsaScript Blob Storage",
    Description = "Provides ElsaScript format support for the BlobStorage workflow provider",
    DependsOn = ["BlobStorage", "ElsaScript"])]
[UsedImplicitly]
public class ElsaScriptBlobStorageFeature : IShellFeature
{
    public void ConfigureServices(IServiceCollection services)
    {
        // Register the ElsaScript format handler
        services.AddScoped<IBlobWorkflowFormatHandler, ElsaScriptBlobWorkflowFormatHandler>();
    }
}

