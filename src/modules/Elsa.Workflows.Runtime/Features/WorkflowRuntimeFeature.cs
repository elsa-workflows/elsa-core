using Elsa.Common.Features;
using Elsa.Extensions;
using Elsa.Features.Abstractions;
using Elsa.Features.Attributes;
using Elsa.Features.Services;
using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Core.Notifications;
using Elsa.Workflows.Core.State;
using Elsa.Workflows.Management.Contracts;
using Elsa.Workflows.Management.Notifications;
using Elsa.Workflows.Management.Services;
using Elsa.Workflows.Runtime.ActivationValidators;
using Elsa.Workflows.Runtime.Commands;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.Runtime.Handlers;
using Elsa.Workflows.Runtime.HostedServices;
using Elsa.Workflows.Runtime.Notifications;
using Elsa.Workflows.Runtime.Options;
using Elsa.Workflows.Runtime.Services;
using Medallion.Threading;
using Medallion.Threading.FileSystem;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.Runtime.Features;

/// <summary>
/// Installs and configures workflow runtime features.
/// </summary>
[DependsOn(typeof(SystemClockFeature))]
public class WorkflowRuntimeFeature : FeatureBase
{
    /// <inheritdoc />
    public WorkflowRuntimeFeature(IModule module) : base(module)
    {
    }

    /// <summary>
    /// A list of workflow builders configured during application startup.
    /// </summary>
    public IDictionary<string, Func<IServiceProvider, ValueTask<IWorkflow>>> Workflows { get; set; } = new Dictionary<string, Func<IServiceProvider, ValueTask<IWorkflow>>>();

    /// <summary>
    /// A factory that instantiates a concrete <see cref="IWorkflowRuntime"/>.
    /// </summary>
    public Func<IServiceProvider, IWorkflowRuntime> WorkflowRuntime { get; set; } = sp => ActivatorUtilities.CreateInstance<DefaultWorkflowRuntime>(sp);

    /// <summary>
    /// A factory that instantiates an <see cref="IWorkflowDispatcher"/>.
    /// </summary>
    public Func<IServiceProvider, IWorkflowDispatcher> WorkflowDispatcher { get; set; } = sp => ActivatorUtilities.CreateInstance<TaskBasedWorkflowDispatcher>(sp);
    
    /// <summary>
    /// A factory that instantiates an <see cref="IBookmarkStore"/>.
    /// </summary>
    public Func<IServiceProvider, IBookmarkStore> BookmarkStore { get; set; } = sp => sp.GetRequiredService<MemoryBookmarkStore>();

    /// <summary>
    /// A factory that instantiates an <see cref="ITriggerStore"/>.
    /// </summary>
    public Func<IServiceProvider, ITriggerStore> WorkflowTriggerStore { get; set; } = sp => sp.GetRequiredService<MemoryTriggerStore>();

    /// <summary>
    /// A factory that instantiates an <see cref="IWorkflowExecutionLogStore"/>.
    /// </summary>
    public Func<IServiceProvider, IWorkflowExecutionLogStore> WorkflowExecutionLogStore { get; set; } = sp => sp.GetRequiredService<MemoryWorkflowExecutionLogStore>();

    /// <summary>
    /// A factory that instantiates an <see cref="IDistributedLockProvider"/>.
    /// </summary>
    public Func<IServiceProvider, IDistributedLockProvider> DistributedLockProvider { get; set; } = _ => new FileDistributedSynchronizationProvider(new DirectoryInfo(Path.Combine(Environment.CurrentDirectory, "App_Data/locks")));

    /// <summary>
    /// A factory that instantiates an <see cref="IWorkflowStateExporter"/>.
    /// </summary>
    public Func<IServiceProvider, IWorkflowStateExporter> WorkflowStateExporter { get; set; } = ActivatorUtilities.GetServiceOrCreateInstance<NoopWorkflowStateExporter>;

    /// <summary>
    /// A factory that instantiates an <see cref="ITaskDispatcher"/>.
    /// </summary>
    public Func<IServiceProvider, ITaskDispatcher> RunTaskDispatcher { get; set; } = sp => sp.GetRequiredService<AsynchronousTaskDispatcher>();

    /// <summary>
    /// A factory that instantiates an <see cref="IBackgroundActivityScheduler"/>.
    /// </summary>
    public Func<IServiceProvider, IBackgroundActivityScheduler> BackgroundActivityInvoker { get; set; } = sp => ActivatorUtilities.CreateInstance<LocalBackgroundActivityScheduler>(sp);

