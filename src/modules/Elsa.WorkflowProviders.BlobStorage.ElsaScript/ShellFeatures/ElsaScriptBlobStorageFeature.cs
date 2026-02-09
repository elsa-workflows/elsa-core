using CShells.Features;
using Elsa.WorkflowProviders.BlobStorage.Contracts;
using Elsa.WorkflowProviders.BlobStorage.ElsaScript.Handlers;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.WorkflowProviders.BlobStorage.ElsaScript.ShellFeatures;

/// <summary>
/// A feature that enables ElsaScript support for the BlobStorage workflow provider.
/// </summary>
[ShellFeature(
    DisplayName = "ElsaScript BlobStorage Support",
    Description = "Enables ElsaScript format support for BlobStorage workflow provider",
    DependsOn = ["BlobStorage", "ElsaScript"])]
public class ElsaScriptBlobStorageFeature : IShellFeature
{
    public void ConfigureServices(IServiceCollection services)
    {
        // Register the ElsaScript format handler
        services.AddScoped<IBlobWorkflowFormatHandler, ElsaScriptBlobWorkflowFormatHandler>();
    }
}
