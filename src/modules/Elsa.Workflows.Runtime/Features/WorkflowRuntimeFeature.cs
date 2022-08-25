using Elsa.Common.Features;
using Elsa.Features.Abstractions;
using Elsa.Features.Attributes;
using Elsa.Features.Services;
using Elsa.Mediator.Extensions;
using Elsa.Workflows.Core.Services;
using Elsa.Workflows.Runtime.Extensions;
using Elsa.Workflows.Runtime.HostedServices;
using Elsa.Workflows.Runtime.Implementations;
using Elsa.Workflows.Runtime.Interpreters;
using Elsa.Workflows.Runtime.Models;
using Elsa.Workflows.Runtime.Options;
using Elsa.Workflows.Runtime.Services;
using Elsa.Workflows.Runtime.Stimuli.Handlers;
using Elsa.Workflows.Runtime.WorkflowProviders;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.Runtime.Features;

[DependsOn(typeof(SystemClockFeature))]
public class WorkflowRuntimeFeature : FeatureBase
{
    public WorkflowRuntimeFeature(IModule module) : base(module)
    {
    }

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
    
    public WorkflowRuntimeFeature AddWorkflow<T>() where T : IWorkflow
    {
        Workflows.Add<T>();
        return this;
    }

    public override void ConfigureHostedServices() =>
        Module
            .ConfigureHostedService<RegisterDescriptors>()
            .ConfigureHostedService<RegisterExpressionSyntaxDescriptors>()
            .ConfigureHostedService<DispatchedWorkflowDefinitionWorker>()
            .ConfigureHostedService<DispatchedWorkflowInstanceWorker>()
            .ConfigureHostedService<PopulateWorkflowDefinitionStore>();
    
    public override void Apply()
    {
        Services
            // Core.
            .AddSingleton<IStimulusInterpreter, StimulusInterpreter>()
            .AddSingleton<IWorkflowInstructionExecutor, WorkflowInstructionExecutor>()
            .AddSingleton<ITriggerIndexer, TriggerIndexer>()
            .AddSingleton<IBookmarkManager, BookmarkManager>()
            .AddSingleton<IWorkflowInstanceFactory, WorkflowInstanceFactory>()
            .AddSingleton<IWorkflowDefinitionService, WorkflowDefinitionService>()
            .AddSingleton(WorkflowInvokerFactory)
            .AddSingleton(WorkflowDispatcherFactory)

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
            .AddNotificationHandlersFrom(typeof(WorkflowRuntimeFeature))

            // Channels for dispatching workflows in-memory.
            .CreateChannel<DispatchWorkflowDefinitionRequest>()
            .CreateChannel<DispatchWorkflowInstanceRequest>()
            ;

        Services.Configure<WorkflowRuntimeOptions>(options => options.Workflows = Workflows);
    }
}