using Elsa.Common;
using Elsa.Common.Features;
using Elsa.Common.Serialization;
using Elsa.Expressions.Features;
using Elsa.Extensions;
using Elsa.Features.Abstractions;
using Elsa.Features.Attributes;
using Elsa.Features.Services;
using Elsa.Workflows.ActivationValidators;
using Elsa.Workflows.Builders;
using Elsa.Workflows.CommitStates;
using Elsa.Workflows.IncidentStrategies;
using Elsa.Workflows.LogPersistence;
using Elsa.Workflows.LogPersistence.Strategies;
using Elsa.Workflows.Middleware.Activities;
using Elsa.Workflows.Middleware.Workflows;
using Elsa.Workflows.Pipelines.ActivityExecution;
using Elsa.Workflows.Pipelines.WorkflowExecution;
using Elsa.Workflows.PortResolvers;
using Elsa.Workflows.Serialization.Configurators;
using Elsa.Workflows.Serialization.Helpers;
using Elsa.Workflows.Serialization.Serializers;
using Elsa.Workflows.Services;
using Elsa.Workflows.UIHints.CheckList;
using Elsa.Workflows.UIHints.Dropdown;
using Elsa.Workflows.UIHints.JsonEditor;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.Features;

/// <summary>
/// Adds workflow services to the system.
/// </summary>
[DependsOn(typeof(SystemClockFeature))]
[DependsOn(typeof(ExpressionsFeature))]
[DependsOn(typeof(MediatorFeature))]
[DependsOn(typeof(DefaultFormattersFeature))]
[DependsOn(typeof(MultitenancyFeature))]
[DependsOn(typeof(CommitStrategiesFeature))]
public class WorkflowsFeature : FeatureBase
{
    /// <inheritdoc />
    public WorkflowsFeature(IModule module) : base(module)
    {
    }
    
    /// <summary>
    /// A factory that instantiates a concrete <see cref="IStandardInStreamProvider"/>.
    /// </summary>
    public Func<IServiceProvider, IStandardInStreamProvider> StandardInStreamProvider { get; set; } = _ => new StandardInStreamProvider(Console.In);
    
    /// <summary>
    /// A factory that instantiates a concrete <see cref="IStandardOutStreamProvider"/>.
    /// </summary>
    public Func<IServiceProvider, IStandardOutStreamProvider> StandardOutStreamProvider { get; set; } = _ => new StandardOutStreamProvider(Console.Out);
    
    /// <summary>
    /// A factory that instantiates a concrete <see cref="IIdentityGenerator"/>.
    /// </summary>
    public Func<IServiceProvider, IIdentityGenerator> IdentityGenerator { get; set; } = sp => new RandomLongIdentityGenerator();
    
    /// <summary>
    /// A handler for committing workflow execution state.
    /// </summary>
    public Func<IServiceProvider, ICommitStateHandler> CommitStateHandler { get; set; } = sp => new NoopCommitStateHandler();
    
    /// <summary>
    /// A factory that instantiates a concrete <see cref="ILoggerStateGenerator{WorkflowExecutionContext}"/>.
    /// </summary>
    public Func<IServiceProvider, ILoggerStateGenerator<WorkflowExecutionContext>> WorkflowLoggerStateGenerator { get; set; } = sp => new WorkflowLoggerStateGenerator();

    /// <summary>
    /// A factory that instantiates a concrete <see cref="ILoggerStateGenerator{ActivityExecutionContext}"/>.
    /// </summary>
    public Func<IServiceProvider, ILoggerStateGenerator<ActivityExecutionContext>> ActivityLoggerStateGenerator { get; set; } = sp => new ActivityLoggerStateGenerator();

    /// <summary>
    /// A delegate to configure the <see cref="IWorkflowExecutionPipeline"/>.
    /// </summary>
    public Action<IWorkflowExecutionPipelineBuilder> WorkflowExecutionPipeline { get; set; } = builder => builder
        .UseExceptionHandling()
        .UseDefaultActivityScheduler();
    
    /// <summary>
    /// A delegate to configure the <see cref="IActivityExecutionPipeline"/>.
    /// </summary>
    public Action<IActivityExecutionPipelineBuilder> ActivityExecutionPipeline { get; set; } = builder => builder.UseDefaultActivityInvoker();

    /// <summary>
    /// Fluent method to set <see cref="StandardInStreamProvider"/>.
    /// </summary>
    public WorkflowsFeature WithStandardInStreamProvider(Func<IServiceProvider, IStandardInStreamProvider> provider)
    {
        StandardInStreamProvider = provider;
        return this;
    }
    
    /// <summary>
    /// Fluent method to set <see cref="StandardOutStreamProvider"/>.
    /// </summary>
    public WorkflowsFeature WithStandardOutStreamProvider(Func<IServiceProvider, IStandardOutStreamProvider> provider)
    {
        StandardOutStreamProvider = provider;
        return this;
    }

    /// <summary>
    /// Fluent method to set <see cref="IdentityGenerator"/>.
    /// </summary>
    public WorkflowsFeature WithIdentityGenerator(Func<IServiceProvider, IIdentityGenerator> generator)
    {
        IdentityGenerator = generator;
        return this;
    }

