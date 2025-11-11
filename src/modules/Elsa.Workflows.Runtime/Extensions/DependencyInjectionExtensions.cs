using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.Providers;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Provides extension methods for <see cref="IServiceCollection"/>.
/// </summary>
public static class DependencyInjectionExtensions
{
    /// <summary>
    /// Adds the specified workflow provider type to the service collection.
    /// </summary>
    /// <typeparam name="T">The type of the workflow provider to add. Must implement <see cref="IWorkflowsProvider"/>.</typeparam>
    [Obsolete("Use AddWorkflowsProvider instead.", false)]
    public static IServiceCollection AddWorkflowDefinitionProvider<T>(this IServiceCollection services) where T : class, IWorkflowsProvider => services.AddScoped<IWorkflowsProvider, T>();
    /// <summary>
    /// Registers a <see cref="ITriggerPayloadValidator{TPayload}"/> with the service container.
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <typeparam name="TValidator">Validator of the validator</typeparam>
    /// <typeparam name="TPayload">Payload type</typeparam>
    public static IServiceCollection AddTriggerPayloadValidator<TValidator, TPayload>(this IServiceCollection services)
        where TValidator : class, ITriggerPayloadValidator<TPayload>
    {
        return services.AddScoped<ITriggerPayloadValidator<TPayload>, TValidator>();
    }

    /// <summary>
    /// Adds the specified workflows provider type to the service collection.
    /// </summary>
    /// <typeparam name="T">The type of the workflow provider to add. Must implement <see cref="IWorkflowsProvider"/>.</typeparam>
    public static IServiceCollection AddWorkflowsProvider<T>(this IServiceCollection services) where T : class, IWorkflowsProvider => services.AddScoped<IWorkflowsProvider, T>();
}