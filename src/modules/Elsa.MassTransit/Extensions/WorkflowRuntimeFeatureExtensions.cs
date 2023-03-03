using Elsa.Features.Services;
using Elsa.MassTransit.Features;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.Features;

// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

/// <summary>
/// Provides extensions to <see cref="IModule"/> that enables and configures the workflow runtime to use an implementation of <see cref="IWorkflowDispatcher"/> using MassTransit.
/// </summary>
public static class WorkflowRuntimeFeatureExtensions
{
    /// <summary>
    /// Enable and configure MassTransit.
    /// </summary>
    public static WorkflowRuntimeFeature UseMassTransitDispatcher(this WorkflowRuntimeFeature feature)
    {
        feature.Module.Configure<MassTransitWorkflowDispatcherFeature>();
        return feature;
    }
}