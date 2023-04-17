using Elsa.WorkflowContexts.Contracts;
using Microsoft.Extensions.DependencyInjection;

// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

/// <summary>
/// Extension methods for <see cref="IServiceCollection"/> to add workflow context providers.
/// </summary>
public static class DependencyInjectionExtensions
{
    /// <summary>
    /// Adds a workflow context provider.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <typeparam name="T">The type of the workflow context provider.</typeparam>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddWorkflowContextProvider<T>(this IServiceCollection services) where T : class, IWorkflowContextProvider
    {
        return services.AddSingleton<IWorkflowContextProvider, T>();
    }
}