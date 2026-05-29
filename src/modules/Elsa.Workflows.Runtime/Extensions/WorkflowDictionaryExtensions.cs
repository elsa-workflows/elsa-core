using Elsa.Workflows;
using Microsoft.Extensions.DependencyInjection;

// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

/// <summary>
/// Extension methods for <see cref="IDictionary{TKey,TValue}"/>.
/// </summary>
public static class WorkflowDictionaryExtensions
{
    /// <summary>
    /// Register a workflow factory that creates an instance of the specified workflow type. 
    /// </summary>
    public static void Add<TWorkflow>(this IDictionary<string, Func<IServiceProvider, ValueTask<IWorkflow>>> dictionary) where TWorkflow : IWorkflow
    {
        dictionary.Add(typeof(TWorkflow));
    }
    
    /// <summary>
    /// Register a workflow factory that creates an instance of the specified workflow type. 
    /// </summary>
    public static void Add(this IDictionary<string, Func<IServiceProvider, ValueTask<IWorkflow>>> dictionary, Type workflowType)
    {
        ValidateWorkflowType(workflowType);

        var key = workflowType.FullName;
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException($"Workflow type '{workflowType.Name}' must have a full name.", nameof(workflowType));

        dictionary[key] = sp =>
        {
            var workflow = (IWorkflow)ActivatorUtilities.GetServiceOrCreateInstance(sp, workflowType);
            return new ValueTask<IWorkflow>(workflow);
        };
    }

    private static void ValidateWorkflowType(Type workflowType)
    {
        if (!typeof(IWorkflow).IsAssignableFrom(workflowType))
            throw new ArgumentException($"Workflow type '{GetDisplayName(workflowType)}' must implement {nameof(IWorkflow)}.", nameof(workflowType));

        if (workflowType.IsAbstract || workflowType.IsInterface || workflowType.ContainsGenericParameters)
            throw new ArgumentException($"Workflow type '{GetDisplayName(workflowType)}' must be a concrete, closed type.", nameof(workflowType));
    }

    private static string GetDisplayName(Type type) => type.FullName ?? type.Name;
}
