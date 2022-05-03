using System.Threading.Channels;
using Elsa.Expressions;
using Elsa.Mediator.Extensions;
using Elsa.Options;
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
    public static IServiceCollection AddElsaRuntime(this ElsaOptionsConfigurator configurator)
    {
        var services = configurator.Services;
        services.AddOptions<WorkflowRuntimeOptions>();

        // Add ordered hosted services.
        configurator
            .AddHostedService<RegisterDescriptors>()
            .AddHostedService<RegisterExpressionSyntaxDescriptors>()
            .AddHostedService<DispatchedWorkflowDefinitionWorker>()
            .AddHostedService<DispatchedWorkflowInstanceWorker>()
            .AddHostedService<PopulateWorkflowDefinitionStore>();

        return services
                // Core.
                .AddSingleton<IWorkflowRegistry, WorkflowRegistry>()
                .AddSingleton<IStimulusInterpreter, StimulusInterpreter>()
                .AddSingleton<IWorkflowInstructionExecutor, WorkflowInstructionExecutor>()
                .AddSingleton<ITriggerIndexer, TriggerIndexer>()
                .AddSingleton<IBookmarkManager, BookmarkManager>()
                .AddSingleton<IWorkflowInstanceFactory, WorkflowInstanceFactory>()
                .AddSingleton<IWorkflowDefinitionService, WorkflowDefinitionService>()
                .AddSingleton(sp => sp.GetRequiredService<IOptions<WorkflowRuntimeOptions>>().Value.WorkflowInvokerFactory(sp))
                .AddSingleton(sp => sp.GetRequiredService<IOptions<WorkflowRuntimeOptions>>().Value.WorkflowDispatcherFactory(sp))

                // Stimulus handlers.
                .AddStimulusHandler<TriggerWorkflowsStimulusHandler>()
                .AddStimulusHandler<ResumeWorkflowsStimulusHandler>()

                // Instruction interpreters.
                .AddInstructionInterpreter<TriggerWorkflowInstructionInterpreter>()
                .AddInstructionInterpreter<ResumeWorkflowInstructionInterpreter>()

                // Workflow definition providers.
                .AddWorkflowDefinitionProvider<ClrWorkflowDefinitionProvider>()

                // Workflow engine.
                .AddSingleton<IWorkflowService, WorkflowService>()

                // Domain event handlers.
                .AddNotificationHandlersFrom(typeof(ServiceCollectionExtensions))

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

    public static IServiceCollection AddWorkflowDefinitionProvider<T>(this IServiceCollection services) where T : class, IWorkflowDefinitionProvider => services.AddSingleton<IWorkflowDefinitionProvider, T>();
    public static IServiceCollection AddStimulusHandler<T>(this IServiceCollection services) where T : class, IStimulusHandler => services.AddSingleton<IStimulusHandler, T>();
    public static IServiceCollection AddInstructionInterpreter<T>(this IServiceCollection services) where T : class, IWorkflowInstructionInterpreter => services.AddSingleton<IWorkflowInstructionInterpreter, T>();
    public static IServiceCollection ConfigureWorkflowRuntime(this IServiceCollection services, Action<WorkflowRuntimeOptions> configure) => services.Configure(configure);
}