using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Providers;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Provides extension methods for <see cref="IServiceCollection"/>.
/// </summary>
public static class DependencyInjectionExtensions
{
    /// <summary>
    /// Adds the <see cref="ClrWorkflowsProvider"/> to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <typeparam name="T">The type of the workflow definition provider.</typeparam>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddWorkflowDefinitionProvider<T>(this IServiceCollection services) where T : class, IWorkflowsProvider => services.AddScoped<IWorkflowsProvider, T>();
}