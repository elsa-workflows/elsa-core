using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Elsa.Common.DistributedHosting;
using Elsa.Common.Features;
using Elsa.Common.RecurringTasks;
using Elsa.Extensions;
using Elsa.Features.Abstractions;
using Elsa.Features.Attributes;
using Elsa.Features.Services;
using Elsa.Mediator.Contracts;
using Elsa.Workflows.Features;
using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Contracts;
using Elsa.Workflows.Management.Services;
using Elsa.Workflows.Runtime.ActivationValidators;
using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.Runtime.Handlers;
using Elsa.Workflows.Runtime.Options;
using Elsa.Workflows.Runtime.Providers;
using Elsa.Workflows.Runtime.Stores;
using Elsa.Workflows.Runtime.Tasks;
using Elsa.Workflows.Runtime.UIHints;
using Medallion.Threading;
using Medallion.Threading.FileSystem;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.Runtime.Features;

/// <summary>
/// Installs and configures workflow runtime features.
/// </summary>
[DependsOn(typeof(SystemClockFeature))]
public class WorkflowRuntimeFeature(IModule module) : FeatureBase(module)
{
    private IDictionary<string, DispatcherChannel> WorkflowDispatcherChannels { get; set; } = new Dictionary<string, DispatcherChannel>();

    /// <summary>
    /// A list of workflow builders configured during application startup.
    /// </summary>
    public IDictionary<string, Func<IServiceProvider, ValueTask<IWorkflow>>> Workflows { get; set; } = new Dictionary<string, Func<IServiceProvider, ValueTask<IWorkflow>>>();

    /// <summary>
    /// A factory that instantiates a concrete <see cref="IWorkflowRuntime"/>.
    /// </summary>
    public Func<IServiceProvider, IWorkflowRuntime> WorkflowRuntime { get; set; } = sp => ActivatorUtilities.CreateInstance<LocalWorkflowRuntime>(sp);

    /// <summary>
    /// A factory that instantiates an <see cref="IWorkflowDispatcher"/>.
    /// </summary>
    public Func<IServiceProvider, IWorkflowDispatcher> WorkflowDispatcher { get; set; } = sp =>
    {
        var decoratedService = ActivatorUtilities.CreateInstance<BackgroundWorkflowDispatcher>(sp);
        return ActivatorUtilities.CreateInstance<ValidatingWorkflowDispatcher>(sp, decoratedService);
    };
    
    /// <summary>
    /// A factory that instantiates an <see cref="IStimulusDispatcher"/>.
    /// </summary>
    public Func<IServiceProvider, IStimulusDispatcher> StimulusDispatcher { get; set; } = sp => ActivatorUtilities.CreateInstance<BackgroundStimulusDispatcher>(sp);

    /// <summary>
    /// A factory that instantiates an <see cref="IWorkflowCancellationDispatcher"/>.
    /// </summary>
    public Func<IServiceProvider, IWorkflowCancellationDispatcher> WorkflowCancellationDispatcher { get; set; } = sp => ActivatorUtilities.CreateInstance<BackgroundWorkflowCancellationDispatcher>(sp);

    /// <summary>
    /// A factory that instantiates an <see cref="IBookmarkStore"/>.
    /// </summary>
    public Func<IServiceProvider, IBookmarkStore> BookmarkStore { get; set; } = sp => sp.GetRequiredService<MemoryBookmarkStore>();

    /// <summary>
    /// A factory that instantiates an <see cref="IBookmarkQueueStore"/>.
    /// </summary>
    public Func<IServiceProvider, IBookmarkQueueStore> BookmarkQueueStore { get; set; } = sp => sp.GetRequiredService<MemoryBookmarkQueueStore>();

    /// <summary>
    /// A factory that instantiates an <see cref="ITriggerStore"/>.
    /// </summary>
    public Func<IServiceProvider, ITriggerStore> TriggerStore { get; set; } = sp => sp.GetRequiredService<MemoryTriggerStore>();

    /// <summary>
    /// A factory that instantiates an <see cref="IWorkflowExecutionLogStore"/>.
    /// </summary>
    public Func<IServiceProvider, IWorkflowExecutionLogStore> WorkflowExecutionLogStore { get; set; } = sp => sp.GetRequiredService<MemoryWorkflowExecutionLogStore>();

    /// <summary>
    /// A factory that instantiates an <see cref="IActivityExecutionStore"/>.
    /// </summary>
    public Func<IServiceProvider, IActivityExecutionStore> ActivityExecutionLogStore { get; set; } = sp => sp.GetRequiredService<MemoryActivityExecutionStore>();

