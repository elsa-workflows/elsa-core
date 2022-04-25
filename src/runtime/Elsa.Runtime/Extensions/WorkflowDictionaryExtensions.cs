using Elsa.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Runtime.Extensions;

public static class WorkflowDictionaryExtensions
{
    /// <summary>
    /// Register a workflow factory that creates an instance of the specified workflow type. 
    /// </summary>
    public static void Add<TWorkflow>(this IDictionary<string, Func<IServiceProvider, IWorkflow>> dictionary) where TWorkflow : IWorkflow => dictionary.Add(typeof(TWorkflow).Name, sp => ActivatorUtilities.GetServiceOrCreateInstance<TWorkflow>(sp));
}