using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using CShells.Features;
using Elsa.Common;
using Elsa.Common.RecurringTasks;
using Elsa.Extensions;
using Elsa.Mediator.Contracts;
using Elsa.Workflows.CommitStates;
using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Contracts;
using Elsa.Workflows.Management.Services;
using Elsa.Workflows.Options;
using Elsa.Workflows.Runtime.ActivationValidators;
using Elsa.Workflows.Runtime.Discovery;
using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.Runtime.Handlers;
using Elsa.Workflows.Runtime.Options;
using Elsa.Workflows.Runtime.Providers;
using Elsa.Workflows.Runtime.Services;
using Elsa.Workflows.Runtime.Stores;
using Elsa.Workflows.Runtime.Tasks;
using Elsa.Workflows.Runtime.UIHints;
using Medallion.Threading;
using Medallion.Threading.FileSystem;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Elsa.Common.Serialization;

namespace Elsa.Workflows.Runtime.ShellFeatures;

/// <summary>
/// Installs and configures workflow runtime features.
/// </summary>
[ShellFeature(
    DisplayName = "Workflow Runtime",
    Description = "Provides workflow execution runtime and scheduling capabilities",
    DependsOn = [typeof(global::Elsa.Workflows.ShellFeatures.WorkflowsFeature)])]
public class WorkflowRuntimeFeature : IShellFeature
{
    private IDictionary<string, DispatcherChannel> WorkflowDispatcherChannels { get; set; } = new Dictionary<string, DispatcherChannel>();

    /// <summary>
    /// A list of workflow builders configured during application startup.
    /// </summary>
    public IDictionary<string, Func<IServiceProvider, ValueTask<IWorkflow>>> Workflows { get; set; } = new Dictionary<string, Func<IServiceProvider, ValueTask<IWorkflow>>>();
    private ISet<Type> WorkflowTypes { get; } = new HashSet<Type>();

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
        var transactionalService = ActivatorUtilities.CreateInstance<TransactionalWorkflowDispatcher>(sp, decoratedService);
        return ActivatorUtilities.CreateInstance<ValidatingWorkflowDispatcher>(sp, transactionalService);
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
    /// A factory that instantiates an <see cref="IWorkflowDispatchOutboxStore"/>.
    /// </summary>
    public Func<IServiceProvider, IWorkflowDispatchOutboxStore> WorkflowDispatchOutboxStore { get; set; } = sp => ActivatorUtilities.CreateInstance<KeyValueWorkflowDispatchOutboxStore>(sp);

    /// <summary>
    /// A factory that instantiates an <see cref="IBookmarkStore"/>.
    /// </summary>
    public Func<IServiceProvider, IBookmarkStore> BookmarkStore { get; set; } = sp => sp.GetRequiredService<MemoryBookmarkStore>();

    /// <summary>
    /// A factory that instantiates an <see cref="IBookmarkQueueStore"/>.
    /// </summary>
    public Func<IServiceProvider, IBookmarkQueueStore> BookmarkQueueStore { get; set; } = sp => sp.GetRequiredService<MemoryBookmarkQueueStore>();

    /// <summary>
    /// A factory that instantiates an <see cref="IBookmarkQueueDeadLetterStore"/>.
    /// </summary>
    public Func<IServiceProvider, IBookmarkQueueDeadLetterStore> BookmarkQueueDeadLetterStore { get; set; } = sp => sp.GetRequiredService<MemoryBookmarkQueueDeadLetterStore>();

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
    public Func<IServiceProvider, IDistributedLockProvider> DistributedLockProvider { get; set; } = _ => new FileDistributedSynchronizationProvider(new DirectoryInfo(Path.Combine(Environment.CurrentDirectory, "App_Data", "locks")));

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
    /// Callback that tunes the graceful-shutdown machinery (drain deadline, per-source pause timeout, stimulus-queue back-pressure,
    /// pause-persistence policy). Applied when <see cref="ConfigureServices"/> binds <see cref="GracefulShutdownOptions"/>.
    /// </summary>
    public GracefulShutdownOptions? GracefulShutdown { get; set; }

