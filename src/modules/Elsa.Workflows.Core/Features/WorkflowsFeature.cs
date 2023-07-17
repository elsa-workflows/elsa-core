using Elsa.Common.Features;
using Elsa.Expressions;
using Elsa.Expressions.Features;
using Elsa.Extensions;
using Elsa.Features.Abstractions;
using Elsa.Features.Attributes;
using Elsa.Features.Services;
using Elsa.Workflows.Core.ActivationValidators;
using Elsa.Workflows.Core.Builders;
using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Core.Expressions;
using Elsa.Workflows.Core.Middleware.Activities;
using Elsa.Workflows.Core.Middleware.Workflows;
using Elsa.Workflows.Core.Pipelines.ActivityExecution;
using Elsa.Workflows.Core.Pipelines.WorkflowExecution;
using Elsa.Workflows.Core.PortResolvers;
using Elsa.Workflows.Core.Serialization;
using Elsa.Workflows.Core.Serialization.Providers;
using Elsa.Workflows.Core.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.Core.Features;

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
    /// A delegate to configure the <see cref="IWorkflowExecutionPipeline"/>.
    /// </summary>
    public Action<IWorkflowExecutionPipelineBuilder> WorkflowExecutionPipeline { get; set; } = builder => builder.UseDefaultActivityScheduler();
    
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
        AddExpressions(Services);
    }

    private void AddElsaCore(IServiceCollection services)
    {
        services

            // Core.
            .AddSingleton<IActivityInvoker, ActivityInvoker>()
            .AddSingleton<IWorkflowRunner, WorkflowRunner>()
            .AddSingleton<IActivityVisitor, ActivityVisitor>()
            .AddSingleton<IIdentityGraphService, IdentityGraphService>()
            .AddSingleton<IWorkflowExecutionContextMapper, WorkflowExecutionContextMapper>()
            .AddSingleton<IActivitySchedulerFactory, ActivitySchedulerFactory>()
            .AddSingleton<IHasher, Hasher>()
            .AddSingleton<IBookmarkHasher, BookmarkHasher>()
            .AddSingleton<IIdentityGenerator, GuidIdentityGenerator>()
            .AddSingleton<IWorkflowExecutionContextFactory, DefaultWorkflowExecutionContextFactory>()
            .AddSingleton<IBookmarkPayloadSerializer>(sp => ActivatorUtilities.CreateInstance<BookmarkPayloadSerializer>(sp))
            .AddSingleton<IActivityDescriber, ActivityDescriber>()
            .AddSingleton<IActivityRegistry, ActivityRegistry>()
            .AddSingleton<IPropertyDefaultValueResolver, PropertyDefaultValueResolver>()
            .AddSingleton<IPropertyOptionsResolver, PropertyOptionsResolver>()
            .AddSingleton<IActivityFactory, ActivityFactory>()
            .AddTransient<WorkflowBuilder>()
            .AddSingleton(typeof(Func<IWorkflowBuilder>), sp => () => sp.GetRequiredService<WorkflowBuilder>())
            .AddSingleton<IWorkflowBuilderFactory, WorkflowBuilderFactory>()
            .AddSingleton<IVariablePersistenceManager, VariablePersistenceManager>()

            // Pipelines.
            .AddSingleton<IActivityExecutionPipeline>(sp => new ActivityExecutionPipeline(sp, ActivityExecutionPipeline))
            .AddSingleton<IWorkflowExecutionPipeline>(sp => new WorkflowExecutionPipeline(sp, WorkflowExecutionPipeline))

            // Built-in activity services.
            .AddSingleton<IActivityPortResolver, OutboundActivityPortResolver>()
            .AddSingleton<IActivityPortResolver, SwitchActivityPortResolver>()
            .AddSingleton<ISerializationOptionsConfigurator, CustomSerializationOptionConfigurator>()
            
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
            .AddSingleton<IActivityStateSerializer, ActivityStateSerializer>()
            .AddSingleton<ISerializationProvider, BasicSerializationProvider>()
            
            // Instantiation strategies.
            .AddSingleton<IWorkflowActivationStrategy, AllowAlwaysStrategy>()

            // Logging
            .AddLogging();
    }

    private void AddExpressions(IServiceCollection services)
    {
        services
            .AddExpressionHandler<LiteralExpressionHandler, LiteralExpression>()
            .AddExpressionHandler<DelegateExpressionHandler, DelegateExpression>()
            .AddExpressionHandler<VariableExpressionHandler, VariableExpression>()
            .AddExpressionHandler<ObjectExpressionHandler, ObjectExpression>()
            .AddExpressionHandler<OutputExpressionHandler, OutputExpression>()
            .AddExpressionHandler<ElsaExpressionHandler, ElsaExpression>();
    }
}