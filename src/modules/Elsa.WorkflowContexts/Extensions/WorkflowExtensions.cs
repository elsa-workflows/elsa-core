using Elsa.WorkflowContexts;
using Elsa.Workflows.Activities;

// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

/// <summary>
/// Adds extension methods to <see cref="Workflow"/>.
/// </summary>
public static class WorkflowExtensions
{
    /// <summary>
    /// Gets the workflow context provider types that are installed on the workflow.
    /// </summary>
    /// <param name="workflow">The workflow to get the provider types from.</param>
    /// <returns>The workflow context provider types.</returns>
    public static IEnumerable<Type> GetWorkflowContextProviderTypes(this Workflow workflow)
    {
        var contextProviderTypes = workflow.PropertyBag.GetOrAdd(Constants.WorkflowContextProviderTypesKey, () => new List<String>());

        var result = new List<Type>();

        foreach (var type in contextProviderTypes)
        {
            var targetType = Type.GetType(type);

            if (targetType != null)
            {
                result.Add(targetType);
            }
        }

        return result;
    }
}