    /// <summary>
    /// Fluent method to set <see cref="ILoggerStateGenerator{WorkflowExecutionContext}"/>.
    /// </summary>
    public WorkflowsFeature WithWorkflowLoggerStateGenerator(Func<IServiceProvider, ILoggerStateGenerator<WorkflowExecutionContext>> generator)
    {
        WorkflowLoggerStateGenerator = generator;
        return this;
    }

    /// <summary>
    /// Fluent method to set <see cref="ILoggerStateGenerator{ActivityExecutionContext}"/>.
    /// </summary>
    public WorkflowsFeature WithActivityLoggerStateGenerator(Func<IServiceProvider, ILoggerStateGenerator<ActivityExecutionContext>> generator)
    {
        ActivityLoggerStateGenerator = generator;
        return this;
    }

    /// <summary>
    /// Fluent method to configure the <see cref="IWorkflowExecutionPipeline"/>.
    /// </summary>
    public WorkflowsFeature WithWorkflowExecutionPipeline(Action<IWorkflowExecutionPipelineBuilder> setup)
    {
        WorkflowExecutionPipeline = setup;
        return this;
    }

    /// <summary>
    /// Fluent method to configure the <see cref="IActivityExecutionPipeline"/>.
    /// </summary>
    public WorkflowsFeature WithActivityExecutionPipeline(Action<IActivityExecutionPipelineBuilder> setup)
    {
        ActivityExecutionPipeline = setup;
        return this;
    }

    /// <inheritdoc />
    public override void Apply()
    {
        AddElsaCore(Services);
    }

    private void AddElsaCore(IServiceCollection services)
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
            .AddScoped(CommitStateHandler)
            .AddSingleton<IHasher, Hasher>()
            .AddSingleton<IStimulusHasher, StimulusHasher>()
            .AddSingleton(IdentityGenerator)
            .AddSingleton<IBookmarkPayloadSerializer>(sp => ActivatorUtilities.CreateInstance<BookmarkPayloadSerializer>(sp))
            .AddSingleton<IActivityDescriber, ActivityDescriber>()
            .AddSingleton<IActivityRegistry, ActivityRegistry>()
            .AddScoped<IActivityRegistryLookupService, ActivityRegistryLookupService>()
            .AddSingleton<IPropertyDefaultValueResolver, PropertyDefaultValueResolver>()
            .AddSingleton<IPropertyUIHandlerResolver, PropertyUIHandlerResolver>()
            .AddSingleton<IActivityFactory, ActivityFactory>()
            .AddTransient<WorkflowBuilder>()
            .AddScoped(typeof(Func<IWorkflowBuilder>), sp => () => sp.GetRequiredService<WorkflowBuilder>())
            .AddScoped<IWorkflowBuilderFactory, WorkflowBuilderFactory>()
            .AddScoped<IVariablePersistenceManager, VariablePersistenceManager>()
            .AddScoped<IIncidentStrategyResolver, DefaultIncidentStrategyResolver>()
            .AddScoped<IActivityStateFilterManager, DefaultActivityStateFilterManager>()
            .AddScoped<IWorkflowInstanceVariableReader, DefaultWorkflowInstanceVariableReader>()
            .AddScoped<IWorkflowInstanceVariableWriter, DefaultWorkflowInstanceVariableWriter>()

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
            .AddScoped(StandardInStreamProvider)
            .AddScoped(StandardOutStreamProvider)

            // Storage drivers.
            .AddScoped<IStorageDriverManager, StorageDriverManager>()
            .AddStorageDriver<WorkflowStorageDriver>()
            .AddStorageDriver<WorkflowInstanceStorageDriver>()
            .AddStorageDriver<MemoryStorageDriver>()

            // Serialization.
            .AddSingleton<IWorkflowStateSerializer, JsonWorkflowStateSerializer>()
            .AddSingleton<IPayloadSerializer, JsonPayloadSerializer>()
            .AddSingleton<IActivitySerializer, JsonActivitySerializer>()
            .AddSingleton<IApiSerializer, ApiSerializer>()
            .AddSingleton<ISafeSerializer, SafeSerializer>()
            .AddSingleton<IJsonSerializer, StandardJsonSerializer>()
            .AddSingleton<SyntheticPropertiesWriter>()
            .AddSingleton<ActivityWriter>()

            // Instantiation strategies.
            .AddScoped<IWorkflowActivationStrategy, AllowAlwaysStrategy>()

            // UI hints.
            .AddScoped<IUIHintHandler, DropDownUIHintHandler>()
            .AddScoped<IUIHintHandler, CheckListUIHintHandler>()
            .AddScoped<IUIHintHandler, JsonEditorUIHintHandler>()

            // UI property handlers.
            .AddScoped<IPropertyUIHandler, StaticCheckListOptionsProvider>()
            .AddScoped<IPropertyUIHandler, StaticDropDownOptionsProvider>()
            .AddScoped<IPropertyUIHandler, JsonCodeOptionsProvider>()

            // Logger state generators.
            .AddSingleton(WorkflowLoggerStateGenerator)
            .AddSingleton(ActivityLoggerStateGenerator)

            // Log Persistence Strategies.
            .AddScoped<ILogPersistenceStrategyService, DefaultLogPersistenceStrategyService>()
            .AddScoped<ILogPersistenceStrategy, Include>()
            .AddScoped<ILogPersistenceStrategy, Exclude>()
            .AddScoped<ILogPersistenceStrategy, Inherit>()
            .AddScoped<ILogPersistenceStrategy, Configuration>()

            // Logging
            .AddLogging();
    }
}