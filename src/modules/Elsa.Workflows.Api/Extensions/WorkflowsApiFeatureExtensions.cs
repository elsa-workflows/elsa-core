using Elsa.Workflows.Api.Features;

// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

public static class WorkflowsApiFeatureExtensions
{
    public static WorkflowsApiFeature AddFastEndpointsAssembly<TMarker>(this WorkflowsApiFeature feature)
    {
        feature.Module.AddFastEndpointsAssembly<TMarker>();
        return feature;
    }
}