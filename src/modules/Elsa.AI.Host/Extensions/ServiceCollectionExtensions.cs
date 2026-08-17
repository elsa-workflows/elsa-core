using Elsa.AI.Abstractions.Contracts;
using Elsa.AI.Host.Context;
using Elsa.AI.Host.Options;
using Elsa.AI.Host.Services;
using Elsa.AI.Host.Streaming;
using Elsa.AI.Host.Tools.Activities;
using Elsa.AI.Host.Tools.Runtime;
using Elsa.AI.Host.Tools.Workflows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAIHostServices(this IServiceCollection services, Action<AIHostOptions>? configureOptions = null)
    {
        if (configureOptions != null)
            services.Configure(configureOptions);

        services.AddLogging();
        services.AddOptions<AIHostOptions>();
        services.TryAddSingleton<InMemoryAIConversationStore>();
        services.TryAddSingleton<IAIConversationStore>(sp => sp.GetRequiredService<InMemoryAIConversationStore>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IAIContextProvider, WorkflowDefinitionContextProvider>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IAIContextProvider, WorkflowInstanceContextProvider>());
        services.TryAddSingleton<AIContextResolver>();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IHostedService, AIContextProviderValidationHostedService>());
        services.TryAddSingleton<AIToolEnablementService>();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IHostedService, AIToolEnablementConfigurationHostedService>());
        services.TryAddSingleton<IAIToolRegistry, AIToolRegistry>();
        services.TryAddSingleton<AIGroundingResultFormatter>();
        services.TryAddSingleton<ActivityGroundingMapper>();
        services.TryAddSingleton<ActivityGroundingSearchService>();
        services.TryAddSingleton<WorkflowGroundingMapper>();
        services.TryAddSingleton<RuntimeGroundingMapper>();
        services.TryAddSingleton<WorkflowDraftValidationService>();
        services.TryAddSingleton<WorkflowProposalDiffService>();
        services.TryAddEnumerable(ServiceDescriptor.Transient<IAITool, ActivitiesSearchTool>());
        services.TryAddEnumerable(ServiceDescriptor.Transient<IAITool, ActivityDescriptorTool>());
        services.TryAddEnumerable(ServiceDescriptor.Transient<IAITool, WorkflowsSearchTool>());
        services.TryAddEnumerable(ServiceDescriptor.Transient<IAITool, WorkflowDefinitionTool>());
        services.TryAddEnumerable(ServiceDescriptor.Transient<IAITool, WorkflowDefinitionGraphTool>());
        services.TryAddEnumerable(ServiceDescriptor.Transient<IAITool, WorkflowUsageSearchTool>());
        services.TryAddEnumerable(ServiceDescriptor.Transient<IAITool, WorkflowValidateDraftTool>());
        services.TryAddEnumerable(ServiceDescriptor.Transient<IAITool, WorkflowProposeCreateTool>());
        services.TryAddEnumerable(ServiceDescriptor.Transient<IAITool, WorkflowProposeUpdateTool>());
        services.TryAddEnumerable(ServiceDescriptor.Transient<IAITool, InstancesSearchTool>());
        services.TryAddEnumerable(ServiceDescriptor.Transient<IAITool, WorkflowInstanceTool>());
        services.TryAddEnumerable(ServiceDescriptor.Transient<IAITool, WorkflowInstanceExecutionHistoryTool>());
        services.TryAddEnumerable(ServiceDescriptor.Transient<IAITool, WorkflowInstanceActivityStateTool>());
        services.TryAddEnumerable(ServiceDescriptor.Transient<IAITool, IncidentsSearchTool>());
        services.TryAddEnumerable(ServiceDescriptor.Transient<IAITool, IncidentTool>());
        services.TryAddScoped<IAIOrchestrator, AIOrchestrator>();
        services.TryAddSingleton<AIStreamSessionManager>();
        services.TryAddSingleton<AIStreamEventMapper>();
        services.TryAddSingleton<AIAuditSink>();
        services.TryAddSingleton<IAIAuditSink>(sp => sp.GetRequiredService<AIAuditSink>());

        return services;
    }
}
