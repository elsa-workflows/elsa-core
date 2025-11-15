using Elsa.Dsl.ElsaScript.Compiler;
using Elsa.Dsl.ElsaScript.Features;
using Elsa.Features.Abstractions;
using Elsa.Features.Attributes;
using Elsa.Features.Services;
using Elsa.WorkflowProviders.BlobStorage.Contracts;
using Elsa.WorkflowProviders.BlobStorage.ElsaScript.Handlers;
using Elsa.WorkflowProviders.BlobStorage.Features;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.WorkflowProviders.BlobStorage.ElsaScript.Features;

/// <summary>
/// A feature that enables ElsaScript support for the BlobStorage workflow provider.
/// </summary>
[DependsOn(typeof(BlobStorageFeature))]
[DependsOn(typeof(ElsaScriptFeature))]
public class ElsaScriptBlobStorageFeature(IModule module) : FeatureBase(module)
{
    /// <inheritdoc />
    public override void Apply()
    {
        // Register the ElsaScript format handler
        Services.AddScoped<IBlobWorkflowFormatHandler, ElsaScriptBlobWorkflowFormatHandler>();
    }
}
