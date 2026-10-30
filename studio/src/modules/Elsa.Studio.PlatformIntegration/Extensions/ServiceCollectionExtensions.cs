using Elsa.Studio.Contracts;
using Elsa.Studio.Extensions;
using Elsa.Studio.PlatformIntegration.Contracts;
using Elsa.Studio.PlatformIntegration.Handlers;
using Elsa.Studio.PlatformIntegration.Services;
using Elsa.Studio.PlatformIntegration.Widgets;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Studio.PlatformIntegration.Extensions;

/// <summary>
/// Contains extension methods for the Elsa Platform integration module.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds the Elsa Platform integration module.
    /// </summary>
    public static IServiceCollection AddPlatformIntegrationModule(this IServiceCollection services, Action<PlatformSubmitOptions>? configure = null)
    {
        services.Configure(configure ?? (_ => { }));
        services.AddHttpClient<IPlatformArtifactSubmitClient, PlatformArtifactSubmitClient>();

        return services
            .AddScoped<IFeature, Feature>()
            .AddScoped<IWidget, WorkflowDefinitionEditorSubmitWidget>()
            .AddScoped<IWidget, WorkflowDefinitionListBulkSubmitWidget>()
            .AddScoped<IWidget, WorkflowDefinitionListRowSubmitWidget>()
            .AddScoped<IPlatformWorkflowSnapshotPackager, PlatformWorkflowSnapshotPackager>()
            .AddScoped<IPlatformWorkflowSubmissionService, PlatformWorkflowSubmissionService>()
            .AddNotificationHandler<SubmitWorkflowDefinitionToPlatform>();
    }
}
