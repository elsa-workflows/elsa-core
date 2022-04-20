using Elsa.ActivityNodeResolvers;
using Elsa.Contracts;
using Elsa.Mediator.Extensions;
using Elsa.Modules.Activities.Extensions;
using Elsa.Options;
using Elsa.Persistence.Extensions;
using Elsa.Pipelines.ActivityExecution;
using Elsa.Pipelines.WorkflowExecution;
using Elsa.Runtime.Contracts;
using Elsa.Runtime.Extensions;
using Elsa.Serialization;
using Elsa.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddElsa(this IServiceCollection services) => services
        .AddElsaCore()
        .AddElsaRuntime()
        .AddDefaultExpressionHandlers();

    public static IServiceCollection AddElsaCore(this IServiceCollection services)
    {
        services.AddOptions<WorkflowEngineOptions>();

        return services

            // Core.
            .AddSingleton<IActivityInvoker, ActivityInvoker>()
            .AddSingleton<IWorkflowRunner, WorkflowRunner>()
            .AddSingleton<IActivityWalker, ActivityWalker>()
            .AddSingleton<IIdentityGraphService, IdentityGraphService>()
            .AddSingleton<IWorkflowStateSerializer, WorkflowStateSerializer>()
            .AddSingleton<IActivitySchedulerFactory, ActivitySchedulerFactory>()
            .AddSingleton<IActivityNodeResolver, OutboundActivityNodeResolver>()
            .AddSingleton<IHasher, Hasher>()
            .AddSingleton<IIdentityGenerator, IdentityGenerator>()
            .AddSingleton<ISystemClock, SystemClock>()
            .AddSingleton<IBookmarkDataSerializer, BookmarkDataSerializer>()
            .AddSingleton<IWellKnownTypeRegistry, WellKnownTypeRegistry>()

            // Expressions.
            .AddSingleton<IExpressionEvaluator, ExpressionEvaluator>()
            .AddSingleton<IExpressionHandlerRegistry, ExpressionHandlerRegistry>()

            // Pipelines.
            .AddSingleton<IActivityExecutionPipeline, ActivityExecutionPipeline>()
            .AddSingleton<IWorkflowExecutionPipeline, WorkflowExecutionPipeline>()
            
            // Additional activity services.
            .AddActivityServices()

            // Persistence services.
            .AddPersistenceServices()
            
            // Mediator.
            .AddMediator()

            // Logging
            .AddLogging();
    }

    public static IServiceCollection AddWorkflowProvider<T>(this IServiceCollection services) where T : class, IWorkflowProvider => services.AddSingleton<IWorkflowProvider, T>();
    public static IServiceCollection AddStimulusHandler<T>(this IServiceCollection services) where T : class, IStimulusHandler => services.AddSingleton<IStimulusHandler, T>();
    public static IServiceCollection AddInstructionInterpreter<T>(this IServiceCollection services) where T : class, IWorkflowInstructionInterpreter => services.AddSingleton<IWorkflowInstructionInterpreter, T>();
}