    /// <summary>
    /// A factory that instantiates an <see cref="IDistributedLockProvider"/>.
    /// </summary>
    public Func<IServiceProvider, IDistributedLockProvider> DistributedLockProvider { get; set; } = _ => new FileDistributedSynchronizationProvider(new DirectoryInfo(Path.Combine(Environment.CurrentDirectory, "App_Data/locks")));

    /// <summary>
    /// A factory that instantiates an <see cref="ITaskDispatcher"/>.
    /// </summary>
    public Func<IServiceProvider, ITaskDispatcher> RunTaskDispatcher { get; set; } = sp => sp.GetRequiredService<BackgroundTaskDispatcher>();

    /// <summary>
    /// A factory that instantiates an <see cref="IBackgroundActivityScheduler"/>.
    /// </summary>
    public Func<IServiceProvider, IBackgroundActivityScheduler> BackgroundActivityScheduler { get; set; } = sp => ActivatorUtilities.CreateInstance<LocalBackgroundActivityScheduler>(sp);

    /// <summary>
    /// A factory that instantiates a log record sink for an <see cref="ActivityExecutionRecord"/>.
    /// </summary>
    public Func<IServiceProvider, ILogRecordSink<ActivityExecutionRecord>> ActivityExecutionLogSink { get; set; } = sp => sp.GetRequiredService<StoreActivityExecutionLogSink>();

    /// <summary>
    /// A factory that instantiates a log record sink for an <see cref="WorkflowExecutionLogRecord"/>.
    /// </summary>
    public Func<IServiceProvider, ILogRecordSink<WorkflowExecutionLogRecord>> WorkflowExecutionLogSink { get; set; } = sp => sp.GetRequiredService<StoreWorkflowExecutionLogSink>();

    /// <summary>
    /// A factory that instantiates an <see cref="ICommandHandler"/>.
    /// </summary>
    public Func<IServiceProvider, ICommandHandler> DispatchWorkflowCommandHandler { get; set; } = sp => sp.GetRequiredService<DispatchWorkflowCommandHandler>();

    /// <summary>
    /// A factory that instantiates an <see cref="IWorkflowResumer"/>.
    /// </summary>
    public Func<IServiceProvider, IWorkflowResumer> WorkflowResumer { get; set; } = sp => sp.GetRequiredService<WorkflowResumer>();

    /// <summary>
    /// A factory that instantiates an <see cref="IBookmarkQueueWorker"/>.
    /// </summary>
    public Func<IServiceProvider, IBookmarkQueueWorker> BookmarkQueueWorker { get; set; } = sp => sp.GetRequiredService<BookmarkQueueWorker>();

    /// <summary>
    /// A delegate to configure the <see cref="DistributedLockingOptions"/>.
    /// </summary>
    public Action<DistributedLockingOptions> DistributedLockingOptions { get; set; } = _ => { };

    /// <summary>
    /// A delegate to configure the <see cref="WorkflowInboxCleanupOptions"/>.
    /// </summary>
    [Obsolete("Will be removed in a future version")]
    public Action<WorkflowInboxCleanupOptions> WorkflowInboxCleanupOptions { get; set; } = _ => { };

    /// <summary>
    /// A delegate to configure the <see cref="WorkflowDispatcherOptions"/>.
    /// </summary>
    public Action<WorkflowDispatcherOptions> WorkflowDispatcherOptions { get; set; } = _ => { };
    
    /// <summary>
    /// A delegate to configure the <see cref="BookmarkQueuePurgeOptions"/>.
    /// </summary>
    public Action<BookmarkQueuePurgeOptions> BookmarkQueuePurgeOptions { get; set; } = _ => { };

    /// <summary>
    /// Enables the workflow inbox cleanup job. 
    /// </summary>
    public WorkflowRuntimeFeature EnableWorkflowInboxCleanupJob()
    {
        Services.Configure<WorkflowInboxCleanupOptions>(options => { options.IsEnabled = true; });
        return this;
    }

    /// <summary>
    /// Disables the workflow inbox cleanup job.
    /// </summary>
    public WorkflowRuntimeFeature DisableWorkflowInboxCleanupJob()
    {
        Services.Configure<WorkflowInboxCleanupOptions>(options => { options.IsEnabled = false; });
        return this;
    }

    /// <summary>
    /// Register the specified workflow type.
    /// </summary>
    public WorkflowRuntimeFeature AddWorkflow<T>() where T : IWorkflow
    {
        Workflows.Add<T>();
        return this;
    }

