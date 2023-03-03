using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Core.Services;
using Microsoft.Extensions.DependencyInjection;

// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

public static class WorkflowDictionaryExtensions
{
    /// <summary>
    /// Register a workflow factory that creates an instance of the specified workflow type. 
    /// </summary>
    public static void Add<TWorkflow>(this IDictionary<string, Func<IServiceProvider, ValueTask<IWorkflow>>> dictionary) where TWorkflow : IWorkflow => dictionary.Add(typeof(TWorkflow).Name, sp =>
    {
        var workflow = ActivatorUtilities.GetServiceOrCreateInstance<TWorkflow>(sp);
        return new ValueTask<IWorkflow>(workflow);
    });
}