using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Elsa.Common.Features;
using Elsa.Extensions;
using Elsa.Features.Abstractions;
using Elsa.Features.Attributes;
using Elsa.Features.Services;
using Elsa.Mediator.Contracts;
using Elsa.Workflows.Contracts;
using Elsa.Workflows.Features;
using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Contracts;
using Elsa.Workflows.Management.Handlers;
using Elsa.Workflows.Management.Services;
using Elsa.Workflows.Runtime.ActivationValidators;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.Runtime.Handlers;
using Elsa.Workflows.Runtime.HostedServices;
using Elsa.Workflows.Runtime.Options;
using Elsa.Workflows.Runtime.Providers;
using Elsa.Workflows.Runtime.Stores;
using Medallion.Threading;
using Medallion.Threading.FileSystem;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.Runtime.Features;

/// Installs and configures workflow runtime features.
[DependsOn(typeof(SystemClockFeature))]
public class WorkflowRuntimeFeature : FeatureBase
{
    /// <inheritdoc />
    public WorkflowRuntimeFeature(IModule module) : base(module)
    {
    }

    private IDictionary<string, DispatcherChannel> WorkflowDispatcherChannels { get; set; } = new Dictionary<string, DispatcherChannel>();
    
    /// A list of workflow builders configured during application startup.
    public IDictionary<string, Func<IServiceProvider, ValueTask<IWorkflow>>> Workflows { get; set; } = new Dictionary<string, Func<IServiceProvider, ValueTask<IWorkflow>>>();
    
    /// A factory that instantiates a concrete <see cref="IWorkflowRuntime"/>.
    public Func<IServiceProvider, IWorkflowRuntime> WorkflowRuntime { get; set; } = sp => ActivatorUtilities.CreateInstance<LocalWorkflowRuntime>(sp);
    
    /// A factory that instantiates an <see cref="IWorkflowDispatcher"/>.
    public Func<IServiceProvider, IWorkflowDispatcher> WorkflowDispatcher { get; set; } = sp =>
    {
        var decoratedService = ActivatorUtilities.CreateInstance<BackgroundWorkflowDispatcher>(sp);
        return ActivatorUtilities.CreateInstance<ValidatingWorkflowDispatcher>(sp, decoratedService);
    };
    
    /// A factory that instantiates an <see cref="IWorkflowCancellationDispatcher"/>.
    public Func<IServiceProvider, IWorkflowCancellationDispatcher> WorkflowCancellationDispatcher { get; set; } = sp => ActivatorUtilities.CreateInstance<BackgroundWorkflowCancellationDispatcher>(sp);

    /// A factory that instantiates an <see cref="IBookmarkStore"/>.
    public Func<IServiceProvider, IBookmarkStore> BookmarkStore { get; set; } = sp => sp.GetRequiredService<MemoryBookmarkStore>();
    
    /// A factory that instantiates an <see cref="IBookmarkQueueItemStore"/>.
    public Func<IServiceProvider, IBookmarkQueueItemStore> BookmarkQueueItemStore { get; set; } = sp => sp.GetRequiredService<MemoryBookmarkQueueItemStore>();
    
    /// A factory that instantiates an <see cref="ITriggerStore"/>.
    public Func<IServiceProvider, ITriggerStore> TriggerStore { get; set; } = sp => sp.GetRequiredService<MemoryTriggerStore>();
    
    /// A factory that instantiates an <see cref="IWorkflowExecutionLogStore"/>.
    public Func<IServiceProvider, IWorkflowExecutionLogStore> WorkflowExecutionLogStore { get; set; } = sp => sp.GetRequiredService<MemoryWorkflowExecutionLogStore>();
    
    /// A factory that instantiates an <see cref="IActivityExecutionStore"/>.
    public Func<IServiceProvider, IActivityExecutionStore> ActivityExecutionLogStore { get; set; } = sp => sp.GetRequiredService<MemoryActivityExecutionStore>();
    
    /// A factory that instantiates an <see cref="IWorkflowExecutionContextStore"/>.
    public Func<IServiceProvider, IWorkflowExecutionContextStore> WorkflowExecutionContextStore { get; set; } = sp => sp.GetRequiredService<MemoryWorkflowExecutionContextStore>();
    
