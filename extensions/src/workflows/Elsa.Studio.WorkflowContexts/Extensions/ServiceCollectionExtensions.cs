using Elsa.Studio.Contracts;
using Elsa.Studio.WorkflowContexts.Contracts;
using Elsa.Studio.WorkflowContexts.Handlers;
using Elsa.Studio.WorkflowContexts.Services;
using Elsa.Studio.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Studio.WorkflowContexts.Extensions;

/// <summary>
/// Contains extension methods for the <see cref="IServiceCollection"/> interface.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds the workflow vcontexts module to the service collection.
    /// </summary>
    public static IServiceCollection AddWorkflowContextsModule(this IServiceCollection services)
    {
        return services
                .AddScoped<IFeature, Feature>()
                .AddScoped<IWorkflowContextsProvider, RemoteWorkflowContextsProvider>()
                .AddUIHintHandler<WorkflowContextProviderPickerHandler>()
            ;
    }
}