    /// <summary>
    /// Register all workflows in the specified assembly.
    /// </summary>
    [RequiresUnreferencedCode("The assembly is required to be referenced.")]
    public WorkflowRuntimeFeature AddWorkflowsFrom(Assembly assembly)
    {
        var workflowTypes = assembly.GetExportedTypes()
            .Where(x => typeof(IWorkflow).IsAssignableFrom(x) && x is { IsAbstract: false, IsInterface: false, IsGenericType: false })
            .ToList();

        foreach (var workflowType in workflowTypes)
            Workflows.Add(workflowType);

        return this;
    }

    /// <summary>
    /// Adds a dispatcher channel.
    /// </summary>
    public WorkflowRuntimeFeature AddDispatcherChannel(string channel)
    {
        return AddDispatcherChannel(new DispatcherChannel
        {
            Name = channel
        });
    }

    /// <summary>
    /// Adds a dispatcher channel.
    /// </summary>
    public WorkflowRuntimeFeature AddDispatcherChannel(DispatcherChannel channel)
    {
        WorkflowDispatcherChannels[channel.Name] = channel;
        return this;
    }

    /// <inheritdoc />
    public override void Configure()
    {
        Module.AddActivitiesFrom<WorkflowRuntimeFeature>();
        Module.Configure<WorkflowsFeature>(workflows =>
        {
            // ExecutionCycleAwareCommitStateHandler decorates DefaultCommitStateHandler — disposes the execution cycle handle AFTER
            // commit so the drain orchestrator's await-disposed sequencing can land its Interrupted write last.
            workflows.CommitStateHandler = sp => sp.GetRequiredService<Elsa.Workflows.Runtime.Services.ExecutionCycleAwareCommitStateHandler>();
        });

        Services.Configure<RecurringTaskOptions>(options =>
        {
            options.Schedule.ConfigureTask<TriggerBookmarkQueueRecurringTask>(TimeSpan.FromSeconds(10));
        });
    }

    /// <inheritdoc />
    /// <summary>
    /// Callback that tunes the graceful-shutdown machinery (drain deadline, per-source pause timeout, stimulus-queue back-pressure,
    /// pause-persistence policy). Applied when <see cref="Apply"/> binds <see cref="GracefulShutdownOptions"/>.
    /// </summary>
    public Action<GracefulShutdownOptions>? GracefulShutdown { get; set; }

