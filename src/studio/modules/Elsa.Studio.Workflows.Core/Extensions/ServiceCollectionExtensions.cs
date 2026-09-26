using Elsa.Studio.Contracts;
using Elsa.Studio.Extensions;
using Elsa.Studio.Services;
using Elsa.Studio.Workflows.Domain.Contracts;
using Elsa.Studio.Workflows.Domain.Providers;
using Elsa.Studio.Workflows.Domain.Services;
using Elsa.Studio.Workflows.Resolvers;
using Elsa.Studio.Workflows.UI.Contracts;
using Elsa.Studio.Workflows.UI.Providers;
using Elsa.Studio.Workflows.UI.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Studio.Workflows.Extensions;

/// <summary>
/// Contains extension methods for the <see cref="IServiceCollection"/> interface.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds the workflows module.
    /// </summary>
    public static IServiceCollection AddWorkflowsCore(this IServiceCollection services)
    {
        services
            .AddScoped<IRemoteFeatureProvider, RemoteFeatureProvider>()
            .AddScoped<IWorkflowDefinitionService, RemoteWorkflowDefinitionService>()
            .AddScoped<IWorkflowRootActivityTemplateProvider, DefaultWorkflowRootActivityTemplateProvider>()
            .AddScoped<IWorkflowInstanceService, RemoteWorkflowInstanceService>()
            .AddScoped<IWorkflowDefinitionEditorService, WorkflowDefinitionEditorService>()
            .AddScoped<IWorkflowDefinitionImporter, WorkflowDefinitionImporter>()
            .AddScoped<IWorkflowDefinitionHistoryService, WorkflowDefinitionHistoryService>()
            .AddScoped<IActivityRegistryProvider, RemoteActivityRegistryProvider>()
            .AddScoped<IActivityExecutionService, RemoteActivityExecutionService>()
            .AddScoped<IActivityRegistry, DefaultActivityRegistry>()
            .AddScoped<IExpressionProvider, RemoteExpressionProvider>()
            .AddScoped<IIdentityGenerator, RandomLongIdentityGenerator>()
            .AddScoped<IActivityNameGenerator, DefaultActivityNameGenerator>()
            .AddScoped<IStorageDriverService, RemoteStorageDriverService>()
            .AddScoped<IServerInformationProvider, RemoteServerInformationProvider>()
            .AddScoped<IVariableTypeService, RemoteVariableTypeService>()
            .AddScoped<IOutputConverterService, RemoteOutputConverterService>()
            .AddScoped<IWorkflowActivationStrategyService, RemoteWorkflowActivationStrategyService>()
            .AddScoped<ILogPersistenceStrategyService, RemoteLogPersistenceStrategyService>()
            .AddScoped<IIncidentStrategiesProvider, RemoteIncidentStrategiesProvider>()
            .AddScoped<ICommitStrategiesProvider, RemoteCommitStrategiesProvider>()
            .AddScoped<IResilienceStrategyCatalog, RemoteResilienceStrategyCatalog>()
            .AddScoped<IDiagramDesignerService, DefaultDiagramDesignerService>()
            .AddScoped<IActivityDisplaySettingsRegistry, DefaultActivityDisplaySettingsRegistry>()
            .AddScoped<IActivityPortService, DefaultActivityPortService>()
            .AddScoped<IActivityVisitor, DefaultActivityVisitor>()
            .AddScoped<IActivityResolver, StateMachineActivityResolver>()
            .AddScoped<IActivityResolver, DefaultActivityResolver>()
            .AddScoped<IWorkflowJsonDetector, CompatWorkflowJsonDetector>()
            .AddScoped<IBpmnInterchangeService, RemoteBpmnInterchangeService>()
            ;

        services.AddActivityDisplaySettingsProvider<DefaultActivityDisplaySettingsProvider>();
        services.AddActivityPortProvider<DefaultActivityPortProvider>();
        
        return services;
    }
    
    /// <summary>
    /// Adds a <see cref="IDiagramDesignerProvider"/> to the service collection.
    /// </summary>
    public static IServiceCollection AddDiagramDesignerProvider<T>(this IServiceCollection services) where T : class, IDiagramDesignerProvider
    {
        services.AddScoped<IDiagramDesignerProvider, T>();
        return services;
    }
    
    /// <summary>
    /// Adds a <see cref="IActivityDisplaySettingsProvider"/> to the service collection.
    /// </summary>
    public static IServiceCollection AddActivityDisplaySettingsProvider<T>(this IServiceCollection services) where T : class, IActivityDisplaySettingsProvider
    {
        services.AddScoped<IActivityDisplaySettingsProvider, T>();
        return services;
    }
    
    /// <summary>
    /// Adds a <see cref="IActivityPortProvider"/> to the service collection.
    /// </summary>
    public static IServiceCollection AddActivityPortProvider<T>(this IServiceCollection services) where T : class, IActivityPortProvider
    {
        services.AddScoped<IActivityPortProvider, T>();
        return services;
    }
}
