using Elsa.WorkflowContexts;
using Elsa.Workflows.Management.Entities;

// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

/// <summary>
/// Adds extension methods to <see cref="WorkflowDefinition"/>.
/// </summary>
public static class WorkflowDefinitionExtensions
{
    /// <summary>
    /// Gets the workflow context provider types that are installed on the workflow definition.
    /// </summary>
    /// <param name="workflowDefinition">The workflow definition to get the provider types from.</param>
    /// <returns>The workflow context provider types.</returns>
    public static IEnumerable<Type> GetWorkflowContextProviderTypes(this WorkflowDefinition workflowDefinition) => 
        workflowDefinition.CustomProperties.GetOrAdd(Constants.WorkflowContextProviderTypesKey, () => new List<Type>());
}