using Elsa.Workflows;
using Elsa.Workflows.Runtime.Helpers;
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
        WorkflowTypeValidator.Validate(workflowType);

        var key = workflowType.GetSimpleAssemblyQualifiedName();
        var legacyKey = workflowType.FullName;
        var factory = new Func<IServiceProvider, ValueTask<IWorkflow>>(sp =>
        {
            var workflow = (IWorkflow)ActivatorUtilities.GetServiceOrCreateInstance(sp, workflowType);
            return new ValueTask<IWorkflow>(workflow);
        });

        dictionary.Add(key, factory);

        if (!string.IsNullOrWhiteSpace(legacyKey) && legacyKey != key)
            dictionary.Add(legacyKey, factory);

        if (dictionary is IWorkflowTypeRegistry workflowTypeRegistry)
            workflowTypeRegistry.AddWorkflowType(workflowType);
    }
}
