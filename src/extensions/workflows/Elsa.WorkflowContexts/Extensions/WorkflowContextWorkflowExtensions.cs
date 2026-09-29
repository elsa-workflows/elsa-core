using Elsa.WorkflowContexts;
using Elsa.Workflows.Activities;

// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

/// <summary>
/// Adds extension methods to <see cref="Workflow"/>.
/// </summary>
public static class WorkflowContextWorkflowExtensions
{
    /// <summary>
    /// Gets the workflow context provider types that are installed on the workflow.
    /// </summary>
    /// <param name="workflow">The workflow to get the provider types from.</param>
    /// <returns>The workflow context provider types.</returns>
    public static IEnumerable<Type> GetWorkflowContextProviderTypes(this Workflow workflow)
    {
        var contextProviderTypes = workflow.CustomProperties.GetOrAdd(Constants.WorkflowContextProviderTypesKey, () => new List<object>());
        var providerTypes = contextProviderTypes.Select(x => Type.GetType(x.ToString()!)).Where(x => x != null).Select(x => x!).ToList();
        
        return providerTypes;
    }
}