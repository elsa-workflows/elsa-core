using Elsa.WorkflowContexts;
using Elsa.WorkflowContexts.Contracts;
using Elsa.Workflows.Core.Contracts;
using JetBrains.Annotations;

// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

/// <summary>
/// Adds extension methods to <see cref="IWorkflowBuilder"/>.
/// </summary>
[PublicAPI]
public static class WorkflowBuilderExtensions
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
        var providerTypes = workflow.CustomProperties.GetOrAdd(Constants.WorkflowContextProviderTypesKey, () => new List<Type>())!;
        providerTypes.Add(providerType);
        return workflow;
    }
}