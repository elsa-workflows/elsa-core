using Elsa.Workflows.Api.Features;

// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

/// <summary>
/// Provides extensions to <see cref="WorkflowsApiFeature"/>.
/// </summary>
public static class WorkflowsApiFeatureExtensions
{
    /// <summary>
    /// Adds FastEndpoint endpoints from the assembly containing <typeparamref name="TMarker"/>.
    /// </summary>
    public static WorkflowsApiFeature AddFastEndpointsAssembly<TMarker>(this WorkflowsApiFeature feature)
    {
        feature.Module.AddFastEndpointsAssembly<TMarker>();
        return feature;
    }
}