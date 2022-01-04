using Elsa.Expressions;
using Elsa.Extensions;
using Elsa.Mediator.Extensions;
using Elsa.Runtime.Contracts;
using Elsa.Runtime.HostedServices;
using Elsa.Runtime.Interpreters;
using Elsa.Runtime.Options;
using Elsa.Runtime.Services;
using Elsa.Runtime.Stimuli.Handlers;
using Elsa.Runtime.WorkflowProviders;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Runtime.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddElsaRuntime(this IServiceCollection services)
    {
        services.AddOptions<WorkflowRuntimeOptions>();

        return services
                // Core.
                .AddSingleton<IWorkflowRegistry, WorkflowRegistry>()
                .AddSingleton<IStimulusInterpreter, StimulusInterpreter>()
                .AddSingleton<IWorkflowInstructionExecutor, WorkflowInstructionExecutor>()
                .AddSingleton<ITriggerIndexer, TriggerIndexer>()

                // Stimulus handlers.
                .AddStimulusHandler<TriggerWorkflowsStimulusHandler>()
                .AddStimulusHandler<ResumeWorkflowsStimulusHandler>()

                // Instruction interpreters.
                .AddInstructionInterpreter<TriggerWorkflowInstructionInterpreter>()
                .AddInstructionInterpreter<ResumeWorkflowInstructionInterpreter>()

                // Workflow providers.
                .AddWorkflowProvider<ConfigurationWorkflowProvider>()
                .AddWorkflowProvider<DatabaseWorkflowProvider>()

                // Workflow engine.
                .AddSingleton<IWorkflowServer, WorkflowServer>()

                // Domain event handlers
                .AddNotificationHandlersFrom(typeof(ServiceCollectionExtensions))
            
                // Hosted Sercices
                .AddHostedService<RegisterDescriptorsHostedService>()
                .AddHostedService<RegisterExpressionSyntaxDescriptorsHostedService>();
            ;
    }

    private static IServiceCollection AddDefaultExpressionHandlers(this IServiceCollection services) =>
        services
            .AddExpressionHandler<LiteralExpressionHandler, LiteralExpression>()
            .AddExpressionHandler<DelegateExpressionHandler, DelegateExpression>()
            .AddExpressionHandler<VariableExpressionHandler, VariableExpression>()
            .AddExpressionHandler<ElsaExpressionHandler, ElsaExpression>();

    public static IServiceCollection AddWorkflowProvider<T>(this IServiceCollection services) where T : class, IWorkflowProvider => services.AddSingleton<IWorkflowProvider, T>();
    public static IServiceCollection AddStimulusHandler<T>(this IServiceCollection services) where T : class, IStimulusHandler => services.AddSingleton<IStimulusHandler, T>();
    public static IServiceCollection AddInstructionInterpreter<T>(this IServiceCollection services) where T : class, IWorkflowInstructionInterpreter => services.AddSingleton<IWorkflowInstructionInterpreter, T>();
    public static IServiceCollection ConfigureWorkflowRuntime(this IServiceCollection services, Action<WorkflowRuntimeOptions> configure) => services.Configure(configure);
    public static IServiceCollection IndexWorkflowTriggers(this IServiceCollection services) => services.AddHostedService<IndexWorkflowTriggersHostedService>();
}