using Elsa.Workflows.Core.Contracts;
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
        dictionary.Add(typeof(TWorkflow).Name, sp =>
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
        dictionary.Add(workflowType.Name, sp =>
        {
            var workflow = (IWorkflow)ActivatorUtilities.GetServiceOrCreateInstance(sp, workflowType);
            return new ValueTask<IWorkflow>(workflow);
        });
    }
}