    /// A factory that instantiates an <see cref="IDistributedLockProvider"/>.
    public Func<IServiceProvider, IDistributedLockProvider> DistributedLockProvider { get; set; } = _ => new FileDistributedSynchronizationProvider(new DirectoryInfo(Path.Combine(Environment.CurrentDirectory, "App_Data/locks")));
    
    /// A factory that instantiates an <see cref="ITaskDispatcher"/>.
    public Func<IServiceProvider, ITaskDispatcher> RunTaskDispatcher { get; set; } = sp => sp.GetRequiredService<BackgroundTaskDispatcher>();
    
    /// A factory that instantiates an <see cref="IBackgroundActivityScheduler"/>.
    public Func<IServiceProvider, IBackgroundActivityScheduler> BackgroundActivityScheduler { get; set; } = sp => ActivatorUtilities.CreateInstance<LocalBackgroundActivityScheduler>(sp);
    
    /// Represents a sink for workflow execution logs.
    public Func<IServiceProvider, IWorkflowExecutionLogSink> WorkflowExecutionLogSink { get; set; } = sp => sp.GetRequiredService<StoreWorkflowExecutionLogSink>();
    
    /// A factory that instantiates an <see cref="ICommandHandler"/>.
    public Func<IServiceProvider, ICommandHandler> DispatchWorkflowCommandHandler { get; set; } = sp => sp.GetRequiredService<DispatchWorkflowCommandHandler>();
    
    public Func<IServiceProvider, IBookmarkQueueWorker> BookmarkQueueWorker { get; set; } = sp => sp.GetRequiredService<BookmarkQueueWorker>();
    
    /// A delegate to configure the <see cref="DistributedLockingOptions"/>.
    public Action<DistributedLockingOptions> DistributedLockingOptions { get; set; } = _ => { };
    
    /// A delegate to configure the <see cref="WorkflowInboxCleanupOptions"/>.
    [Obsolete("Will be removed in a future version")]
    public Action<WorkflowInboxCleanupOptions> WorkflowInboxCleanupOptions { get; set; } = _ => { };
    
    /// A delegate to configure the <see cref="WorkflowDispatcherOptions"/>.
    public Action<WorkflowDispatcherOptions> WorkflowDispatcherOptions { get; set; } = _ => { };
    
    /// Register the specified workflow type.
    public WorkflowRuntimeFeature AddWorkflow<T>() where T : IWorkflow
    {
        Workflows.Add<T>();
        return this;
    }
    
    /// Register all workflows in the specified assembly.
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
    
    /// Adds a dispatcher channel.
    public WorkflowRuntimeFeature AddDispatcherChannel(string channel)
    {
        return AddDispatcherChannel(new DispatcherChannel
        {
            Name = channel
        });
    }
    
