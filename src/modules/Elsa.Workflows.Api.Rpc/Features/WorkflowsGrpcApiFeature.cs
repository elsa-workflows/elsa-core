using Elsa.Features.Abstractions;
using Elsa.Features.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.Api.Rpc.Features;

/// <summary>
/// Adds workflows API features.
/// </summary>
public class WorkflowsGrpcApiFeature : FeatureBase
{
    /// <inheritdoc />
    public WorkflowsGrpcApiFeature(IModule module) : base(module)
    {
    }

    /// <inheritdoc />
    public override void Configure()
    {
        
    }

    /// <inheritdoc />
    public override void Apply()
    {
        Services.AddGrpc().AddJsonTranscoding();
    }
}