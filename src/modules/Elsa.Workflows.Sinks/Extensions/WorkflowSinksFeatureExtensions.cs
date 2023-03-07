using Elsa.Workflows.Sinks.Contracts;
using Elsa.Workflows.Sinks.Features;
using Microsoft.Extensions.DependencyInjection;

// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

/// <summary>
/// Provides helpful extensions to install the workflow sinks feature into the module.
/// </summary>
public static class WorkflowSinksFeatureExtensions
{
    /// <summary>
    /// Enables the workflow sinks feature of the specified module.
    /// </summary>
    public static WorkflowSinksFeature AddWorkflowSink<T>(this WorkflowSinksFeature feature) where T: class, IWorkflowSink
    {
        feature.Services.AddSingleton<IWorkflowSink, T>();
        return feature;
    }
}