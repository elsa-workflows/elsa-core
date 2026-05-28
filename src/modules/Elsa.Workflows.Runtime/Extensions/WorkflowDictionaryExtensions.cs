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
        // FullName should never be null here, as we filter out generic types
        dictionary.Add(typeof(TWorkflow).FullName!, sp =>
        {
            var workflow = ActivatorUtilities.GetServiceOrCreateInstance<TWorkflow>(sp);
            return new ValueTask<IWorkflow>(workflow);
        });
    }
    
    /// <summary>
    /// Register a workflow factory that creates an instance of the specified workflow type. 
    /// </summary>
    public static void Add(this IDictionary<string, Func<IServiceProvider, ValueTask<IWorkflow>>> dictionary, Type workflowType)
    {
        // FullName should never be null here, as we filter out generic types
        dictionary.Add(workflowType.FullName!, sp =>
        {
            var workflow = (IWorkflow)ActivatorUtilities.GetServiceOrCreateInstance(sp, workflowType);
            return new ValueTask<IWorkflow>(workflow);
        });
    }
}