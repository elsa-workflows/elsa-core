using System.Reflection;
using Elsa.Common.Features;
using Elsa.Extensions;
using Elsa.Features.Abstractions;
using Elsa.Features.Attributes;
using Elsa.Features.Services;
using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Management.Contracts;
using Elsa.Workflows.Management.Handlers;
using Elsa.Workflows.Runtime.ActivationValidators;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.Runtime.Handlers;
using Elsa.Workflows.Runtime.HostedServices;
using Elsa.Workflows.Runtime.Options;
using Elsa.Workflows.Runtime.Providers;
using Elsa.Workflows.Runtime.Services;
using Elsa.Workflows.Runtime.Stores;
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
    public Func<IServiceProvider, IWorkflowDispatcher> WorkflowDispatcher { get; set; } = sp => ActivatorUtilities.CreateInstance<BackgroundWorkflowDispatcher>(sp);
    
    /// <summary>
    /// A factory that instantiates an <see cref="IBookmarkStore"/>.
    /// </summary>
    public Func<IServiceProvider, IBookmarkStore> BookmarkStore { get; set; } = sp => sp.GetRequiredService<MemoryBookmarkStore>();

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
    /// A factory that instantiates an <see cref="IWorkflowInboxMessageStore"/>.
    /// </summary>
    public Func<IServiceProvider, IWorkflowInboxMessageStore> WorkflowInboxStore { get; set; } = sp => sp.GetRequiredService<MemoryWorkflowInboxMessageStore>();

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
    public Func<IServiceProvider, IBackgroundActivityScheduler> BackgroundActivityInvoker { get; set; } = sp => ActivatorUtilities.CreateInstance<LocalBackgroundActivityScheduler>(sp);

    /// <summary>
    /// A delegate to configure the <see cref="DistributedLockingOptions"/>.
    /// </summary>
    public Action<DistributedLockingOptions> DistributedLockingOptions { get; set; } = _ => { };
    
    /// <summary>
    /// A delegate to configure the <see cref="WorkflowInboxCleanupOptions"/>.
    /// </summary>
    public Action<WorkflowInboxCleanupOptions> WorkflowInboxCleanupOptions { get; set; } = _ => { };

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
    public WorkflowRuntimeFeature AddWorkflowsFrom(Assembly assembly)
    {
        var workflowTypes = assembly.GetExportedTypes()
            .Where(x => typeof(IWorkflow).IsAssignableFrom(x) && x is { IsAbstract: false, IsInterface: false, IsGenericType: false })
            .ToList();
        
        foreach (var workflowType in workflowTypes)
            Workflows.Add(workflowType);
        
        return this;
    }

    /// <inheritdoc />
    public override void Configure()
    {
        // Activities
        Module.AddActivitiesFrom<WorkflowRuntimeFeature>();
    }

    /// <inheritdoc />
    public override void ConfigureHostedServices()
    {
        Module.ConfigureHostedService<PopulateRegistriesHostedService>();
        Module.ConfigureHostedService<WorkflowInboxCleanupHostedService>();
    }

    /// <inheritdoc />
    public override void Apply()
    {
        // Options.
        Services.Configure(DistributedLockingOptions);
        Services.Configure<RuntimeOptions>(options => { options.Workflows = Workflows; });
        Services.Configure(WorkflowInboxCleanupOptions);

        Services
            // Core.
            .AddSingleton<ITriggerIndexer, TriggerIndexer>()
            .AddSingleton<IWorkflowInstanceFactory, WorkflowInstanceFactory>()
            .AddSingleton<IWorkflowHostFactory, WorkflowHostFactory>()
            .AddSingleton<IBackgroundActivityInvoker, DefaultBackgroundActivityInvoker>()
            .AddSingleton(WorkflowRuntime)
            .AddSingleton(WorkflowDispatcher)
            .AddSingleton(BookmarkStore)
            .AddSingleton(TriggerStore)
            .AddSingleton(WorkflowExecutionLogStore)
            .AddSingleton(ActivityExecutionLogStore)
            .AddSingleton(WorkflowInboxStore)
            .AddSingleton(RunTaskDispatcher)
            .AddSingleton(BackgroundActivityInvoker)
            .AddSingleton<IBookmarkManager, DefaultBookmarkManager>()
            .AddSingleton<IActivityExecutionManager, DefaultActivityExecutionManager>()
            .AddSingleton<IActivityExecutionStatsService, ActivityExecutionStatsService>()
            .AddSingleton<IActivityExecutionMapper, DefaultActivityExecutionMapper>()
            .AddSingleton<IWorkflowDefinitionStorePopulator, DefaultWorkflowDefinitionStorePopulator>()
            .AddSingleton<IRegistriesPopulator, DefaultRegistriesPopulator>()
            .AddSingleton<ITaskReporter, TaskReporter>()
            .AddSingleton<SynchronousTaskDispatcher>()
            .AddSingleton<BackgroundTaskDispatcher>()
            .AddSingleton<IEventPublisher, EventPublisher>()
            .AddSingleton<IWorkflowInbox, DefaultWorkflowInbox>()
            
            // Lazy services.
            .AddSingleton<Func<IEnumerable<IWorkflowProvider>>>(sp => sp.GetServices<IWorkflowProvider>)
            .AddSingleton<Func<IEnumerable<IWorkflowMaterializer>>>(sp => sp.GetServices<IWorkflowMaterializer>)

            // Noop stores.
            .AddSingleton<MemoryWorkflowExecutionLogStore>()
            .AddSingleton<MemoryActivityExecutionStore>()
            
            // Memory stores.
            .AddMemoryStore<StoredBookmark, MemoryBookmarkStore>()
            .AddMemoryStore<StoredTrigger, MemoryTriggerStore>()
            .AddMemoryStore<WorkflowExecutionLogRecord, MemoryWorkflowExecutionLogStore>()
            .AddMemoryStore<ActivityExecutionRecord, MemoryActivityExecutionStore>()
            .AddMemoryStore<WorkflowInboxMessage, MemoryWorkflowInboxMessageStore>()

            // Distributed locking.
            .AddSingleton(DistributedLockProvider)

            // Workflow definition providers.
            .AddWorkflowDefinitionProvider<ClrWorkflowProvider>()

            // Domain handlers.
            .AddCommandHandler<DispatchWorkflowRequestHandler>()
            .AddNotificationHandler<ResumeDispatchWorkflowActivity>()
            .AddNotificationHandler<IndexWorkflowTriggersHandler>()
            .AddNotificationHandler<ScheduleBackgroundActivities>()
            .AddNotificationHandler<CancelBackgroundActivities>()
            .AddNotificationHandler<DeleteBookmarks>()
            .AddNotificationHandler<DeleteTriggers>()
            .AddNotificationHandler<DeleteWorkflowInstances>()
            .AddNotificationHandler<DeleteActivityExecutionLogRecords>()
            .AddNotificationHandler<ReadWorkflowInboxMessage>()
            .AddNotificationHandler<DeliverWorkflowMessagesFromInbox>()
            .AddNotificationHandler<DeleteWorkflowExecutionLogRecords>()

            // Workflow activation strategies.
            .AddSingleton<IWorkflowActivationStrategy, SingletonStrategy>()
            .AddSingleton<IWorkflowActivationStrategy, CorrelatedSingletonStrategy>()
            .AddSingleton<IWorkflowActivationStrategy, CorrelationStrategy>()
            ;
    }
}