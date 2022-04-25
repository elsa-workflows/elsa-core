using System.Threading.Channels;
using Elsa.Expressions;
using Elsa.Mediator.Extensions;
using Elsa.Runtime.HostedServices;
using Elsa.Runtime.Implementations;
using Elsa.Runtime.Interpreters;
using Elsa.Runtime.Models;
using Elsa.Runtime.Options;
using Elsa.Runtime.Services;
using Elsa.Runtime.Stimuli.Handlers;
using Elsa.Runtime.WorkflowProviders;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

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
                .AddSingleton<IBookmarkManager, BookmarkManager>()
                .AddSingleton(sp => sp.GetRequiredService<IOptions<WorkflowRuntimeOptions>>().Value.WorkflowInvokerFactory(sp))
                .AddSingleton(sp => sp.GetRequiredService<IOptions<WorkflowRuntimeOptions>>().Value.WorkflowDispatcherFactory(sp))

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
                .AddSingleton<IWorkflowService, WorkflowService>()

                // Domain event handlers.
                .AddNotificationHandlersFrom(typeof(ServiceCollectionExtensions))

                // Hosted Services.
                .AddHostedService<RegisterDescriptors>()
                .AddHostedService<RegisterExpressionSyntaxDescriptors>()
                .AddHostedService<DispatchedWorkflowDefinitionWorker>()
                .AddHostedService<DispatchedWorkflowInstanceWorker>()

                // Channels for dispatching workflows in-memory.
                .CreateChannel<DispatchWorkflowDefinitionRequest>()
                .CreateChannel<DispatchWorkflowInstanceRequest>()
            ;
    }

    private static IServiceCollection CreateChannel<T>(this IServiceCollection services) =>
        services
            .AddSingleton(CreateChannel<T>())
            .AddSingleton(CreateChannelReader<T>)
            .AddSingleton(CreateChannelWriter<T>);

    private static Channel<T> CreateChannel<T>() => Channel.CreateUnbounded<T>(new UnboundedChannelOptions());
    private static ChannelReader<T> CreateChannelReader<T>(IServiceProvider serviceProvider) => serviceProvider.GetRequiredService<Channel<T>>().Reader;
    private static ChannelWriter<T> CreateChannelWriter<T>(IServiceProvider serviceProvider) => serviceProvider.GetRequiredService<Channel<T>>().Writer;

    public static IServiceCollection AddDefaultExpressionHandlers(this IServiceCollection services) =>
        services
            .AddExpressionHandler<LiteralExpressionHandler, LiteralExpression>()
            .AddExpressionHandler<DelegateExpressionHandler, DelegateExpression>()
            .AddExpressionHandler<VariableExpressionHandler, VariableExpression>()
            .AddExpressionHandler<OutputExpressionHandler, OutputExpression>()
            .AddExpressionHandler<ElsaExpressionHandler, ElsaExpression>();

    public static IServiceCollection AddWorkflowProvider<T>(this IServiceCollection services) where T : class, IWorkflowProvider => services.AddSingleton<IWorkflowProvider, T>();
    public static IServiceCollection AddStimulusHandler<T>(this IServiceCollection services) where T : class, IStimulusHandler => services.AddSingleton<IStimulusHandler, T>();
    public static IServiceCollection AddInstructionInterpreter<T>(this IServiceCollection services) where T : class, IWorkflowInstructionInterpreter => services.AddSingleton<IWorkflowInstructionInterpreter, T>();
    public static IServiceCollection ConfigureWorkflowRuntime(this IServiceCollection services, Action<WorkflowRuntimeOptions> configure) => services.Configure(configure);
    public static IServiceCollection IndexWorkflowTriggers(this IServiceCollection services) => services.AddHostedService<IndexWorkflowTriggers>();
}