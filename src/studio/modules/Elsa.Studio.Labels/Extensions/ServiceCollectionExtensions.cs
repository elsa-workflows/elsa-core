using Elsa.Studio.Contracts;
using Elsa.Studio.Extensions;
using Elsa.Studio.Labels;
using Elsa.Studio.Labels.Client;
using Elsa.Studio.Labels.Contracts;
using Elsa.Studio.Labels.Menu;
using Elsa.Studio.Models;
using Elsa.Studio.WorkflowContexts.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Studio.Labels;

/// <summary>
/// Contains extension methods for the <see cref="IServiceCollection"/> interface.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds the Labels module.
    /// </summary>
    public static IServiceCollection AddLabelsModule(this IServiceCollection services, BackendApiConfig backendApiConfig)
    {
        return services
            .AddScoped<IFeature, Feature>()
            .AddScoped<IMenuProvider, LabelsMenu>()
            .AddScoped<IWorkflowDefinitionLabelsProvider, RemoteWorkflowDefinitionLabelsProvider>()
            .AddRemoteApi<IWorkflowDefinitionLabelsApi>(backendApiConfig)
            .AddRemoteApi<ILabelsApi>(backendApiConfig);
    }
}