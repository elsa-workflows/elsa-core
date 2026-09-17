using System.Collections.Concurrent;
using Elsa.Common.Models;
using Elsa.Extensions;
using Elsa.Mediator.Contracts;
using Elsa.Mediator.HostedServices;
using Elsa.Persistence.EFCore.Extensions;
using Elsa.Persistence.EFCore.Modules.Management;
using Elsa.Testing.Shared;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Management.Notifications;
using Elsa.Workflows.Models;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.ActivationValidators;
using Elsa.Workflows.Runtime.Requests;
using Medallion.Threading;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit.Abstractions;

namespace Elsa.Workflows.IntegrationTests.Scenarios.CorrelatedActivation;

public class Tests
{
    private readonly ITestOutputHelper _testOutputHelper;

    public Tests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
    }

    [Fact(DisplayName = "Concurrent start with the same CorrelationId and CorrelatedSingleton creates one Running instance")]
    public async Task ConcurrentStart_WithCorrelatedSingleton_CreatesOneRunningInstance()
    {
        var services = CreateServices();
        await services.PopulateRegistriesAsync();
        var handle = WorkflowDefinitionHandle.ByDefinitionId(nameof(CorrelatedSingletonConversationWorkflow), VersionOptions.Published);
        await using var scope1 = services.CreateAsyncScope();
        await using var scope2 = services.CreateAsyncScope();
        var starter1 = scope1.ServiceProvider.GetRequiredService<IWorkflowStarter>();
        var starter2 = scope2.ServiceProvider.GetRequiredService<IWorkflowStarter>();

        var results = await Task.WhenAll(
            starter1.StartWorkflowAsync(new StartWorkflowRequest
            {
                WorkflowDefinitionHandle = handle,
                CorrelationId = "conversation-1"
            }),
            starter2.StartWorkflowAsync(new StartWorkflowRequest
            {
                WorkflowDefinitionHandle = handle,
                CorrelationId = "conversation-1"
            }));

        Assert.Equal(1, results.Count(result => !result.CannotStart));
        Assert.Equal(1, results.Count(result => result.CannotStart));
        Assert.Single(await FindRunningAsync(services, nameof(CorrelatedSingletonConversationWorkflow), "conversation-1"));
        Assert.Single(await FindByCorrelationAsync(services, nameof(CorrelatedSingletonConversationWorkflow), "conversation-1"));
    }

    [Fact(DisplayName = "Concurrent dispatch with the same CorrelationId and CorrelatedSingleton creates one Running instance")]
    public async Task ConcurrentDispatch_WithCorrelatedSingleton_CreatesOneRunningInstance()
    {
        var savedSignal = new WorkflowInstanceSavedSignal();
        var services = CreateServices(serviceCollection =>
        {
            serviceCollection.AddSingleton(savedSignal);
            serviceCollection.AddNotificationHandler<WorkflowInstanceSavedSignal, WorkflowInstanceSaved>(sp => sp.GetRequiredService<WorkflowInstanceSavedSignal>());
        });
        await services.PopulateRegistriesAsync();
        var graph = await FindGraphAsync(services, nameof(CorrelatedSingletonConversationWorkflow));
        var dispatcher = services.GetRequiredService<IWorkflowDispatcher>();
        var commandProcessor = services.GetServices<IHostedService>().OfType<BackgroundCommandSenderHostedService>().Single();
        await commandProcessor.StartAsync(CancellationToken.None);

        try
        {
            await Task.WhenAll(
                dispatcher.DispatchAsync(new DispatchWorkflowDefinitionRequest(graph.Workflow.Identity.Id)
                {
                    CorrelationId = "conversation-1"
                }, null),
                dispatcher.DispatchAsync(new DispatchWorkflowDefinitionRequest(graph.Workflow.Identity.Id)
                {
                    CorrelationId = "conversation-1"
                }, null));

            await savedSignal.Saved.Task.WaitAsync(TimeSpan.FromSeconds(10));

            var instances = await FindRunningAsync(services, nameof(CorrelatedSingletonConversationWorkflow), "conversation-1");
            Assert.Single(instances);
            Assert.Single(await FindByCorrelationAsync(services, nameof(CorrelatedSingletonConversationWorkflow), "conversation-1"));
        }
        finally
        {
            await commandProcessor.StopAsync(CancellationToken.None);
        }
    }

    [Theory(DisplayName = "Concurrent dispatch without a restrictive activation strategy allows both instances")]
    [InlineData(nameof(GroupedConversationWorkflow))]
    [InlineData(nameof(AllowAlwaysConversationWorkflow))]
    public async Task ConcurrentDispatch_WithoutRestriction_AllowsMultipleRunningInstances(string definitionId)
    {
        var savedSignal = new WorkflowInstanceSavedSignal(expectedCount: 2);
        var services = CreateServices(serviceCollection =>
        {
            serviceCollection.AddSingleton(savedSignal);
            serviceCollection.AddNotificationHandler<WorkflowInstanceSavedSignal, WorkflowInstanceSaved>(sp => sp.GetRequiredService<WorkflowInstanceSavedSignal>());
        });
        await services.PopulateRegistriesAsync();
        var graph = await FindGraphAsync(services, definitionId);
        var dispatcher = services.GetRequiredService<IWorkflowDispatcher>();
        var commandProcessor = services.GetServices<IHostedService>().OfType<BackgroundCommandSenderHostedService>().Single();
        await commandProcessor.StartAsync(CancellationToken.None);

        try
        {
            await Task.WhenAll(
                dispatcher.DispatchAsync(new DispatchWorkflowDefinitionRequest(graph.Workflow.Identity.Id) { CorrelationId = "conversation-1" }, null),
                dispatcher.DispatchAsync(new DispatchWorkflowDefinitionRequest(graph.Workflow.Identity.Id) { CorrelationId = "conversation-1" }, null));

            await savedSignal.Saved.Task.WaitAsync(TimeSpan.FromSeconds(10));

            Assert.Equal(2, (await FindRunningAsync(services, definitionId, "conversation-1")).Count);
            Assert.Equal(2, (await FindByCorrelationAsync(services, definitionId, "conversation-1")).Count);
        }
        finally
        {
            await commandProcessor.StopAsync(CancellationToken.None);
        }
    }

    [Fact(DisplayName = "Concurrent dispatch on two runtime nodes sharing SQLite and a distributed lock persists one Running instance")]
    public async Task ConcurrentDispatch_AcrossRuntimeNodes_CreatesOneRunningInstance()
    {
        var databasePath = Path.Join(Path.GetTempPath(), $"elsa-activation-multinode-{Guid.NewGuid():N}.db");
        var connectionString = $"Data Source={databasePath};Default Timeout=30;Pooling=False";
        var lockProvider = new BarrierDistributedLockProvider();

        try
        {
            await using var nodeA = CreateSqliteServices(connectionString, lockProvider);
            await using var nodeB = CreateSqliteServices(connectionString, lockProvider);
            Assert.NotSame(nodeA, nodeB);
            Assert.Same(lockProvider, nodeA.GetRequiredService<IDistributedLockProvider>());
            Assert.Same(lockProvider, nodeB.GetRequiredService<IDistributedLockProvider>());

            await using (var dbContext = await nodeA.GetRequiredService<IDbContextFactory<ManagementElsaDbContext>>().CreateDbContextAsync())
                await dbContext.Database.EnsureCreatedAsync();

            await Task.WhenAll(nodeA.PopulateRegistriesAsync(), nodeB.PopulateRegistriesAsync());
            var graphA = await FindGraphAsync(nodeA, nameof(CorrelatedSingletonConversationWorkflow));
            var graphB = await FindGraphAsync(nodeB, nameof(CorrelatedSingletonConversationWorkflow));
            var dispatcherA = nodeA.GetRequiredService<IWorkflowDispatcher>();
            var dispatcherB = nodeB.GetRequiredService<IWorkflowDispatcher>();
            var commandProcessorA = nodeA.GetServices<IHostedService>().OfType<BackgroundCommandSenderHostedService>().Single();
            var commandProcessorB = nodeB.GetServices<IHostedService>().OfType<BackgroundCommandSenderHostedService>().Single();
            await Task.WhenAll(
                commandProcessorA.StartAsync(CancellationToken.None),
                commandProcessorB.StartAsync(CancellationToken.None));

            try
            {
                await Task.WhenAll(
                    dispatcherA.DispatchAsync(new DispatchWorkflowDefinitionRequest(graphA.Workflow.Identity.Id) { CorrelationId = "shared-runtime-correlation" }, null),
                    dispatcherB.DispatchAsync(new DispatchWorkflowDefinitionRequest(graphB.Workflow.Identity.Id) { CorrelationId = "shared-runtime-correlation" }, null));

                await lockProvider.TwoActivationLockAttempts.Task.WaitAsync(TimeSpan.FromSeconds(10));
                // The SQLite connection timeout is 30 seconds; allow a transient provider lock wait
                // to complete so the assertion timeout does not fire before the persistence timeout.
                await lockProvider.TwoActivationLockReleases.Task.WaitAsync(TimeSpan.FromSeconds(40));

                Assert.Equal(2, lockProvider.ActivationLockAttempts);
                Assert.Equal(2, lockProvider.ActivationLockReleases);
                Assert.Single(lockProvider.ActivationLockNames.Distinct());
                Assert.Single(await FindRunningAsync(nodeA, nameof(CorrelatedSingletonConversationWorkflow), "shared-runtime-correlation"));
                Assert.Single(await FindRunningAsync(nodeB, nameof(CorrelatedSingletonConversationWorkflow), "shared-runtime-correlation"));
                Assert.Single(await FindByCorrelationAsync(nodeA, nameof(CorrelatedSingletonConversationWorkflow), "shared-runtime-correlation"));
                Assert.Single(await FindByCorrelationAsync(nodeB, nameof(CorrelatedSingletonConversationWorkflow), "shared-runtime-correlation"));
            }
            finally
            {
                lockProvider.ReleaseActivationBarrier();
                await Task.WhenAll(
                    commandProcessorA.StopAsync(CancellationToken.None),
                    commandProcessorB.StopAsync(CancellationToken.None));
            }
        }
        finally
        {
            lockProvider.ReleaseActivationBarrier();
            if (File.Exists(databasePath))
                File.Delete(databasePath);
        }
    }

    [Fact(DisplayName = "Waiting dispatch resumes its parent when child activation is denied")]
    public async Task DispatchWorkflow_WaitForCompletion_ResumesParentWhenActivationIsDenied()
    {
        var finishedSignal = new WorkflowInstanceFinishedSavedSignal();
        var services = CreateServices(serviceCollection =>
        {
            serviceCollection.AddSingleton(finishedSignal);
            serviceCollection.AddNotificationHandler<WorkflowInstanceFinishedSavedSignal, WorkflowInstanceSaved>(sp => sp.GetRequiredService<WorkflowInstanceFinishedSavedSignal>());
        });
        await services.PopulateRegistriesAsync();
        var starter = services.GetRequiredService<IWorkflowStarter>();
        var child = await starter.StartWorkflowAsync(new StartWorkflowRequest
        {
            WorkflowDefinitionHandle = WorkflowDefinitionHandle.ByDefinitionId(nameof(CorrelatedSingletonConversationWorkflow), VersionOptions.Published),
            CorrelationId = "denied-dispatch-correlation"
        });
        Assert.False(child.CannotStart);

        var commandProcessor = services.GetServices<IHostedService>().OfType<BackgroundCommandSenderHostedService>().Single();
        var parent = await starter.StartWorkflowAsync(new StartWorkflowRequest
        {
            WorkflowDefinitionHandle = WorkflowDefinitionHandle.ByDefinitionId(nameof(WaitForDeniedDispatchConversationWorkflow), VersionOptions.Published)
        });

        Assert.False(parent.CannotStart);
        var parentInstanceId = Assert.IsType<string>(parent.WorkflowInstanceId);
        await commandProcessor.StartAsync(CancellationToken.None);
        try
        {
            await finishedSignal.WaitForFinishAsync(parentInstanceId);

            var persistedParent = Assert.Single(
                await FindByDefinitionAsync(services, nameof(WaitForDeniedDispatchConversationWorkflow)),
                instance => instance.Id == parentInstanceId);
            Assert.Equal(WorkflowStatus.Finished, persistedParent.Status);
            Assert.Empty(persistedParent.WorkflowState.Bookmarks);

            var persistedChildren = await FindByCorrelationAsync(services, nameof(CorrelatedSingletonConversationWorkflow), "denied-dispatch-correlation");
            Assert.Equal(child.WorkflowInstanceId, Assert.Single(persistedChildren).Id);
        }
        finally
        {
            await commandProcessor.StopAsync(CancellationToken.None);
        }
    }

    [Fact(DisplayName = "Without an activation strategy, the same CorrelationId may have many Running instances")]
    public async Task ConcurrentStart_WithoutStrategy_AllowsMultipleRunningInstances()
    {
        var services = CreateServices();
        await services.PopulateRegistriesAsync();
        var starter = services.GetRequiredService<IWorkflowStarter>();
        var handle = WorkflowDefinitionHandle.ByDefinitionId(nameof(GroupedConversationWorkflow), VersionOptions.Published);

        var results = await Task.WhenAll(
            starter.StartWorkflowAsync(new StartWorkflowRequest
            {
                WorkflowDefinitionHandle = handle,
                CorrelationId = "conversation-1"
            }),
            starter.StartWorkflowAsync(new StartWorkflowRequest
            {
                WorkflowDefinitionHandle = handle,
                CorrelationId = "conversation-1"
            }));

        Assert.All(results, result => Assert.False(result.CannotStart));
        Assert.Equal(2, (await FindRunningAsync(services, nameof(GroupedConversationWorkflow), "conversation-1")).Count);
        Assert.Equal(2, (await FindByCorrelationAsync(services, nameof(GroupedConversationWorkflow), "conversation-1")).Count);
    }

    [Theory(DisplayName = "Correlation strategies require a non-blank CorrelationId and persist no instance")]
    [InlineData(nameof(CorrelatedSingletonConversationWorkflow))]
    [InlineData(nameof(GlobalCorrelationConversationWorkflow))]
    public async Task Start_WithBlankCorrelationId_ThrowsAndDoesNotPersistInstance(string definitionId)
    {
        var services = CreateServices();
        await services.PopulateRegistriesAsync();
        var starter = services.GetRequiredService<IWorkflowStarter>();
        var handle = WorkflowDefinitionHandle.ByDefinitionId(definitionId, VersionOptions.Published);

        var exception = await Assert.ThrowsAsync<ArgumentException>(() => starter.StartWorkflowAsync(new StartWorkflowRequest
        {
            WorkflowDefinitionHandle = handle
        }));

        Assert.Contains("non-blank correlation ID", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(await FindByDefinitionAsync(services, definitionId));
    }

    [Fact(DisplayName = "Without an activation strategy a blank CorrelationId is allowed")]
    public async Task Start_WithoutStrategy_AllowsBlankCorrelationId()
    {
        var services = CreateServices();
        await services.PopulateRegistriesAsync();
        var starter = services.GetRequiredService<IWorkflowStarter>();
        var handle = WorkflowDefinitionHandle.ByDefinitionId(nameof(GroupedConversationWorkflow), VersionOptions.Published);

        var result = await starter.StartWorkflowAsync(new StartWorkflowRequest { WorkflowDefinitionHandle = handle });

        Assert.False(result.CannotStart);
        Assert.Single(await FindByDefinitionAsync(services, nameof(GroupedConversationWorkflow)));
    }

    [Fact(DisplayName = "CorrelatedSingleton scopes uniqueness to DefinitionId + CorrelationId")]
    public async Task CorrelatedSingleton_AllowsSameCorrelationIdOnDifferentDefinitions()
    {
        var services = CreateServices();
        await services.PopulateRegistriesAsync();
        var starter = services.GetRequiredService<IWorkflowStarter>();

        var first = await starter.StartWorkflowAsync(new StartWorkflowRequest
        {
            WorkflowDefinitionHandle = WorkflowDefinitionHandle.ByDefinitionId(nameof(CorrelatedSingletonConversationWorkflow), VersionOptions.Published),
            CorrelationId = "shared-conversation"
        });
        var second = await starter.StartWorkflowAsync(new StartWorkflowRequest
        {
            WorkflowDefinitionHandle = WorkflowDefinitionHandle.ByDefinitionId(nameof(OtherCorrelatedSingletonConversationWorkflow), VersionOptions.Published),
            CorrelationId = "shared-conversation"
        });

        Assert.False(first.CannotStart);
        Assert.False(second.CannotStart);
        Assert.Single(await FindRunningAsync(services, nameof(CorrelatedSingletonConversationWorkflow), "shared-conversation"));
        Assert.Single(await FindRunningAsync(services, nameof(OtherCorrelatedSingletonConversationWorkflow), "shared-conversation"));
    }

    [Fact(DisplayName = "Correlation strategy refuses a second Running instance for the same CorrelationId across definitions")]
    public async Task CorrelationStrategy_RefusesSecondDefinitionWithSameCorrelationId()
    {
        var services = CreateServices();
        await services.PopulateRegistriesAsync();
        var starter = services.GetRequiredService<IWorkflowStarter>();

        var first = await starter.StartWorkflowAsync(new StartWorkflowRequest
        {
            WorkflowDefinitionHandle = WorkflowDefinitionHandle.ByDefinitionId(nameof(GlobalCorrelationConversationWorkflow), VersionOptions.Published),
            CorrelationId = "shared-conversation"
        });
        var second = await starter.StartWorkflowAsync(new StartWorkflowRequest
        {
            WorkflowDefinitionHandle = WorkflowDefinitionHandle.ByDefinitionId(nameof(OtherGlobalCorrelationConversationWorkflow), VersionOptions.Published),
            CorrelationId = "shared-conversation"
        });

        Assert.False(first.CannotStart);
        Assert.True(second.CannotStart);
        Assert.Single(await FindRunningAsync(services, nameof(GlobalCorrelationConversationWorkflow), "shared-conversation"));
        Assert.Empty(await FindRunningAsync(services, nameof(OtherGlobalCorrelationConversationWorkflow), "shared-conversation"));
        Assert.Single(await FindByCorrelationAsync(services, nameof(GlobalCorrelationConversationWorkflow), "shared-conversation"));
        Assert.Empty(await FindByCorrelationAsync(services, nameof(OtherGlobalCorrelationConversationWorkflow), "shared-conversation"));
    }

    [Fact(DisplayName = "Singleton scopes uniqueness to tenant and definition, independent of correlation ID")]
    public async Task Singleton_RefusesSecondRunningInstanceWithDifferentCorrelationId()
    {
        var services = CreateServices();
        await services.PopulateRegistriesAsync();
        var starter = services.GetRequiredService<IWorkflowStarter>();
        var handle = WorkflowDefinitionHandle.ByDefinitionId(nameof(SingletonConversationWorkflow), VersionOptions.Published);

        var first = await starter.StartWorkflowAsync(new StartWorkflowRequest { WorkflowDefinitionHandle = handle, CorrelationId = "conversation-1" });
        var second = await starter.StartWorkflowAsync(new StartWorkflowRequest { WorkflowDefinitionHandle = handle, CorrelationId = "conversation-2" });

        Assert.False(first.CannotStart);
        Assert.True(second.CannotStart);
        Assert.Single(await FindByDefinitionAsync(services, nameof(SingletonConversationWorkflow)));
    }

    [Fact(DisplayName = "A terminal instance no longer occupies its activation scope")]
    public async Task CorrelatedSingleton_AllowsNewInstanceAfterPriorInstanceFinishes()
    {
        var services = CreateServices();
        await services.PopulateRegistriesAsync();
        var starter = services.GetRequiredService<IWorkflowStarter>();
        var handle = WorkflowDefinitionHandle.ByDefinitionId(nameof(CorrelatedSingletonConversationWorkflow), VersionOptions.Published);
        var first = await starter.StartWorkflowAsync(new StartWorkflowRequest { WorkflowDefinitionHandle = handle, CorrelationId = "conversation-terminal" });
        Assert.False(first.CannotStart);

        var firstInstance = Assert.Single(await FindByCorrelationAsync(services, nameof(CorrelatedSingletonConversationWorkflow), "conversation-terminal"));
        firstInstance.Status = WorkflowStatus.Finished;
        firstInstance.SubStatus = WorkflowSubStatus.Finished;
        firstInstance.WorkflowState.Status = WorkflowStatus.Finished;
        firstInstance.WorkflowState.SubStatus = WorkflowSubStatus.Finished;
        await services.GetRequiredService<IWorkflowInstanceStore>().SaveAsync(firstInstance);

        var second = await starter.StartWorkflowAsync(new StartWorkflowRequest { WorkflowDefinitionHandle = handle, CorrelationId = "conversation-terminal" });

        Assert.False(second.CannotStart);
        Assert.Single(await FindRunningAsync(services, nameof(CorrelatedSingletonConversationWorkflow), "conversation-terminal"));
        Assert.Equal(2, (await FindByCorrelationAsync(services, nameof(CorrelatedSingletonConversationWorkflow), "conversation-terminal")).Count);
    }

    private IServiceProvider CreateServices(Action<IServiceCollection>? configureServices = null)
    {
        var builder = new TestApplicationBuilder(_testOutputHelper);
        if (configureServices != null)
            builder.ConfigureServices(configureServices);

        return builder
            .AddWorkflow<CorrelatedSingletonConversationWorkflow>()
            .AddWorkflow<WaitForDeniedDispatchConversationWorkflow>()
            .AddWorkflow<SingletonConversationWorkflow>()
            .AddWorkflow<GroupedConversationWorkflow>()
            .AddWorkflow<AllowAlwaysConversationWorkflow>()
            .AddWorkflow<OtherCorrelatedSingletonConversationWorkflow>()
            .AddWorkflow<GlobalCorrelationConversationWorkflow>()
            .AddWorkflow<OtherGlobalCorrelationConversationWorkflow>()
            .Build();
    }

    private ServiceProvider CreateSqliteServices(string connectionString, IDistributedLockProvider lockProvider)
    {
        var builder = new TestApplicationBuilder(_testOutputHelper)
            .ConfigureElsa(elsa => elsa
                .UseWorkflowManagement(management => management.UseWorkflowInstances(instances =>
                    instances.UseEntityFrameworkCore(persistence => persistence.UseSqlite(connectionString))))
                .UseWorkflowRuntime(runtime => runtime.DistributedLockProvider = _ => lockProvider));

        return (ServiceProvider)builder
            .AddWorkflow<CorrelatedSingletonConversationWorkflow>()
            .Build();
    }

    private static async Task<WorkflowGraph> FindGraphAsync(IServiceProvider services, string definitionId)
    {
        var graph = await services.GetRequiredService<IWorkflowDefinitionService>()
            .FindWorkflowGraphAsync(definitionId, VersionOptions.Published);
        Assert.NotNull(graph);
        return graph;
    }

    private static async Task<List<WorkflowInstance>> FindRunningAsync(IServiceProvider services, string definitionId, string? correlationId)
    {
        var filter = new WorkflowInstanceFilter
        {
            DefinitionId = definitionId,
            WorkflowStatus = WorkflowStatus.Running
        };

        if (correlationId != null)
            filter.CorrelationId = correlationId;

        return (await services.GetRequiredService<IWorkflowInstanceStore>().FindManyAsync(filter)).ToList();
    }

    private static async Task<List<WorkflowInstance>> FindByCorrelationAsync(IServiceProvider services, string definitionId, string correlationId) =>
        (await services.GetRequiredService<IWorkflowInstanceStore>().FindManyAsync(new WorkflowInstanceFilter
        {
            DefinitionId = definitionId,
            CorrelationId = correlationId
        })).ToList();

    private static async Task<List<WorkflowInstance>> FindByDefinitionAsync(IServiceProvider services, string definitionId) =>
        (await services.GetRequiredService<IWorkflowInstanceStore>().FindManyAsync(new WorkflowInstanceFilter
        {
            DefinitionId = definitionId
        })).ToList();

    private sealed class WorkflowInstanceSavedSignal(int expectedCount = 1) : INotificationHandler<WorkflowInstanceSaved>
    {
        private int _count;

        public TaskCompletionSource Saved { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task HandleAsync(WorkflowInstanceSaved notification, CancellationToken cancellationToken)
        {
            if (Interlocked.Increment(ref _count) >= expectedCount)
                Saved.TrySetResult();
            return Task.CompletedTask;
        }
    }

    private sealed class WorkflowInstanceFinishedSavedSignal : INotificationHandler<WorkflowInstanceSaved>
    {
        private readonly ConcurrentDictionary<string, TaskCompletionSource> _pending = new();
        private readonly ConcurrentDictionary<string, byte> _finished = new();

        public Task WaitForFinishAsync(string workflowInstanceId)
        {
            if (_finished.ContainsKey(workflowInstanceId))
                return Task.CompletedTask;

            var completion = _pending.GetOrAdd(workflowInstanceId, _ => new(TaskCreationOptions.RunContinuationsAsynchronously));
            if (_finished.ContainsKey(workflowInstanceId))
                completion.TrySetResult();
            return completion.Task.WaitAsync(TimeSpan.FromSeconds(10));
        }

        public Task HandleAsync(WorkflowInstanceSaved notification, CancellationToken cancellationToken)
        {
            if (notification.WorkflowInstance.Status != WorkflowStatus.Finished)
                return Task.CompletedTask;

            var workflowInstanceId = notification.WorkflowInstance.Id;
            _finished.TryAdd(workflowInstanceId, 0);
            if (_pending.TryGetValue(workflowInstanceId, out var completion))
                completion.TrySetResult();
            return Task.CompletedTask;
        }
    }

    private sealed class BarrierDistributedLockProvider : IDistributedLockProvider
    {
        private const string ActivationLockPrefix = "workflow-activation:v1:";
        private readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new();
        private readonly ConcurrentQueue<string> _activationLockNames = new();
        private int _activationLockAttempts;
        private int _activationLockReleases;

        public TaskCompletionSource TwoActivationLockAttempts { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource TwoActivationLockReleases { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public int ActivationLockAttempts => Volatile.Read(ref _activationLockAttempts);
        public int ActivationLockReleases => Volatile.Read(ref _activationLockReleases);
        public IReadOnlyCollection<string> ActivationLockNames => _activationLockNames.ToArray();

        public IDistributedLock CreateLock(string name) => new BarrierDistributedLock(this, name, _locks.GetOrAdd(name, _ => new SemaphoreSlim(1, 1)));

        public void ReleaseActivationBarrier() => TwoActivationLockAttempts.TrySetResult();

        private async ValueTask<IDistributedSynchronizationHandle> AcquireAsync(string name, SemaphoreSlim semaphore, TimeSpan? timeout, CancellationToken cancellationToken)
        {
            var isActivationLock = name.StartsWith(ActivationLockPrefix, StringComparison.Ordinal);
            if (isActivationLock)
            {
                _activationLockNames.Enqueue(name);
                if (Interlocked.Increment(ref _activationLockAttempts) == 2)
                    TwoActivationLockAttempts.TrySetResult();
                await TwoActivationLockAttempts.Task.WaitAsync(cancellationToken);
            }

            if (!await semaphore.WaitAsync(timeout ?? Timeout.InfiniteTimeSpan, cancellationToken))
                throw new TimeoutException($"Could not acquire lock '{name}'.");

            return new Handle(this, name, semaphore, isActivationLock);
        }

        private void Release(string name, SemaphoreSlim semaphore, bool isActivationLock)
        {
            semaphore.Release();
            if (isActivationLock && Interlocked.Increment(ref _activationLockReleases) == 2)
                TwoActivationLockReleases.TrySetResult();
        }

        private sealed class BarrierDistributedLock(BarrierDistributedLockProvider provider, string name, SemaphoreSlim semaphore) : IDistributedLock
        {
            public string Name => name;

            public IDistributedSynchronizationHandle? TryAcquire(TimeSpan timeout = default, CancellationToken cancellationToken = default) =>
                semaphore.Wait(timeout, cancellationToken) ? new Handle(provider, name, semaphore, name.StartsWith(ActivationLockPrefix, StringComparison.Ordinal)) : null;

            public IDistributedSynchronizationHandle Acquire(TimeSpan? timeout = null, CancellationToken cancellationToken = default) =>
                provider.AcquireAsync(name, semaphore, timeout, cancellationToken).AsTask().GetAwaiter().GetResult();

            public ValueTask<IDistributedSynchronizationHandle?> TryAcquireAsync(TimeSpan timeout = default, CancellationToken cancellationToken = default) =>
                TryAcquireCoreAsync(timeout, cancellationToken);

            private async ValueTask<IDistributedSynchronizationHandle?> TryAcquireCoreAsync(TimeSpan timeout, CancellationToken cancellationToken) =>
                await semaphore.WaitAsync(timeout, cancellationToken)
                    ? new Handle(provider, name, semaphore, name.StartsWith(ActivationLockPrefix, StringComparison.Ordinal))
                    : null;

            public ValueTask<IDistributedSynchronizationHandle> AcquireAsync(TimeSpan? timeout = null, CancellationToken cancellationToken = default) =>
                provider.AcquireAsync(name, semaphore, timeout, cancellationToken);
        }

        private sealed class Handle(BarrierDistributedLockProvider provider, string name, SemaphoreSlim semaphore, bool isActivationLock) : IDistributedSynchronizationHandle
        {
            private int _disposed;

            public CancellationToken HandleLostToken => CancellationToken.None;

            public void Dispose()
            {
                if (Interlocked.Exchange(ref _disposed, 1) == 0)
                    provider.Release(name, semaphore, isActivationLock);
            }

            public ValueTask DisposeAsync()
            {
                Dispose();
                return ValueTask.CompletedTask;
            }
        }
    }
}