    public override void Apply()
    {
        // Options.
        Services.Configure(DistributedLockingOptions);
        Services.Configure(WorkflowInboxCleanupOptions);
        Services.Configure(WorkflowDispatcherOptions);
        Services.Configure(BookmarkQueuePurgeOptions);
        Services.Configure<RuntimeOptions>(options => { options.Workflows = Workflows; });
        Services.Configure<WorkflowDispatcherOptions>(options =>
        {
            options.Channels.AddRange(WorkflowDispatcherChannels.Values);
        });
        Services.Configure<GracefulShutdownOptions>(options =>
        {
            GracefulShutdown?.Invoke(options);
            options.Validate();
        });

        // Graceful-shutdown core (US1 — quiescence machinery).
        Services
            .AddSingleton<IQuiescenceSignal, Elsa.Workflows.Runtime.Services.QuiescenceSignal>()
            .AddSingleton<IIngressSourceRegistry, Elsa.Workflows.Runtime.Services.IngressSourceRegistry>()
            .AddSingleton<IExecutionCycleRegistry, Elsa.Workflows.Runtime.Services.ExecutionCycleRegistry>()
            // Lazy collection breaks the otherwise-circular DI chain QuiescenceSignal → IExecutionCycleRegistry →
            // IIngressSourceRegistry → IEnumerable<IIngressSource> → IQuiescenceSignal. Adapters take a direct
            // IQuiescenceSignal dependency; the registry materializes the collection on first read.
            .AddSingleton(sp => new Lazy<IEnumerable<IIngressSource>>(sp.GetServices<IIngressSource>))
            // Drain orchestrator + hosted service (US1). See FR-029 / R5 — heartbeat must outlive drain.
            .AddSingleton<IDrainOrchestrator, Elsa.Workflows.Runtime.Services.DrainOrchestrator>()
            .AddHostedService<Elsa.Workflows.Runtime.HostedServices.DrainOrchestratorHostedService>()
            // Domain service that backs all runtime-admin transports (US2). Encapsulates the audit-on-effective-
            // transition rule (SC-007) so transports stay thin. Scoped because INotificationSender is scoped.
            .AddScoped<IWorkflowRuntimeAdminService, Elsa.Workflows.Runtime.Services.WorkflowRuntimeAdminService>()
            // Interrupted-workflow recovery on shell activation (US3). Disjoint from the timeout-based
            // RestartInterruptedWorkflowsTask: filter is SubStatus = Interrupted; that task's filter is IsExecuting=true.
            .AddScoped<IInterruptedRecoveryScanner, Elsa.Workflows.Runtime.Services.InterruptedRecoveryScanner>()
            .AddStartupTask<Elsa.Workflows.Runtime.StartupTasks.RecoverInterruptedWorkflowsStartupTask>()
            // Internal bookmark-queue processor surfaced as an ingress source for diagnostic visibility (FR-006).
            // Pause behavior is enforced inside BookmarkQueueProcessor via IQuiescenceSignal (FR-024).
            .AddSingleton<IIngressSource, Elsa.Workflows.Runtime.IngressSources.InternalBookmarkQueueIngressSource>()
            // Re-applies persisted pause state on activation when PausePersistence = AcrossReactivations (FR-028).
            .AddStartupTask<Elsa.Workflows.Runtime.StartupTasks.InitializePauseStateStartupTask>();

        Services
            // Core.
            .AddScoped<ITriggerIndexer, TriggerIndexer>()
            .AddScoped<IWorkflowInstanceFactory, WorkflowInstanceFactory>()
            .AddScoped<IWorkflowHostFactory, WorkflowHostFactory>()
            .AddScoped<IBackgroundActivityInvoker, BackgroundActivityInvoker>()
            .AddScoped(WorkflowRuntime)
            .AddScoped(WorkflowDispatcher)
            .AddScoped(StimulusDispatcher)
            .AddScoped(WorkflowCancellationDispatcher)
            .AddScoped(RunTaskDispatcher)
            .AddScoped(ActivityExecutionLogSink)
            .AddScoped(WorkflowExecutionLogSink)
            .AddSingleton(BackgroundActivityScheduler)
            .AddSingleton<RandomLongIdentityGenerator>()
            .AddSingleton<IBookmarkQueueSignaler, BookmarkQueueSignaler>()
            .AddScoped(BookmarkQueueWorker)
            .AddScoped<IBookmarkManager, DefaultBookmarkManager>()
            .AddScoped<IActivityExecutionManager, DefaultActivityExecutionManager>()
            .AddScoped<IActivityExecutionStatsService, ActivityExecutionStatsService>()
            .AddScoped<IActivityExecutionMapper, DefaultActivityExecutionMapper>()
            .AddScoped<IWorkflowDefinitionStorePopulator, DefaultWorkflowDefinitionStorePopulator>()
            .AddScoped<IRegistriesPopulator, DefaultRegistriesPopulator>()
            .AddScoped<IWorkflowDefinitionsRefresher, WorkflowDefinitionsRefresher>()
            .AddScoped<IWorkflowDefinitionsReloader, WorkflowDefinitionsReloader>()
            .AddScoped<IWorkflowRegistry, DefaultWorkflowRegistry>()
            .AddScoped<IWorkflowMatcher, WorkflowMatcher>()
            .AddScoped<IWorkflowInvoker, WorkflowInvoker>()
            .AddScoped<IStimulusSender, StimulusSender>()
            .AddScoped<ITriggerBoundWorkflowService, TriggerBoundWorkflowService>()
            .AddScoped<IBookmarkBoundWorkflowService, BookmarkBoundWorkflowService>()
            .AddScoped<ITaskReporter, TaskReporter>()
            .AddScoped<SynchronousTaskDispatcher>()
            .AddScoped<BackgroundTaskDispatcher>()
            .AddScoped<StoreActivityExecutionLogSink>()
            .AddScoped<StoreWorkflowExecutionLogSink>()
            .AddScoped<DispatchWorkflowCommandHandler>()
            .AddScoped<IEventPublisher, EventPublisher>()
            .AddScoped<IBookmarkUpdater, BookmarkUpdater>()
            .AddScoped<IBookmarksPersister, BookmarksPersister>()
            .AddScoped<IBookmarkResumer, BookmarkResumer>()
            .AddScoped<IBookmarkQueue, StoreBookmarkQueue>()
            .AddScoped(WorkflowResumer)
            .AddScoped<WorkflowResumer>()
            .AddScoped(BookmarkQueueWorker)
            .AddScoped<BookmarkQueueWorker>()
            .AddScoped<ITriggerInvoker, TriggerInvoker>()
            .AddScoped<IWorkflowCanceler, WorkflowCanceler>()
            .AddScoped<IWorkflowCancellationService, WorkflowCancellationService>()
            .AddScoped<IWorkflowActivationStrategyEvaluator, DefaultWorkflowActivationStrategyEvaluator>()
            .AddScoped<IWorkflowStarter, DefaultWorkflowStarter>()
            .AddScoped<IWorkflowRestarter, DefaultWorkflowRestarter>()
            .AddScoped<IBookmarkQueuePurger, DefaultBookmarkQueuePurger>()
            .AddScoped<ILogRecordExtractor<WorkflowExecutionLogRecord>, WorkflowExecutionLogRecordExtractor>()
            .AddScoped<IActivityPropertyLogPersistenceEvaluator, ActivityPropertyLogPersistenceEvaluator>()
            .AddScoped<IBookmarkQueueProcessor, BookmarkQueueProcessor>()
            .AddScoped<DefaultCommitStateHandler>()
            .AddScoped<Elsa.Workflows.Runtime.Services.ExecutionCycleAwareCommitStateHandler>()
            .AddScoped<WorkflowHeartbeatGeneratorFactory>()

            // Deprecated services.
            .AddScoped<IWorkflowInbox, StimulusProxyWorkflowInbox>()

            // Stores.
            .AddScoped(BookmarkStore)
            .AddScoped(BookmarkQueueStore)
            .AddScoped(TriggerStore)
            .AddScoped(WorkflowExecutionLogStore)
            .AddScoped(ActivityExecutionLogStore)

            // Lazy services.
            .AddScoped<Func<IEnumerable<IWorkflowsProvider>>>(sp => sp.GetServices<IWorkflowsProvider>)
            .AddScoped<Func<IEnumerable<IWorkflowMaterializer>>>(sp => sp.GetServices<IWorkflowMaterializer>)

            // Noop stores.
            .AddScoped<MemoryWorkflowExecutionLogStore>()
            .AddScoped<MemoryActivityExecutionStore>()

            // Memory stores.
            .AddMemoryStore<StoredBookmark, MemoryBookmarkStore>()
            .AddMemoryStore<StoredTrigger, MemoryTriggerStore>()
            .AddMemoryStore<BookmarkQueueItem, MemoryBookmarkQueueStore>()
            .AddMemoryStore<WorkflowExecutionLogRecord, MemoryWorkflowExecutionLogStore>()
            .AddMemoryStore<ActivityExecutionRecord, MemoryActivityExecutionStore>()
            
            // Startup tasks, background tasks, and recurring tasks.
            .AddStartupTask<PopulateRegistriesStartupTask>()
            .AddRecurringTask<TriggerBookmarkQueueRecurringTask>(TimeSpan.FromMinutes(1))
            .AddRecurringTask<PurgeBookmarkQueueRecurringTask>(TimeSpan.FromSeconds(10))
            .AddRecurringTask<RestartInterruptedWorkflowsTask>(TimeSpan.FromMinutes(5)) // Same default as the workflow liveness threshold.
            
            // Distributed locking.
            .AddSingleton(DistributedLockProvider)

            // Workflow providers.
            .AddWorkflowsProvider<ClrWorkflowsProvider>()
            
            // UI property handlers.
            .AddScoped<IPropertyUIHandler, DispatcherChannelOptionsProvider>()

            // Domain handlers.
            .AddCommandHandler<DispatchWorkflowCommandHandler>()
            .AddCommandHandler<DispatchStimulusCommandHandler>()
            .AddCommandHandler<CancelWorkflowsCommandHandler>()
            .AddNotificationHandler<ResumeDispatchWorkflowActivity>()
            .AddNotificationHandler<ResumeBulkDispatchWorkflowActivity>()
            .AddNotificationHandler<ResumeExecuteWorkflowActivity>()
            .AddNotificationHandler<IndexTriggers>()
            .AddNotificationHandler<CancelBackgroundActivities>()
            .AddNotificationHandler<DeleteBookmarks>()
            .AddNotificationHandler<DeleteTriggers>()
            .AddNotificationHandler<DeleteActivityExecutionLogRecords>()
            .AddNotificationHandler<DeleteWorkflowExecutionLogRecords>()
            .AddNotificationHandler<RefreshActivityRegistry>()
            .AddNotificationHandler<SignalBookmarkQueueWorker>()
            .AddNotificationHandler<EvaluateParentLogPersistenceModes>()
            .AddNotificationHandler<CaptureActivityExecutionState>()
            .AddNotificationHandler<ValidateWorkflowRequestHandler>()

            // Workflow activation strategies.
            .AddScoped<IWorkflowActivationStrategy, SingletonStrategy>()
            .AddScoped<IWorkflowActivationStrategy, CorrelatedSingletonStrategy>()
            .AddScoped<IWorkflowActivationStrategy, CorrelationStrategy>()
            ;
    }
}