    /// Adds a dispatcher channel.
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
            workflows.CommitStateHandler = sp => sp.GetRequiredService<StoreCommitStateHandler>();
        });
    }

    /// <inheritdoc />
    public override void ConfigureHostedServices()
    {
        Module
            .ConfigureHostedService<PopulateRegistriesHostedService>(1)
            .ConfigureHostedService<TriggerBookmarkQueueWorker>(1);
    }

    /// <inheritdoc />
    public override void Apply()
    {
        // Options.
        Services.Configure(DistributedLockingOptions);
        Services.Configure(WorkflowInboxCleanupOptions);
        Services.Configure(WorkflowDispatcherOptions);
        Services.Configure<RuntimeOptions>(options => { options.Workflows = Workflows; });
        Services.Configure<WorkflowDispatcherOptions>(options =>
        {
            options.Channels.AddRange(WorkflowDispatcherChannels.Values);
        });

        Services
            // Core.
            .AddScoped<ITriggerIndexer, TriggerIndexer>()
            .AddScoped<IWorkflowInstanceFactory, WorkflowInstanceFactory>()
            .AddScoped<IWorkflowHostFactory, WorkflowHostFactory>()
            .AddScoped<IBackgroundActivityInvoker, BackgroundActivityInvoker>()
            .AddScoped(WorkflowRuntime)
            .AddScoped(WorkflowDispatcher)
            .AddScoped(WorkflowCancellationDispatcher)
            .AddScoped(WorkflowExecutionContextStore)
            .AddScoped(RunTaskDispatcher)
            .AddScoped(WorkflowExecutionLogSink)
            .AddSingleton(BackgroundActivityScheduler)
            .AddSingleton<RandomLongIdentityGenerator>()
            .AddSingleton<IBookmarkQueueWorkerSignaler, BookmarkQueueWorkerSignaler>()
            .AddSingleton<IBookmarkQueueWorker, BookmarkQueueWorker>()
            .AddScoped<IBookmarkManager, DefaultBookmarkManager>()
            .AddScoped<IActivityExecutionManager, DefaultActivityExecutionManager>()
            .AddScoped<IActivityExecutionStatsService, ActivityExecutionStatsService>()
            .AddScoped<IActivityExecutionMapper, DefaultActivityExecutionMapper>()
            .AddScoped<IWorkflowDefinitionStorePopulator, DefaultWorkflowDefinitionStorePopulator>()
            .AddScoped<IRegistriesPopulator, DefaultRegistriesPopulator>()
            .AddScoped<IWorkflowDefinitionsRefresher, WorkflowDefinitionsRefresher>()
            .AddScoped<IWorkflowRegistry, DefaultWorkflowRegistry>()
            .AddScoped<IWorkflowMatcher, WorkflowMatcher>()
            .AddScoped<IWorkflowInvoker, WorkflowInvoker>()
            .AddScoped<IStimulusSender, StimulusSender>()
            .AddScoped<ITriggerBoundWorkflowService, TriggerBoundWorkflowService>()
            .AddScoped<IBookmarkBoundWorkflowService, BookmarkBoundWorkflowService>()
            .AddScoped<ITaskReporter, TaskReporter>()
            .AddScoped<SynchronousTaskDispatcher>()
            .AddScoped<BackgroundTaskDispatcher>()
            .AddScoped<StoreWorkflowExecutionLogSink>()
            .AddScoped<DispatchWorkflowCommandHandler>()
            .AddScoped<IEventPublisher, EventPublisher>()
            .AddScoped<IBookmarkUpdater, BookmarkUpdater>()
            .AddScoped<IBookmarksPersister, BookmarksPersister>()
            .AddScoped<IBookmarkResumer, BookmarkResumer>()
            .AddScoped<IBookmarkQueue, StoreBookmarkQueue>()
            .AddScoped<IWorkflowCanceler, WorkflowCanceler>()
            .AddScoped<IWorkflowCancellationService, WorkflowCancellationService>()
            .AddScoped<IBookmarkQueueProcessor, BookmarkQueueProcessor>()
            .AddScoped<StoreCommitStateHandler>()
            
            // Deprecated services.
            .AddScoped<IWorkflowInbox, StimulusProxyWorkflowInbox>()
            
            // Stores.
            .AddScoped(BookmarkStore)
            .AddScoped(BookmarkQueueItemStore)
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
            .AddMemoryStore<BookmarkQueueItem, MemoryBookmarkQueueItemStore>()
            .AddMemoryStore<WorkflowExecutionLogRecord, MemoryWorkflowExecutionLogStore>()
            .AddMemoryStore<ActivityExecutionRecord, MemoryActivityExecutionStore>()
            .AddMemoryStore<WorkflowExecutionContext, MemoryWorkflowExecutionContextStore>()

            // Distributed locking.
            .AddSingleton(DistributedLockProvider)

            // Workflow definition providers.
            .AddWorkflowDefinitionProvider<ClrWorkflowsProvider>()

            // Domain handlers.
            .AddCommandHandler<DispatchWorkflowCommandHandler>()
            .AddNotificationHandler<ResumeDispatchWorkflowActivity>()
            .AddNotificationHandler<ResumeBulkDispatchWorkflowActivity>()
            .AddNotificationHandler<IndexTriggers>()
            .AddNotificationHandler<CancelBackgroundActivities>()
            .AddNotificationHandler<DeleteBookmarks>()
            .AddNotificationHandler<DeleteTriggers>()
            .AddNotificationHandler<DeleteWorkflowInstances>()
            .AddNotificationHandler<DeleteActivityExecutionLogRecords>()
            .AddNotificationHandler<DeleteWorkflowExecutionLogRecords>()
            .AddNotificationHandler<WorkflowExecutionContextNotificationsHandler>()
            .AddNotificationHandler<SignalBookmarkQueueWorker>()

            // Workflow activation strategies.
            .AddScoped<IWorkflowActivationStrategy, SingletonStrategy>()
            .AddScoped<IWorkflowActivationStrategy, CorrelatedSingletonStrategy>()
            .AddScoped<IWorkflowActivationStrategy, CorrelationStrategy>()
            ;
        
    }
}