    /// <summary>
    /// A delegate to configure the <see cref="DistributedLockingOptions"/>.
    /// </summary>
    public Action<DistributedLockingOptions> DistributedLockingOptions { get; set; } = _ => { };

    /// <summary>
    /// Register the specified workflow type.
    /// </summary>
    public WorkflowRuntimeFeature AddWorkflow<T>() where T : IWorkflow
    {
        Workflows.Add<T>();
        return this;
    }

    /// <inheritdoc />
    public override void Configure()
    {
        // Activities
        Module.AddActivitiesFrom<WorkflowRuntimeFeature>();
    }

    /// <inheritdoc />
    public override void ConfigureHostedServices() =>
        Module
            .ConfigureHostedService<RegisterDescriptors>()
            .ConfigureHostedService<RegisterExpressionSyntaxDescriptors>()
            .ConfigureHostedService<PopulateWorkflowDefinitionStore>();

    /// <inheritdoc />
    public override void Apply()
    {
        // Options.
        Services.Configure(DistributedLockingOptions);
        Services.Configure<RuntimeOptions>(options => { options.Workflows = Workflows; });

        Services
            // Core.
            .AddSingleton<ITriggerIndexer, TriggerIndexer>()
            .AddSingleton<IWorkflowInstanceFactory, WorkflowInstanceFactory>()
            .AddSingleton<IWorkflowDefinitionService, WorkflowDefinitionService>()
            .AddSingleton<IWorkflowHostFactory, WorkflowHostFactory>()
            .AddSingleton<IBackgroundActivityInvoker, DefaultBackgroundActivityInvoker>()
            .AddSingleton(WorkflowRuntime)
            .AddSingleton(WorkflowDispatcher)
            .AddSingleton(BookmarkStore)
            .AddSingleton(WorkflowTriggerStore)
            .AddSingleton(WorkflowExecutionLogStore)
            .AddSingleton(RunTaskDispatcher)
            .AddSingleton(BackgroundActivityInvoker)
            .AddSingleton<ITaskReporter, TaskReporter>()
            .AddSingleton<SynchronousTaskDispatcher>()
            .AddSingleton<AsynchronousTaskDispatcher>()
            .AddSingleton<IEventPublisher, EventPublisher>()
            
            // Lazy services.
            .AddSingleton<Func<IEnumerable<IWorkflowDefinitionProvider>>>(sp => sp.GetServices<IWorkflowDefinitionProvider>)
            .AddSingleton<Func<IEnumerable<IWorkflowMaterializer>>>(sp => sp.GetServices<IWorkflowMaterializer>)

            // Memory stores.
            .AddMemoryStore<WorkflowState, MemoryWorkflowStateStore>()
            .AddMemoryStore<StoredBookmark, MemoryBookmarkStore>()
            .AddMemoryStore<StoredTrigger, MemoryTriggerStore>()
            .AddMemoryStore<WorkflowExecutionLogRecord, MemoryWorkflowExecutionLogStore>()

            // Distributed locking.
            .AddSingleton(DistributedLockProvider)

            // Workflow definition providers.
            .AddWorkflowDefinitionProvider<ClrWorkflowDefinitionProvider>()

            // Workflow state exporter.
            .AddSingleton(WorkflowStateExporter)

            // Domain handlers.
            .AddCommandHandler<DispatchWorkflowRequestHandler, DispatchTriggerWorkflowsCommand>()
            .AddCommandHandler<DispatchWorkflowRequestHandler, DispatchWorkflowDefinitionCommand>()
            .AddCommandHandler<DispatchWorkflowRequestHandler, DispatchWorkflowInstanceCommand>()
            .AddCommandHandler<DispatchWorkflowRequestHandler, DispatchResumeWorkflowsCommand>()
            .AddNotificationHandler<ExportWorkflowStateHandler, WorkflowExecuted>()
            .AddNotificationHandler<ResumeDispatchWorkflowActivityHandler, WorkflowExecuted>()
            .AddNotificationHandler<IndexWorkflowTriggersHandler, WorkflowDefinitionPublished>()
            .AddNotificationHandler<IndexWorkflowTriggersHandler, WorkflowDefinitionRetracted>()
            .AddNotificationHandler<ScheduleBackgroundActivities, WorkflowBookmarksIndexed>()
            .AddNotificationHandler<CancelBackgroundActivities, WorkflowBookmarksIndexed>()

            // Workflow activation strategies.
            .AddSingleton<IWorkflowActivationStrategy, SingletonStrategy>()
            .AddSingleton<IWorkflowActivationStrategy, CorrelatedSingletonStrategy>()
            .AddSingleton<IWorkflowActivationStrategy, CorrelationStrategy>()
            ;
    }
}