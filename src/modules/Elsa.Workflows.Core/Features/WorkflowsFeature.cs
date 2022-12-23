using Elsa.Common.Features;
using Elsa.Expressions;
using Elsa.Expressions.Extensions;
using Elsa.Expressions.Features;
using Elsa.Features.Abstractions;
using Elsa.Features.Attributes;
using Elsa.Features.Services;
using Elsa.Workflows.Core.ActivationValidators;
using Elsa.Workflows.Core.ActivityNodeResolvers;
using Elsa.Workflows.Core.Builders;
using Elsa.Workflows.Core.Expressions;
using Elsa.Workflows.Core.Implementations;
using Elsa.Workflows.Core.Pipelines.ActivityExecution;
using Elsa.Workflows.Core.Pipelines.WorkflowExecution;
using Elsa.Workflows.Core.Serialization;
using Elsa.Workflows.Core.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.Core.Features;

/// <summary>
/// Adds workflow services to the system.
/// </summary>
[DependsOn(typeof(SystemClockFeature))]
[DependsOn(typeof(ExpressionsFeature))]
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
            .AddSingleton<IActivityWalker, ActivityWalker>()
            .AddSingleton<IIdentityGraphService, IdentityGraphService>()
            .AddSingleton<IWorkflowStateSerializer, WorkflowStateSerializer>()
            .AddSingleton<IActivitySchedulerFactory, ActivitySchedulerFactory>()
            .AddSingleton<IHasher, Hasher>()
            .AddSingleton<IBookmarkHasher, BookmarkHasher>()
            .AddSingleton<IIdentityGenerator, RandomIdentityGenerator>()
            .AddSingleton<IWorkflowExecutionContextFactory, DefaultWorkflowExecutionContextFactory>()
            .AddSingleton<IBookmarkPayloadSerializer, BookmarkPayloadSerializer>()
            .AddTransient<WorkflowBuilder>()
            .AddSingleton(typeof(Func<IWorkflowBuilder>), sp => () => sp.GetRequiredService<WorkflowBuilder>())
            .AddSingleton<IWorkflowBuilderFactory, WorkflowBuilderFactory>()
            .AddSingleton<IWellKnownTypeRegistry, WellKnownTypeRegistry>()
            .AddSingleton<IVariablePersistenceManager, VariablePersistenceManager>()

            // Pipelines.
            .AddSingleton<IActivityExecutionPipeline, ActivityExecutionPipeline>()
            .AddSingleton<IWorkflowExecutionPipeline, WorkflowExecutionPipeline>()

            // Built-in activity services.
            .AddSingleton<IActivityPortResolver, OutboundActivityPortResolver>()
            .AddSingleton<IActivityPortResolver, SwitchActivityPortResolver>()
            .AddSingleton<ISerializationOptionsConfigurator, CustomSerializationOptionConfigurator>()
            
            // Stream providers.
            .AddSingleton(StandardInStreamProvider)
            .AddSingleton(StandardOutStreamProvider)
            
            // Storage drivers.
            .AddSingleton<IStorageDriverManager, StorageDriverManager>()
            .AddStorageDriver<WorkflowStorageDriver>()
            .AddStorageDriver<MemoryStorageDriver>()
            
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
            .AddExpressionHandler<JsonExpressionHandler, JsonExpression>()
            .AddExpressionHandler<OutputExpressionHandler, OutputExpression>()
            .AddExpressionHandler<ElsaExpressionHandler, ElsaExpression>();
    }
}