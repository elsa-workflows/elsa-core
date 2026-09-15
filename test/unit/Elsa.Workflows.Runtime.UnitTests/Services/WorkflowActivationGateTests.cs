using System.Collections.Concurrent;
using Elsa.Common.DistributedHosting;
using Elsa.Common.Multitenancy;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Models;
using Elsa.Workflows.Runtime.ActivationValidators;
using Medallion.Threading;
using Microsoft.Extensions.Options;
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
        Assert.Equal(0, lockProvider.HeldLocks);
        await evaluator.Received(1).CanStartWorkflowAsync(Arg.Is<WorkflowActivationStrategyEvaluationContext>(
            context => context.Workflow == workflow && context.CorrelationId == "order-1"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public async Task AllowsWithoutLock_WhenCorrelatedSingletonHasBlankCorrelationId(string? correlationId)
    {
        var evaluator = Substitute.For<IWorkflowActivationStrategyEvaluator>();
        evaluator.CanStartWorkflowAsync(Arg.Any<WorkflowActivationStrategyEvaluationContext>()).Returns(true);
        var lockProvider = new InMemoryDistributedLockProvider();
        var gate = CreateGate(evaluator, lockProvider);
        var workflow = CreateWorkflow(typeof(CorrelatedSingletonStrategy));

        await using var lease = await gate.EvaluateAsync(workflow, correlationId);

        Assert.True(lease.CanStart);
        Assert.Equal(0, lockProvider.HeldLocks);
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
    public async Task SerializesCheckAndCreate_WhenTwoCallersRaceOnSameCorrelation()
    {
        var started = 0;
        var evaluator = Substitute.For<IWorkflowActivationStrategyEvaluator>();
        evaluator.CanStartWorkflowAsync(Arg.Any<WorkflowActivationStrategyEvaluationContext>())
            .Returns(_ => Interlocked.CompareExchange(ref started, 1, 0) == 0);
        var gate = CreateGate(evaluator, new InMemoryDistributedLockProvider());
        var workflow = CreateWorkflow(typeof(CorrelatedSingletonStrategy));

        var firstReady = new TaskCompletionSource();
        var releaseFirst = new TaskCompletionSource();

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

        await Task.Delay(50);
        Assert.False(second.IsCompleted);

        releaseFirst.SetResult();
        var results = await Task.WhenAll(first, second);

        Assert.Contains(true, results);
        Assert.Contains(false, results);
    }

    private static WorkflowActivationGate CreateGate(IWorkflowActivationStrategyEvaluator evaluator, IDistributedLockProvider lockProvider)
    {
        var tenantAccessor = Substitute.For<ITenantAccessor>();
        tenantAccessor.TenantId.Returns("");
        return new(
            evaluator,
            lockProvider,
            Microsoft.Extensions.Options.Options.Create(new DistributedLockingOptions()),
            tenantAccessor);
    }

    private static Workflow CreateWorkflow(Type strategyType)
    {
        return new()
        {
            Identity = new WorkflowIdentity("def-1", 1, "def-1"),
            Options = new WorkflowOptions
            {
                ActivationStrategyType = strategyType
            }
        };
    }

    private sealed class InMemoryDistributedLockProvider : IDistributedLockProvider
    {
        private readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new();

        public int HeldLocks => _locks.Values.Count(semaphore => semaphore.CurrentCount == 0);

        public IDistributedLock CreateLock(string name) =>
            new InMemoryDistributedLock(_locks.GetOrAdd(name, _ => new SemaphoreSlim(1, 1)), name);
    }

    private sealed class InMemoryDistributedLock(SemaphoreSlim semaphore, string name) : IDistributedLock
    {
        public string Name => name;

        public IDistributedSynchronizationHandle? TryAcquire(TimeSpan timeout = new(), CancellationToken cancellationToken = new())
        {
            return semaphore.Wait(timeout, cancellationToken) ? new Handle(semaphore) : null;
        }

        public IDistributedSynchronizationHandle Acquire(TimeSpan? timeout = null, CancellationToken cancellationToken = new())
        {
            if (!semaphore.Wait(timeout ?? Timeout.InfiniteTimeSpan, cancellationToken))
                throw new TimeoutException($"Could not acquire lock '{name}'.");

            return new Handle(semaphore);
        }

        public async ValueTask<IDistributedSynchronizationHandle?> TryAcquireAsync(TimeSpan timeout = new(), CancellationToken cancellationToken = new())
        {
            return await semaphore.WaitAsync(timeout, cancellationToken) ? new Handle(semaphore) : null;
        }

        public async ValueTask<IDistributedSynchronizationHandle> AcquireAsync(TimeSpan? timeout = null, CancellationToken cancellationToken = new())
        {
            if (!await semaphore.WaitAsync(timeout ?? Timeout.InfiniteTimeSpan, cancellationToken))
                throw new TimeoutException($"Could not acquire lock '{name}'.");

            return new Handle(semaphore);
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
