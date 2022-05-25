using Elsa.Workflows.Core.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.Runtime.Extensions;

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