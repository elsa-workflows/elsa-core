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

public class WorkflowConfigurator : ConfiguratorBase
{
    public WorkflowConfigurator(IServiceConfiguration serviceConfiguration)
    {
        ServiceConfiguration = serviceConfiguration;
    }
    
    public IServiceConfiguration ServiceConfiguration { get; }
    
    public override void ConfigureServices(IServiceConfiguration serviceConfiguration)
    {
        var services = serviceConfiguration.Services;
        AddElsaCore(services);
        AddExpressions(services);
    }

    private void AddElsaCore(IServiceCollection services)
    {
        //services.AddOptions<ElsaOptions>();

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