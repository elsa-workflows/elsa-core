using Elsa.Mediator.Extensions;
using Elsa.Workflows.Core.Implementations;
using Elsa.Workflows.Core.Options;
using Elsa.Workflows.Core.Services;
using Elsa.Workflows.Runtime.Extensions;
using Elsa.Workflows.Runtime.HostedServices;
using Elsa.Workflows.Runtime.Implementations;
using Elsa.Workflows.Runtime.Interpreters;
using Elsa.Workflows.Runtime.Models;
using Elsa.Workflows.Runtime.Services;
using Elsa.Workflows.Runtime.Stimuli.Handlers;
using Elsa.Workflows.Runtime.WorkflowProviders;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ServiceCollectionExtensions = Elsa.Workflows.Core.ServiceCollectionExtensions;

namespace Elsa.Workflows.Runtime.Options;

public class ElsaRuntimeOptions : ConfiguratorBase
{
    /// <summary>
    /// A factory that instantiates a concrete <see cref="IStandardInStreamProvider"/>.
    /// </summary>
    public Func<IServiceProvider, IStandardInStreamProvider> StandardInStreamProvider { get; set; } = _ => new StandardInStreamProvider(Console.In);
    
    /// <summary>
    /// A factory that instantiates a concrete <see cref="IStandardOutStreamProvider"/>.
    /// </summary>
    public Func<IServiceProvider, IStandardOutStreamProvider> StandardOutStreamProvider { get; set; } = _ => new StandardOutStreamProvider(Console.Out);

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

    public ElsaRuntimeOptions WithStandardInStreamProvider(Func<IServiceProvider, IStandardInStreamProvider> provider)
    {
        StandardInStreamProvider = provider;
        return this;
    }

    public ElsaRuntimeOptions WithStandardOutStreamProvider(Func<IServiceProvider, IStandardOutStreamProvider> provider)
    {
        StandardOutStreamProvider = provider;
        return this;
    }

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
            
            // Stream providers.
            .AddSingleton(StandardInStreamProvider)
            .AddSingleton(StandardOutStreamProvider)
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