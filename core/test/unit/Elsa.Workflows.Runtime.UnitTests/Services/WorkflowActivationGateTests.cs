using System.Collections.Concurrent;
using Elsa.Common.DistributedHosting;
using Elsa.Common.Multitenancy;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Models;
using Elsa.Workflows.Runtime.ActivationValidators;
using Medallion.Threading;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace Elsa.Workflows.Runtime.UnitTests.Services;

public class WorkflowActivationGateTests
{
    [Fact]
    public async Task AllowsWithoutLock_WhenWorkflowHasNoActivationStrategy()
    {
        var evaluator = Substitute.For<IWorkflowActivationStrategyEvaluator>();
        evaluator.CanStartWorkflowAsync(Arg.Any<WorkflowActivationStrategyEvaluationContext>()).Returns(true);
        var lockProvider = new InMemoryDistributedLockProvider();
        var gate = CreateGate(evaluator, lockProvider);
        var workflow = new Workflow();

        await using var lease = await gate.EvaluateAsync(workflow, "order-1");

        Assert.True(lease.CanStart);
        Assert.Empty(lockProvider.LockNames);
        await evaluator.Received(1).CanStartWorkflowAsync(Arg.Is<WorkflowActivationStrategyEvaluationContext>(
            context => context.Workflow == workflow && context.CorrelationId == "order-1"));
    }

    [Fact]
    public async Task ReturnsDenied_WhenEvaluatorDisallows()
    {
        var evaluator = Substitute.For<IWorkflowActivationStrategyEvaluator>();
        evaluator.CanStartWorkflowAsync(Arg.Any<WorkflowActivationStrategyEvaluationContext>()).Returns(false);
        var lockProvider = new InMemoryDistributedLockProvider();
        var gate = CreateGate(evaluator, lockProvider);

        var lease = await gate.EvaluateAsync(CreateWorkflow(typeof(CorrelatedSingletonStrategy)), "order-1");

        Assert.False(lease.CanStart);
        Assert.Equal(0, lockProvider.HeldLocks);
    }

    [Fact]
    public async Task ReleasesActivationLock_WhenEvaluatorThrows()
    {
        var evaluator = Substitute.For<IWorkflowActivationStrategyEvaluator>();
        evaluator.CanStartWorkflowAsync(Arg.Any<WorkflowActivationStrategyEvaluationContext>())
            .Returns(_ => Task.FromException<bool>(new InvalidOperationException("evaluation failed")));
        var lockProvider = new InMemoryDistributedLockProvider();
        var gate = CreateGate(evaluator, lockProvider);

        await Assert.ThrowsAsync<InvalidOperationException>(() => gate.EvaluateAsync(CreateWorkflow(typeof(SingletonStrategy)), null));

        Assert.Equal(0, lockProvider.HeldLocks);
    }

    [Fact]
    public async Task ReleasesActivationLock_WhenEvaluatorIsCancelled()
    {
        using var cancellationSource = new CancellationTokenSource();
        var evaluator = Substitute.For<IWorkflowActivationStrategyEvaluator>();
        evaluator.CanStartWorkflowAsync(Arg.Any<WorkflowActivationStrategyEvaluationContext>())
            .Returns(_ =>
            {
                cancellationSource.Cancel();
                return Task.FromCanceled<bool>(cancellationSource.Token);
            });
        var lockProvider = new InMemoryDistributedLockProvider();
        var gate = CreateGate(evaluator, lockProvider);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => gate.EvaluateAsync(CreateWorkflow(typeof(SingletonStrategy)), null, cancellationSource.Token));