    /// <summary>
    /// Register the specified workflow type.
    /// </summary>
    public WorkflowRuntimeFeature AddWorkflow<T>() where T : IWorkflow
    {
        return AddWorkflow(typeof(T));
    }

    /// <summary>
    /// Register the specified workflow type.
    /// </summary>
    public WorkflowRuntimeFeature AddWorkflow(Type workflowType)
    {
        Workflows.Add(workflowType);
        WorkflowTypes.Add(workflowType);
        return this;
    }

    /// <summary>
    /// Register all workflows in the specified assembly.
    /// </summary>
    [RequiresUnreferencedCode("The assembly is required to be referenced.")]
    public WorkflowRuntimeFeature AddWorkflowsFrom(Assembly assembly)
    {
        foreach (var workflowType in WorkflowTypeScanner.GetWorkflowTypes(assembly))
            AddWorkflow(workflowType);

        return this;
    }

    public void ConfigureServices(IServiceCollection services)
    {
        // Options.
        services.Configure<SerializationTypeOptions>(RegisterWorkflowTypeAliases);
        services.Configure<RuntimeOptions>(options => { options.Workflows = Workflows; });
        services.Configure<WorkflowDispatcherOptions>(options =>
        {
            options.Channels.AddRange(WorkflowDispatcherChannels.Values);
        });
        services.AddGracefulShutdownOptions(options =>
        {
            if (GracefulShutdown == null)
                return;

            options.DrainDeadline = GracefulShutdown.DrainDeadline;
            options.IngressPauseTimeout = GracefulShutdown.IngressPauseTimeout;
            options.StimulusQueueMaxDepthWhilePaused = GracefulShutdown.StimulusQueueMaxDepthWhilePaused;
            options.OverflowPolicy = GracefulShutdown.OverflowPolicy;
            options.PausePersistence = GracefulShutdown.PausePersistence;
            options.MaxForceCancelledInstanceIdsReported = GracefulShutdown.MaxForceCancelledInstanceIdsReported;
        });

        services
            // Graceful-shutdown core (US1 — quiescence machinery).
            // Per-shell QuiescenceSignal: the persistence key includes the shell id so multi-shell deployments under
            // PausePersistencePolicy.AcrossReactivations don't collide on a single key. Falls back to "default" only
            // when ShellSettings isn't in the DI graph (degenerate single-shell or non-CShells host).
            .AddSingleton<IQuiescenceSignal>(sp => new QuiescenceSignal(
                sp.GetRequiredService<IOptions<GracefulShutdownOptions>>(),
                sp.GetRequiredService<ISystemClock>(),
                sp.GetRequiredService<IExecutionCycleRegistry>(),
                sp.GetRequiredService<IServiceScopeFactory>(),
                shellName: sp.GetService<CShells.ShellSettings>()?.Id))
            .AddSingleton<IIngressSourceRegistry, IngressSourceRegistry>()
            .AddSingleton<IExecutionCycleRegistry, ExecutionCycleRegistry>()
            .AddSingleton(sp => new Lazy<IEnumerable<IIngressSource>>(sp.GetServices<IIngressSource>))
            .AddSingleton<IDrainOrchestrator, DrainOrchestrator>()
            .AddScoped<IWorkflowRuntimeAdminService, WorkflowRuntimeAdminService>()
            .AddTransient<CShells.Lifecycle.IDrainHandler, Lifecycle.ElsaShellDrainHandler>()
            .AddScoped<IInterruptedRecoveryScanner, InterruptedRecoveryScanner>()
            .AddStartupTask<StartupTasks.RecoverInterruptedWorkflowsStartupTask>()
            .AddSingleton<IIngressSource, IngressSources.InternalBookmarkQueueIngressSource>()
            .AddTransient<CShells.Lifecycle.IShellInitializer, Lifecycle.InitializePauseStateShellInitializer>()
            
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
            .AddScoped<IBookmarkQueueDeadLetterManager, BookmarkQueueDeadLetterManager>()
            .AddScoped<WorkflowResumer>()
            .AddScoped<BookmarkQueueWorker>()
            .AddScoped(WorkflowResumer)
            .AddScoped<ITriggerInvoker, TriggerInvoker>()
            .AddScoped<IWorkflowCanceler, WorkflowCanceler>()
            .AddScoped<IWorkflowCancellationService, WorkflowCancellationService>()
            .AddScoped<IWorkflowActivationStrategyEvaluator, DefaultWorkflowActivationStrategyEvaluator>()
            .AddScoped<IWorkflowStarter, DefaultWorkflowStarter>()
            .AddScoped<IWorkflowRestarter, DefaultWorkflowRestarter>()
            .AddScoped<IBookmarkQueuePurger, DefaultBookmarkQueuePurger>()
            .AddSingleton<IWorkflowDispatchOutboxAccessor, WorkflowDispatchOutboxAccessor>()
            .AddScoped<ILogRecordExtractor<WorkflowExecutionLogRecord>, WorkflowExecutionLogRecordExtractor>()
            .AddScoped<IActivityPropertyLogPersistenceEvaluator, ActivityPropertyLogPersistenceEvaluator>()
            .AddScoped<IBookmarkQueueProcessor, BookmarkQueueProcessor>()
            .AddScoped<DefaultCommitStateHandler>()
            // Decorator: disposes the execution cycle handle AFTER the workflow runner's terminal commit has persisted state,
            // so the drain orchestrator's force-cancel path can sequence its Interrupted write to land last.
            .AddScoped<ICommitStateHandler, ExecutionCycleAwareCommitStateHandler>()
            .AddScoped<WorkflowHeartbeatGeneratorFactory>()

            // Deprecated services.
            .AddScoped<IWorkflowInbox, StimulusProxyWorkflowInbox>()

            // Stores.
            .AddScoped(BookmarkStore)
            .AddScoped(BookmarkQueueStore)
            .AddScoped(BookmarkQueueDeadLetterStore)
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
            .AddMemoryStore<BookmarkQueueDeadLetterItem, MemoryBookmarkQueueDeadLetterStore>()
            .AddMemoryStore<WorkflowExecutionLogRecord, MemoryWorkflowExecutionLogStore>()
            .AddMemoryStore<ActivityExecutionRecord, MemoryActivityExecutionStore>()

            // Startup tasks, background tasks, and recurring tasks.
            .AddStartupTask<PopulateRegistriesStartupTask>()
            .AddRecurringTask<TriggerBookmarkQueueRecurringTask>(TimeSpan.FromMinutes(1))
            .AddRecurringTask<PurgeBookmarkQueueRecurringTask>(TimeSpan.FromSeconds(10))
            .AddRecurringTask<RestartInterruptedWorkflowsTask>(TimeSpan.FromMinutes(5)) // Same default as the workflow liveness threshold.
            .AddRecurringTask<ProcessWorkflowDispatchOutboxRecurringTask>(TimeSpan.FromSeconds(10))

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
            .AddNotificationHandler<ProcessWorkflowDispatchOutbox>()
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

        services.TryAddScoped<IWorkflowDispatchOutbox>(sp => ActivatorUtilities.CreateInstance<WorkflowDispatchOutbox>(sp));
        services.TryAddScoped(WorkflowDispatchOutboxStore);
        services.TryAddScoped<IWorkflowDispatchOutboxProcessor, WorkflowDispatchOutboxProcessor>();
    }

    private void RegisterWorkflowTypeAliases(SerializationTypeOptions options)
    {
        WorkflowRuntimeTypeAliasRegistrar.Register(options, GetRegisteredWorkflowTypes());
    }

    private IEnumerable<Type> GetRegisteredWorkflowTypes()
    {
        return WorkflowTypes
            .Concat(Workflows.Keys.Select(TryResolveWorkflowType).Where(type => type != null).Select(type => type!))
            .Distinct();
    }

    private static Type? TryResolveWorkflowType(string typeName)
    {
        Type? type;

        try
        {
            type = Type.GetType(typeName, false);
        }
        catch (Exception e) when (e is ArgumentException or FileLoadException or FileNotFoundException or TypeLoadException or BadImageFormatException)
        {
            return null;
        }

        return type != null && typeof(IWorkflow).IsAssignableFrom(type) && type is { IsAbstract: false, IsInterface: false, ContainsGenericParameters: false }
            ? type
            : null;
    }
}
