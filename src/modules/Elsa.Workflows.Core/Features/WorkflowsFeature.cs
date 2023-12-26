using Elsa.Common.Contracts;
using Elsa.Common.Features;
using Elsa.Expressions.Features;
using Elsa.Extensions;
using Elsa.Features.Abstractions;
using Elsa.Features.Attributes;
using Elsa.Features.Services;
using Elsa.Workflows.ActivationValidators;
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
using Elsa.Workflows.UIHints.Dropdown;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.Features;

/// <summary>
/// Adds workflow services to the system.
/// </summary>
[DependsOn(typeof(SystemClockFeature))]
[DependsOn(typeof(ExpressionsFeature))]
[DependsOn(typeof(MediatorFeature))]
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
    /// <param name="generator"></param>
    /// <returns></returns>
    public WorkflowsFeature WithIdentityGenerator(Func<IServiceProvider, IIdentityGenerator> generator)
    {
        IdentityGenerator = generator;
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
            .AddSingleton<IActivityInvoker, ActivityInvoker>()
            .AddSingleton<IWorkflowRunner, WorkflowRunner>()
            .AddSingleton<IActivityVisitor, ActivityVisitor>()
            .AddSingleton<IIdentityGraphService, IdentityGraphService>()
            .AddSingleton<IWorkflowStateExtractor, WorkflowStateExtractor>()
            .AddSingleton<IActivitySchedulerFactory, ActivitySchedulerFactory>()
            .AddSingleton<IHasher, Hasher>()
            .AddSingleton<IBookmarkHasher, BookmarkHasher>()
            .AddSingleton(IdentityGenerator)
            .AddSingleton<IBookmarkPayloadSerializer>(sp => ActivatorUtilities.CreateInstance<BookmarkPayloadSerializer>(sp))
            .AddSingleton<IActivityDescriber, ActivityDescriber>()
            .AddSingleton<IActivityRegistry, ActivityRegistry>()
            .AddSingleton<IPropertyDefaultValueResolver, PropertyDefaultValueResolver>()
            .AddSingleton<IPropertyUIHandlerResolver, PropertyUIHandlerResolver>()
            .AddSingleton<IActivityFactory, ActivityFactory>()
            .AddTransient<WorkflowBuilder>()
            .AddSingleton(typeof(Func<IWorkflowBuilder>), sp => () => sp.GetRequiredService<WorkflowBuilder>())
            .AddSingleton<IWorkflowBuilderFactory, WorkflowBuilderFactory>()
            .AddSingleton<IVariablePersistenceManager, VariablePersistenceManager>()
            .AddSingleton<IIncidentStrategyResolver, DefaultIncidentStrategyResolver>()

            // Incident Strategies.
            .AddTransient<IIncidentStrategy, FaultStrategy>()
            .AddTransient<IIncidentStrategy, ContinueWithIncidentsStrategy>()

            // Pipelines.
            .AddSingleton<IActivityExecutionPipeline>(sp => new ActivityExecutionPipeline(sp, ActivityExecutionPipeline))
            .AddSingleton<IWorkflowExecutionPipeline>(sp => new WorkflowExecutionPipeline(sp, WorkflowExecutionPipeline))

            // Built-in activity services.
            .AddSingleton<IActivityResolver, PropertyBasedActivityResolver>()
            .AddSingleton<IActivityResolver, SwitchActivityResolver>()
            .AddSingleton<ISerializationOptionsConfigurator, AdditionalConvertersConfigurator>()
            .AddSingleton<ISerializationOptionsConfigurator, CustomConstructorConfigurator>()

            // Domain event handlers.
            .AddHandlersFrom<WorkflowsFeature>()

            // Stream providers.
            .AddSingleton(StandardInStreamProvider)
            .AddSingleton(StandardOutStreamProvider)

            // Storage drivers.
            .AddSingleton<IStorageDriverManager, StorageDriverManager>()
            .AddStorageDriver<WorkflowStorageDriver>()
            .AddStorageDriver<MemoryStorageDriver>()

            // Serialization.
            .AddSingleton<IWorkflowStateSerializer, JsonWorkflowStateSerializer>()
            .AddSingleton<IPayloadSerializer, JsonPayloadSerializer>()
            .AddSingleton<IActivitySerializer, JsonActivitySerializer>()
            .AddSingleton<IApiSerializer, ApiSerializer>()
            .AddSingleton<ISafeSerializer, SafeSerializer>()

            // Instantiation strategies.
            .AddSingleton<IWorkflowActivationStrategy, AllowAlwaysStrategy>()
            
            // UI hints.
            .AddSingleton<IUIHintHandler, DropDownUIHintHandler>()

            // Logging
            .AddLogging();
    }
}