        Assert.Equal(0, lockProvider.HeldLocks);
    }

    [Fact]
    public async Task CancelsEvaluatorAndRejectsActivation_WhenDistributedLeaseIsLostDuringEvaluation()
    {
        using var handleLostSource = new CancellationTokenSource();
        var evaluatorStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var completeEvaluation = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        CancellationToken strategyToken = default;
        var evaluator = Substitute.For<IWorkflowActivationStrategyEvaluator>();
        evaluator.CanStartWorkflowAsync(Arg.Any<WorkflowActivationStrategyEvaluationContext>())
            .Returns(call =>
            {
                strategyToken = call.Arg<WorkflowActivationStrategyEvaluationContext>().CancellationToken;
                evaluatorStarted.SetResult();
                return completeEvaluation.Task;
            });
        var lockProvider = new HandleLostTokenDistributedLockProvider(handleLostSource.Token);
        var gate = CreateGate(evaluator, lockProvider);

        var evaluation = gate.EvaluateAsync(CreateWorkflow(typeof(SingletonStrategy)), null);
        await evaluatorStarted.Task;
        handleLostSource.Cancel();
        var strategyObservedCancellation = strategyToken.IsCancellationRequested;
        completeEvaluation.SetResult(true);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => evaluation);
        Assert.True(strategyObservedCancellation);
        Assert.Equal(1, lockProvider.HandleDisposeCount);
    }

    [Fact]
    public async Task CancelsLeaseToken_WhenDistributedLeaseIsLostAfterEvaluation()
    {
        using var handleLostSource = new CancellationTokenSource();
        var evaluator = Substitute.For<IWorkflowActivationStrategyEvaluator>();
        evaluator.CanStartWorkflowAsync(Arg.Any<WorkflowActivationStrategyEvaluationContext>()).Returns(true);
        var lockProvider = new HandleLostTokenDistributedLockProvider(handleLostSource.Token);
        var gate = CreateGate(evaluator, lockProvider);

        await using var lease = await gate.EvaluateAsync(CreateWorkflow(typeof(SingletonStrategy)), null);
        Assert.True(lease.CanStart);
        var cancellationToken = lease.GetEffectiveCancellationToken(CancellationToken.None);
        Assert.False(cancellationToken.IsCancellationRequested);

        handleLostSource.Cancel();

        Assert.True(cancellationToken.IsCancellationRequested);
    }

    [Fact]
    public async Task SerializesCheckAndCreate_WhenTwoCallersRaceOnSameCorrelation()
    {
        var started = 0;
        var evaluator = Substitute.For<IWorkflowActivationStrategyEvaluator>();
        evaluator.CanStartWorkflowAsync(Arg.Any<WorkflowActivationStrategyEvaluationContext>())
            .Returns(_ => Interlocked.CompareExchange(ref started, 1, 0) == 0);
        var lockProvider = new InMemoryDistributedLockProvider();
        var gate = CreateGate(evaluator, lockProvider);
        var workflow = CreateWorkflow(typeof(CorrelatedSingletonStrategy));

        var firstReady = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseFirst = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var first = Task.Run(async () =>
        {
            await using var lease = await gate.EvaluateAsync(workflow, "order-1");
            firstReady.SetResult();
            await releaseFirst.Task;
            return lease.CanStart;
        });

        await firstReady.Task;

        var second = Task.Run(async () =>
        {
            await using var lease = await gate.EvaluateAsync(workflow, "order-1");
            return lease.CanStart;
        });

        await lockProvider.Contended.Task;
        Assert.False(second.IsCompleted);
        await evaluator.Received(1).CanStartWorkflowAsync(Arg.Any<WorkflowActivationStrategyEvaluationContext>());

        releaseFirst.SetResult();
        var results = await Task.WhenAll(first, second);

        Assert.Contains(true, results);
        Assert.Contains(false, results);
        await evaluator.Received(2).CanStartWorkflowAsync(Arg.Any<WorkflowActivationStrategyEvaluationContext>());
    }

    [Fact]
    public async Task BuiltInLockKeys_AreHashedDeterministicAndScopedByTenantAndStrategy()
    {
        const string tenantA = "tenant-secret-a";
        const string tenantB = "tenant-secret-b";
        const string definitionId = "definition-secret";
        const string correlationId = "correlation-secret";
        var evaluator = Substitute.For<IWorkflowActivationStrategyEvaluator>();
        evaluator.CanStartWorkflowAsync(Arg.Any<WorkflowActivationStrategyEvaluationContext>()).Returns(true);
        var lockProvider = new InMemoryDistributedLockProvider();
        var singleton = CreateWorkflow(typeof(SingletonStrategy), definitionId);
        var correlatedSingleton = CreateWorkflow(typeof(CorrelatedSingletonStrategy), definitionId);
        var correlation = CreateWorkflow(typeof(CorrelationStrategy), definitionId);

        await using (await CreateGate(evaluator, lockProvider, tenantA).EvaluateAsync(singleton, correlationId)) { }
        await using (await CreateGate(evaluator, lockProvider, tenantA).EvaluateAsync(singleton, correlationId)) { }
        await using (await CreateGate(evaluator, lockProvider, tenantA).EvaluateAsync(correlatedSingleton, correlationId)) { }
        await using (await CreateGate(evaluator, lockProvider, tenantA).EvaluateAsync(correlation, correlationId)) { }
        await using (await CreateGate(evaluator, lockProvider, tenantB).EvaluateAsync(singleton, correlationId)) { }
        await using (await CreateGate(evaluator, lockProvider, tenantB).EvaluateAsync(correlatedSingleton, correlationId)) { }
        await using (await CreateGate(evaluator, lockProvider, tenantB).EvaluateAsync(correlation, correlationId)) { }

        var keys = lockProvider.LockNames.ToArray();
        Assert.Equal(7, keys.Length);
        Assert.Equal(keys[0], keys[1]);
        Assert.Equal(6, keys.Skip(1).Distinct().Count());
        Assert.All(keys, key =>
        {
            Assert.DoesNotContain(tenantA, key, StringComparison.Ordinal);
            Assert.DoesNotContain(tenantB, key, StringComparison.Ordinal);
            Assert.DoesNotContain(definitionId, key, StringComparison.Ordinal);
            Assert.DoesNotContain(correlationId, key, StringComparison.Ordinal);
            Assert.Matches("^workflow-activation:v1:[a-z-]+:[A-F0-9]{64}$", key);
        });
    }

    [Fact]
    public async Task UsesDefaultTenantScope_WhenTenantAccessorIsNotRegistered()
    {
        var evaluator = Substitute.For<IWorkflowActivationStrategyEvaluator>();
        evaluator.CanStartWorkflowAsync(Arg.Any<WorkflowActivationStrategyEvaluationContext>()).Returns(true);
        var lockProvider = new InMemoryDistributedLockProvider();
        using var serviceProvider = new ServiceCollection()
            .AddSingleton<IWorkflowActivationStrategyEvaluator>(evaluator)
            .AddSingleton<IDistributedLockProvider>(lockProvider)
            .AddSingleton(Microsoft.Extensions.Options.Options.Create(new DistributedLockingOptions()))
            .AddScoped<WorkflowActivationGate>()
            .BuildServiceProvider();
        using var serviceScope = serviceProvider.CreateScope();
        var unregisteredAccessorGate = serviceScope.ServiceProvider.GetRequiredService<WorkflowActivationGate>();
        var tenantAccessor = new DefaultTenantAccessor();
        var defaultTenantGate = new WorkflowActivationGate(
            evaluator,
            lockProvider,
            Microsoft.Extensions.Options.Options.Create(new DistributedLockingOptions()),
            tenantAccessor);
        var workflow = CreateWorkflow(typeof(SingletonStrategy));

        await using (await unregisteredAccessorGate.EvaluateAsync(workflow, null)) { }
        await using (await defaultTenantGate.EvaluateAsync(workflow, null)) { }
        using (tenantAccessor.PushContext(new Tenant { Id = "tenant-b", Name = "Tenant B" }))
            await using (await defaultTenantGate.EvaluateAsync(workflow, null)) { }

        var keys = lockProvider.LockNames.ToArray();
        Assert.Equal(3, keys.Length);
        Assert.Equal(keys[0], keys[1]);
        Assert.NotEqual(keys[1], keys[2]);
        Assert.DoesNotContain("tenant-b", keys[2], StringComparison.Ordinal);
    }

    [Fact]
    public async Task CustomStrategy_IsEvaluatedWithoutInventedDistributedScope()
    {
        var evaluator = Substitute.For<IWorkflowActivationStrategyEvaluator>();
        evaluator.CanStartWorkflowAsync(Arg.Any<WorkflowActivationStrategyEvaluationContext>()).Returns(true);
        var lockProvider = new InMemoryDistributedLockProvider();
        var gate = CreateGate(evaluator, lockProvider);

        await using var lease = await gate.EvaluateAsync(CreateWorkflow(typeof(CustomStrategy)), "corr-1");

        Assert.True(lease.CanStart);
        Assert.Empty(lockProvider.LockNames);
        await evaluator.Received(1).CanStartWorkflowAsync(Arg.Any<WorkflowActivationStrategyEvaluationContext>());
    }

    private static WorkflowActivationGate CreateGate(IWorkflowActivationStrategyEvaluator evaluator, IDistributedLockProvider lockProvider, string tenantId = "tenant-a")
    {
        var tenantAccessor = Substitute.For<ITenantAccessor>();
        tenantAccessor.TenantId.Returns(tenantId);
        return new(
            evaluator,
            lockProvider,
            Microsoft.Extensions.Options.Options.Create(new DistributedLockingOptions()),
            tenantAccessor);
    }

    private static Workflow CreateWorkflow(Type strategyType, string definitionId = "def-1") => new()
    {
        Identity = new WorkflowIdentity(definitionId, 1, definitionId),
        Options = new WorkflowOptions { ActivationStrategyType = strategyType }
    };

    private sealed class CustomStrategy : IWorkflowActivationStrategy
    {
        public ValueTask<bool> GetAllowActivationAsync(WorkflowInstantiationStrategyContext context) => ValueTask.FromResult(true);
    }

    private sealed class InMemoryDistributedLockProvider : IDistributedLockProvider
    {
        private readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new();
        private readonly ConcurrentQueue<string> _lockNames = new();

        public int HeldLocks => _locks.Values.Count(semaphore => semaphore.CurrentCount == 0);
        public IReadOnlyCollection<string> LockNames => _lockNames.ToArray();
        public TaskCompletionSource Contended { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public IDistributedLock CreateLock(string name)
        {
            _lockNames.Enqueue(name);
            return new InMemoryDistributedLock(_locks.GetOrAdd(name, _ => new SemaphoreSlim(1, 1)), name, Contended);
        }
    }

    private sealed class HandleLostTokenDistributedLockProvider(CancellationToken handleLostToken) : IDistributedLockProvider
    {
        private HandleLostTokenDistributedSynchronizationHandle Handle { get; } = new(handleLostToken);
        public int HandleDisposeCount => Handle.DisposeCount;

        public IDistributedLock CreateLock(string name) => new HandleLostTokenDistributedLock(name, Handle);

        private sealed class HandleLostTokenDistributedLock(string name, HandleLostTokenDistributedSynchronizationHandle handle) : IDistributedLock
        {
            public string Name => name;

            public IDistributedSynchronizationHandle? TryAcquire(TimeSpan timeout = default, CancellationToken cancellationToken = default) => handle;

            public IDistributedSynchronizationHandle Acquire(TimeSpan? timeout = null, CancellationToken cancellationToken = default) => handle;

            public ValueTask<IDistributedSynchronizationHandle?> TryAcquireAsync(TimeSpan timeout = default, CancellationToken cancellationToken = default) =>
                ValueTask.FromResult<IDistributedSynchronizationHandle?>(handle);

            public ValueTask<IDistributedSynchronizationHandle> AcquireAsync(TimeSpan? timeout = null, CancellationToken cancellationToken = default) =>
                ValueTask.FromResult<IDistributedSynchronizationHandle>(handle);
        }

        private sealed class HandleLostTokenDistributedSynchronizationHandle(CancellationToken handleLostToken) : IDistributedSynchronizationHandle
        {
            private int _disposeCount;

            public CancellationToken HandleLostToken => handleLostToken;
            public int DisposeCount => Volatile.Read(ref _disposeCount);

            public void Dispose() => Interlocked.Increment(ref _disposeCount);

            public ValueTask DisposeAsync()
            {
                Dispose();
                return ValueTask.CompletedTask;
            }
        }
    }

    private sealed class InMemoryDistributedLock(SemaphoreSlim semaphore, string name, TaskCompletionSource contended) : IDistributedLock
    {
        public string Name => name;

        public IDistributedSynchronizationHandle? TryAcquire(TimeSpan timeout = new(), CancellationToken cancellationToken = new()) =>
            semaphore.Wait(timeout, cancellationToken) ? new Handle(semaphore) : null;

        public IDistributedSynchronizationHandle Acquire(TimeSpan? timeout = null, CancellationToken cancellationToken = new())
        {
            if (!semaphore.Wait(timeout ?? Timeout.InfiniteTimeSpan, cancellationToken))
                throw new TimeoutException($"Could not acquire lock '{name}'.");

            return new Handle(semaphore);
        }

        public async ValueTask<IDistributedSynchronizationHandle?> TryAcquireAsync(TimeSpan timeout = new(), CancellationToken cancellationToken = new())
        {
            SignalContention();
            return await semaphore.WaitAsync(timeout, cancellationToken) ? new Handle(semaphore) : null;
        }

        public async ValueTask<IDistributedSynchronizationHandle> AcquireAsync(TimeSpan? timeout = null, CancellationToken cancellationToken = new())
        {
            SignalContention();
            if (!await semaphore.WaitAsync(timeout ?? Timeout.InfiniteTimeSpan, cancellationToken))
                throw new TimeoutException($"Could not acquire lock '{name}'.");

            return new Handle(semaphore);
        }

        private void SignalContention()
        {
            if (semaphore.CurrentCount == 0)
                contended.TrySetResult();
        }

        private sealed class Handle(SemaphoreSlim semaphore) : IDistributedSynchronizationHandle
        {
            public CancellationToken HandleLostToken => CancellationToken.None;

            public void Dispose() => semaphore.Release();

            public ValueTask DisposeAsync()
            {
                semaphore.Release();
                return ValueTask.CompletedTask;
            }
        }
    }
}
