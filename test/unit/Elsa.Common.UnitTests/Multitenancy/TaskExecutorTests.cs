using Elsa.Common.DistributedHosting;
using Elsa.Common.Multitenancy;
using Elsa.Common.RecurringTasks;
using Medallion.Threading;
using Microsoft.Extensions.Options;

namespace Elsa.Common.UnitTests.Multitenancy;

public class TaskExecutorTests
{
    private readonly RecordingLockProvider _lockProvider = new();

    [Fact]
    public async Task ExecuteTaskAsync_WithoutTheSingleNodeAttribute_TakesNoLock()
    {
        var task = new PlainTask();

        await CreateTaskExecutor().ExecuteTaskAsync(task, CancellationToken.None);

        Assert.Empty(_lockProvider.LockNames);
        Assert.True(task.WasExecuted);
    }

    [Fact]
    public async Task ExecuteTaskAsync_ForATenant_QualifiesTheLockNameWithTheTenant()
    {
        await CreateTaskExecutor("tenant-1").ExecuteTaskAsync(new TenantScopedTask(), CancellationToken.None);

        Assert.Equal($"{typeof(TenantScopedTask).AssemblyQualifiedName}:tenant-1", Assert.Single(_lockProvider.LockNames));
    }

    [Fact]
    public async Task ExecuteTaskAsync_ForDifferentTenants_TakesDifferentLocks()
    {
        await CreateTaskExecutor("tenant-1").ExecuteTaskAsync(new TenantScopedTask(), CancellationToken.None);
        await CreateTaskExecutor("tenant-2").ExecuteTaskAsync(new TenantScopedTask(), CancellationToken.None);

        Assert.Equal(2, _lockProvider.LockNames.Distinct().Count());
    }

    [Fact]
    public async Task ExecuteTaskAsync_ForTheDefaultTenant_KeepsTheUnqualifiedLockName()
    {
        await CreateTaskExecutor().ExecuteTaskAsync(new TenantScopedTask(), CancellationToken.None);

        Assert.Equal(typeof(TenantScopedTask).AssemblyQualifiedName, Assert.Single(_lockProvider.LockNames));
    }

    [Fact]
    public async Task ExecuteTaskAsync_ForAHostScopedTask_KeepsTheUnqualifiedLockNameForEveryTenant()
    {
        await CreateTaskExecutor("tenant-1").ExecuteTaskAsync(new HostScopedTask(), CancellationToken.None);
        await CreateTaskExecutor("tenant-2").ExecuteTaskAsync(new HostScopedTask(), CancellationToken.None);

        Assert.Equal([typeof(HostScopedTask).AssemblyQualifiedName!, typeof(HostScopedTask).AssemblyQualifiedName!], _lockProvider.LockNames);
    }

    [Fact]
    public void SingleNodeTaskAttribute_WithoutAnExplicitScope_IsTenantScoped()
    {
        Assert.Equal(SingleNodeTaskScope.Tenant, new SingleNodeTaskAttribute().Scope);
    }

    private TaskExecutor CreateTaskExecutor(string? tenantId = null)
    {
        var tenantAccessor = new DefaultTenantAccessor();

        if (tenantId != null)
            tenantAccessor.PushContext(new() { Id = tenantId });

        return new(_lockProvider, tenantAccessor, Microsoft.Extensions.Options.Options.Create(new DistributedLockingOptions()));
    }

    private class PlainTask : ITask
    {
        public bool WasExecuted { get; private set; }

        public Task ExecuteAsync(CancellationToken cancellationToken)
        {
            WasExecuted = true;
            return Task.CompletedTask;
        }
    }

    [SingleNodeTask]
    private class TenantScopedTask : ITask
    {
        public Task ExecuteAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    [SingleNodeTask(SingleNodeTaskScope.Host)]
    private class HostScopedTask : ITask
    {
        public Task ExecuteAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private class RecordingLockProvider : IDistributedLockProvider
    {
        public List<string> LockNames { get; } = [];

        public IDistributedLock CreateLock(string name)
        {
            LockNames.Add(name);
            return new GrantedLock(name);
        }

        private class GrantedLock(string name) : IDistributedLock
        {
            public string Name { get; } = name;

            public IDistributedSynchronizationHandle? TryAcquire(TimeSpan timeout = default, CancellationToken cancellationToken = default) => new GrantedHandle();
            public IDistributedSynchronizationHandle Acquire(TimeSpan? timeout = null, CancellationToken cancellationToken = default) => new GrantedHandle();
            public ValueTask<IDistributedSynchronizationHandle?> TryAcquireAsync(TimeSpan timeout = default, CancellationToken cancellationToken = default) => new((IDistributedSynchronizationHandle?)new GrantedHandle());
            public ValueTask<IDistributedSynchronizationHandle> AcquireAsync(TimeSpan? timeout = null, CancellationToken cancellationToken = default) => new(new GrantedHandle());
        }

        private class GrantedHandle : IDistributedSynchronizationHandle
        {
            public CancellationToken HandleLostToken => CancellationToken.None;
            public void Dispose() { }
            public ValueTask DisposeAsync() => default;
        }
    }
}
