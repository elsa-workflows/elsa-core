using Elsa.Common.Contracts;
using Elsa.Common.Serialization;
using Elsa.Extensions;
using Elsa.Framework;
using Elsa.Framework.Shells;
using Elsa.Workflows.ActivationValidators;
using Elsa.Workflows.Activities.Flowchart.Serialization;
using Elsa.Workflows.Builders;
using Elsa.Workflows.Contracts;
using Elsa.Workflows.IncidentStrategies;
using Elsa.Workflows.Middleware.Activities;
using Elsa.Workflows.Middleware.Workflows;
using Elsa.Workflows.Pipelines.ActivityExecution;
using Elsa.Workflows.Pipelines.WorkflowExecution;
using Elsa.Workflows.PortResolvers;
using Elsa.Workflows.Serialization.Configurators;
using Elsa.Workflows.Serialization.Serializers;
using Elsa.Workflows.Services;
using Elsa.Workflows.UIHints.CheckList;
using Elsa.Workflows.UIHints.Dropdown;
using Elsa.Workflows.UIHints.JsonEditor;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.Features;

public class WorkflowsShellFeature : ShellFeature
{
    private Action<IWorkflowExecutionPipelineBuilder> WorkflowExecutionPipeline { get; set; } = builder => builder
        .UseExceptionHandling()
        .UseDefaultActivityScheduler();

    private Action<IActivityExecutionPipelineBuilder> ActivityExecutionPipeline { get; set; } = builder => builder.UseDefaultActivityInvoker();
    
    public override void ConfigureServices(IServiceCollection services)
    {
        services

            // Core.
            .AddScoped<IActivityInvoker, ActivityInvoker>()
            .AddScoped<IWorkflowRunner, WorkflowRunner>()
            .AddScoped<IActivityVisitor, ActivityVisitor>()
            .AddScoped<IIdentityGraphService, IdentityGraphService>()
            .AddScoped<IWorkflowGraphBuilder, WorkflowGraphBuilder>()
            .AddScoped<IWorkflowStateExtractor, WorkflowStateExtractor>()
            .AddScoped<IActivitySchedulerFactory, ActivitySchedulerFactory>()
            .AddSingleton<IHasher, Hasher>()
            .AddSingleton<IStimulusHasher, StimulusHasher>()
            .AddSingleton<RandomLongIdentityGenerator>()
            .AddSingleton<IBookmarkPayloadSerializer>(sp => ActivatorUtilities.CreateInstance<BookmarkPayloadSerializer>(sp))
            .AddSingleton<IActivityDescriber, ActivityDescriber>()
            .AddSingleton<IActivityRegistry, ActivityRegistry>()
            .AddSingleton<IPropertyDefaultValueResolver, PropertyDefaultValueResolver>()
            .AddSingleton<IPropertyUIHandlerResolver, PropertyUIHandlerResolver>()
            .AddSingleton<IActivityFactory, ActivityFactory>()
            .AddTransient<WorkflowBuilder>()
            .AddScoped(typeof(Func<IWorkflowBuilder>), sp => () => sp.GetRequiredService<WorkflowBuilder>())
            .AddScoped<IWorkflowBuilderFactory, WorkflowBuilderFactory>()
            .AddScoped<IVariablePersistenceManager, VariablePersistenceManager>()
            .AddScoped<IIncidentStrategyResolver, DefaultIncidentStrategyResolver>()

            // Incident Strategies.
            .AddTransient<IIncidentStrategy, FaultStrategy>()
            .AddTransient<IIncidentStrategy, ContinueWithIncidentsStrategy>()

            // Pipelines.
            .AddScoped<IActivityExecutionPipeline>(sp => new ActivityExecutionPipeline(sp, ActivityExecutionPipeline))
            .AddScoped<IWorkflowExecutionPipeline>(sp => new WorkflowExecutionPipeline(sp, WorkflowExecutionPipeline))

            // Built-in activity services.
            .AddScoped<IActivityResolver, PropertyBasedActivityResolver>()
            .AddScoped<IActivityResolver, SwitchActivityResolver>()
            .AddSerializationOptionsConfigurator<AdditionalConvertersConfigurator>()
            .AddSerializationOptionsConfigurator<CustomConstructorConfigurator>()

            // Domain event handlers.
            .AddHandlersFrom<WorkflowsFeature>()

            // Stream providers.
            .AddScoped(_ => new StandardInStreamProvider(Console.In))
            .AddScoped(_ => new StandardOutStreamProvider(Console.Out))

            // Storage drivers.
            .AddScoped<IStorageDriverManager, StorageDriverManager>()
            .AddStorageDriver<WorkflowStorageDriver>()
            .AddStorageDriver<MemoryStorageDriver>()

            // Serialization.
            .AddSingleton<IWorkflowStateSerializer, JsonWorkflowStateSerializer>()
            .AddSingleton<IPayloadSerializer, JsonPayloadSerializer>()
            .AddSingleton<IActivitySerializer, JsonActivitySerializer>()
            .AddSingleton<IApiSerializer, ApiSerializer>()
            .AddSingleton<ISafeSerializer, SafeSerializer>()
            .AddSingleton<IJsonSerializer, StandardJsonSerializer>()
            .AddSerializationOptionsConfigurator<FlowchartSerializationOptionConfigurator>()

            // Instantiation strategies.
            .AddScoped<IWorkflowActivationStrategy, AllowAlwaysStrategy>()
            
            // UI hints.
            .AddScoped<IUIHintHandler, DropDownUIHintHandler>()
            .AddScoped<IUIHintHandler, CheckListUIHintHandler>()
            .AddScoped<IUIHintHandler, JsonEditorUIHintHandler>()

            // Logging
            .AddLogging()
            ;
    }
}