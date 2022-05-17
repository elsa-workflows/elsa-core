using Elsa.Mediator.Extensions;
using Elsa.Options;
using Elsa.Runtime.Extensions;
using Elsa.Runtime.HostedServices;
using Elsa.Runtime.Implementations;
using Elsa.Runtime.Interpreters;
using Elsa.Runtime.Models;
using Elsa.Runtime.Services;
using Elsa.Runtime.Stimuli.Handlers;
using Elsa.Runtime.WorkflowProviders;
using Elsa.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Elsa.Runtime.Options;

public class ElsaRuntimeOptions : ConfiguratorBase
{
    /// <summary>
    /// A list of workflow builders configured during application startup.
    /// </summary>
    public IDictionary<string, Func<IServiceProvider, ValueTask<IWorkflow>>> Workflows { get; set; } = new Dictionary<string, Func<IServiceProvider, ValueTask<IWorkflow>>>();

    /// <summary>
    /// A factory that instantiates a concrete <see cref="IWorkflowInvoker"/>.
    /// </summary>
    public Func<IServiceProvider, IWorkflowInvoker> WorkflowInvokerFactory { get; set; } = sp => ActivatorUtilities.CreateInstance<DefaultWorkflowInvoker>(sp);

    /// <summary>
    /// A factory that instantiates a concrete <see cref="IWorkflowDispatcher"/>.
    /// </summary>
    public Func<IServiceProvider, IWorkflowDispatcher> WorkflowDispatcherFactory { get; set; } = sp => ActivatorUtilities.CreateInstance<TaskBasedWorkflowDispatcher>(sp);

    public override void ConfigureServices(ElsaOptionsConfigurator configurator)
    {
        var services = configurator.Services;
        services.AddOptions<ElsaRuntimeOptions>();

        services
            // Core.
            .AddSingleton<IStimulusInterpreter, StimulusInterpreter>()
            .AddSingleton<IWorkflowInstructionExecutor, WorkflowInstructionExecutor>()
            .AddSingleton<ITriggerIndexer, TriggerIndexer>()
            .AddSingleton<IBookmarkManager, BookmarkManager>()
            .AddSingleton<IWorkflowInstanceFactory, WorkflowInstanceFactory>()
            .AddSingleton<IWorkflowDefinitionService, WorkflowDefinitionService>()
            .AddSingleton(sp => sp.GetRequiredService<IOptions<ElsaRuntimeOptions>>().Value.WorkflowInvokerFactory(sp))
            .AddSingleton(sp => sp.GetRequiredService<IOptions<ElsaRuntimeOptions>>().Value.WorkflowDispatcherFactory(sp))

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

    public override void ConfigureHostedServices(ElsaOptionsConfigurator configurator)
    {
        configurator
            .AddHostedService<RegisterDescriptors>()
            .AddHostedService<RegisterExpressionSyntaxDescriptors>()
            .AddHostedService<DispatchedWorkflowDefinitionWorker>()
            .AddHostedService<DispatchedWorkflowInstanceWorker>()
            .AddHostedService<PopulateWorkflowDefinitionStore>();
    }
}