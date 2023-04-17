using Elsa.WorkflowContexts.Contracts;
using Elsa.Workflows.Core.Contracts;

// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

/// <summary>
/// Adds extension methods to <see cref="IWorkflowBuilder"/>.
/// </summary>
public static class WorkflowExtensions
{
    /// <summary>
    /// Installs the specified workflow context provider type into the specified workflow.
    /// </summary>
    /// <param name="workflow">The workflow to add the provider to.</param>
    /// <typeparam name="T">The type of the provider to add.</typeparam>
    public static IWorkflowBuilder AddWorkflowContextProvider<T>(this IWorkflowBuilder workflow) where T : IWorkflowContextProvider
    {
        return workflow.AddWorkflowContextProvider(typeof(T));
    }

    /// <summary>
    /// Installs the specified workflow context provider type into the specified workflow.
    /// </summary>
    /// <param name="workflow">The workflow to add the provider to.</param>
    /// <param name="providerType">The type of the provider to add.</param>
    public static IWorkflowBuilder AddWorkflowContextProvider(this IWorkflowBuilder workflow, Type providerType)
    {
        var providerTypes = workflow.CustomProperties.GetOrAdd("Elsa:WorkflowContexts", () => new List<Type>())!;
        providerTypes.Add(providerType);
        return workflow;
    }
}