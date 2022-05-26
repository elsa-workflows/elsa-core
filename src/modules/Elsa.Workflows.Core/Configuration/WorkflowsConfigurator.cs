using Elsa.Expressions;
using Elsa.Expressions.Extensions;
using Elsa.ServiceConfiguration.Abstractions;
using Elsa.ServiceConfiguration.Services;
using Elsa.Workflows.Core.ActivityNodeResolvers;
using Elsa.Workflows.Core.Expressions;
using Elsa.Workflows.Core.Implementations;
using Elsa.Workflows.Core.Pipelines.ActivityExecution;
using Elsa.Workflows.Core.Pipelines.WorkflowExecution;
using Elsa.Workflows.Core.Serialization;
using Elsa.Workflows.Core.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.Core.Configuration;

public class WorkflowsConfigurator : ConfiguratorBase
{
    public WorkflowsConfigurator(IServiceConfiguration serviceConfiguration) : base(serviceConfiguration)
    {
        ServiceConfiguration = serviceConfiguration;
    }
    
    /// <summary>
    /// A factory that instantiates a concrete <see cref="IStandardInStreamProvider"/>.
    /// </summary>
    public Func<IServiceProvider, IStandardInStreamProvider> StandardInStreamProvider { get; set; } = _ => new StandardInStreamProvider(Console.In);

    /// <summary>
    /// A factory that instantiates a concrete <see cref="IStandardOutStreamProvider"/>.
    /// </summary>
    public Func<IServiceProvider, IStandardOutStreamProvider> StandardOutStreamProvider { get; set; } = _ => new StandardOutStreamProvider(Console.Out);
    
    public WorkflowsConfigurator WithStandardInStreamProvider(Func<IServiceProvider, IStandardInStreamProvider> provider)
    {
        StandardInStreamProvider = provider;
        return this;
    }

    public WorkflowsConfigurator WithStandardOutStreamProvider(Func<IServiceProvider, IStandardOutStreamProvider> provider)
    {
        StandardOutStreamProvider = provider;
        return this;
    }
    
    public override void ConfigureServices(IServiceConfiguration serviceConfiguration)
    {
        var services = serviceConfiguration.Services;
        AddElsaCore(services);
        AddExpressions(services);
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
            .AddSingleton<IIdentityGenerator, RandomIdentityGenerator>()
            .AddSingleton<ISystemClock, SystemClock>()
            .AddSingleton<IBookmarkDataSerializer, BookmarkDataSerializer>()
            .AddSingleton<IWellKnownTypeRegistry, WellKnownTypeRegistry>()

            // Pipelines.
            .AddSingleton<IActivityExecutionPipeline, ActivityExecutionPipeline>()
            .AddSingleton<IWorkflowExecutionPipeline, WorkflowExecutionPipeline>()

            // Built-in activity services.
            .AddSingleton<IActivityNodeResolver, OutboundActivityNodeResolver>()
            .AddSingleton<IActivityNodeResolver, SwitchActivityNodeResolver>()
            .AddSingleton<ISerializationOptionsConfigurator, CustomSerializationOptionConfigurator>()
            
            // Stream providers.
            .AddSingleton(StandardInStreamProvider)
            .AddSingleton(StandardOutStreamProvider)

            // Logging
            .AddLogging();
    }

    private void AddExpressions(IServiceCollection services)
    {
        services
            .AddExpressions()
            .AddExpressionHandler<LiteralExpressionHandler, LiteralExpression>()
            .AddExpressionHandler<DelegateExpressionHandler, DelegateExpression>()
            .AddExpressionHandler<VariableExpressionHandler, VariableExpression>()
            .AddExpressionHandler<JsonExpressionHandler, JsonExpression>()
            .AddExpressionHandler<OutputExpressionHandler, OutputExpression>()
            .AddExpressionHandler<ElsaExpressionHandler, ElsaExpression>();
    }
}