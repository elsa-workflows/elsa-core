using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Elsa.Extensions;
using Elsa.Workflows;
using Elsa.Workflows.Extensions;
using Elsa.Workflows.Options;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.Discovery;
using Elsa.Workflows.Runtime.Options;
using Elsa.Workflows.Runtime.Providers;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Provides extension methods for <see cref="IServiceCollection"/>.
/// </summary>
public static class DependencyInjectionExtensions
{
    /// <summary>
    /// Registers and validates <see cref="GracefulShutdownOptions"/>.
    /// </summary>
    public static IServiceCollection AddGracefulShutdownOptions(this IServiceCollection services, Action<GracefulShutdownOptions>? configure = null)
    {
        var builder = services.AddOptions<GracefulShutdownOptions>().ValidateOnStart();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IValidateOptions<GracefulShutdownOptions>, Elsa.Extensions.ValidateGracefulShutdownOptions>());

        if (configure != null)
            builder.Configure(configure);


        return services;
    }

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
    /// <typeparam name="T">The type of the workflows provider to add. Must implement <see cref="IWorkflowsProvider"/>.</typeparam>
    public static IServiceCollection AddWorkflowsProvider<T>(this IServiceCollection services) where T : class, IWorkflowsProvider => services.AddScoped<IWorkflowsProvider, T>();

    /// <summary>
    /// Registers the specified code-first workflow type.
    /// </summary>
    public static IServiceCollection AddWorkflow<T>(this IServiceCollection services) where T : IWorkflow => services.AddWorkflow(typeof(T));

    /// <summary>
    /// Registers the specified code-first workflow type.
    /// </summary>
    public static IServiceCollection AddWorkflow(this IServiceCollection services, Type workflowType)
    {
        AddWorkflowRegistration(services, workflowType);
        return services;
    }

    /// <summary>
    /// Registers all code-first workflows contained in the assembly containing the specified marker type.
    /// </summary>
    [RequiresUnreferencedCode("The assembly is required to be referenced.")]
    public static IServiceCollection AddWorkflowsFrom<TMarker>(this IServiceCollection services) => services.AddWorkflowsFrom(typeof(TMarker).Assembly);

    /// <summary>
    /// Registers all code-first workflows in the specified assembly.
    /// </summary>
    [RequiresUnreferencedCode("The assembly is required to be referenced.")]
    public static IServiceCollection AddWorkflowsFrom(this IServiceCollection services, Assembly assembly)
    {
        foreach (var workflowType in WorkflowTypeScanner.GetWorkflowTypes(assembly))
            AddWorkflowRegistration(services, workflowType);

        return services;
    }

    private static void AddWorkflowRegistration(IServiceCollection services, Type workflowType)
    {
        services.PostConfigure<RuntimeOptions>(options => options.Workflows.Add(workflowType));
        services.Configure<WorkflowJsonTypeOptions>(options => options.AddSimpleAssemblyQualifiedTypeAlias(workflowType));
    }
}
