using System.Collections.Concurrent;
using Elsa.Alterations.Core.Contracts;
using Elsa.Alterations.Core.Entities;
using Elsa.Alterations.Services;
using Elsa.Common.Multitenancy;
using Elsa.Mediator.Contracts;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace Elsa.Alterations.IntegrationTests;

public class BackgroundAlterationJobDispatcherTests : IAsyncLifetime
{
    private readonly List<Func<CancellationToken, Task>> _queuedCallbacks = [];
    private readonly DefaultTenantAccessor _tenantAccessor;
    private readonly RecordingAlterationJobRunner _runner;
    private readonly ServiceProvider _serviceProvider;

    public BackgroundAlterationJobDispatcherTests()
    {
        var jobQueue = Substitute.For<IJobQueue>();
        jobQueue
            .Enqueue(Arg.Any<Func<CancellationToken, Task>>())
            .Returns(callInfo =>
            {
                var callback = callInfo.Arg<Func<CancellationToken, Task>>();
                _queuedCallbacks.Add(callback);
                return $"queued-job-{_queuedCallbacks.Count}";
            });

        _tenantAccessor = new DefaultTenantAccessor();
        _runner = new RecordingAlterationJobRunner(_tenantAccessor);
        var services = new ServiceCollection()
            .AddSingleton<IJobQueue>(jobQueue)
            .AddSingleton<ITenantAccessor>(_tenantAccessor)
            .AddSingleton<ITenantScopeFactory, DefaultTenantScopeFactory>()
            .AddScoped<IAlterationJobRunner>(_ => _runner)
            .AddScoped<BackgroundAlterationJobDispatcher>();
        _serviceProvider = services.BuildServiceProvider(validateScopes: true);
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync() => await _serviceProvider.DisposeAsync();

    [Fact]
    public async Task DispatchAsync_WhenQueuedWorkRunsAfterDispatchScopeEnds_PreservesTenant()
    {
        const string jobId = "alteration-job";
        var dispatchingTenant = new Tenant { Id = "tenant-a", Name = "Tenant A" };
        var workerTenant = new Tenant { Id = "tenant-b", Name = "Tenant B" };

        await DispatchAndRunUnderWorkerTenantAsync(jobId, dispatchingTenant, workerTenant, callback => callback(CancellationToken.None));
    }

    [Fact]
    public async Task DispatchAsync_WhenRunnerThrows_StillRestoresWorkerTenant()
    {
        const string jobId = "alteration-job";
        var dispatchingTenant = new Tenant { Id = "tenant-a", Name = "Tenant A" };
        var workerTenant = new Tenant { Id = "tenant-b", Name = "Tenant B" };
        _runner.ExceptionToThrow = new InvalidOperationException("Runner failure");

        await DispatchAndRunUnderWorkerTenantAsync(
            jobId,
            dispatchingTenant,
            workerTenant,
            callback => Assert.ThrowsAsync<InvalidOperationException>(() => callback(CancellationToken.None)));
    }

    [Fact]
    public async Task DispatchAsync_WhenNoTenantIsPushedAtDispatchTime_UsesDefaultTenant()
    {
        const string jobId = "alteration-job";

        await DispatchAsync(jobId, tenant: null);

        var callback = Assert.Single(_queuedCallbacks);
        await callback(CancellationToken.None);

        // With no tenant pushed at dispatch time, DefaultTenantScopeFactory.CreateScope(null) pushes a null
        // tenant onto the accessor. DefaultTenantAccessor.TenantId then falls back to Tenant.DefaultTenantId
        // (an empty string) rather than null, so that is the value the runner observes.
        Assert.Equal(Tenant.DefaultTenantId, _runner.GetObservedTenantId(jobId));
        Assert.Null(_tenantAccessor.Tenant);
    }

    /// <summary>
    /// Dispatches a job under <paramref name="dispatchingTenant"/>, then runs the single queued callback while a
    /// different <paramref name="workerTenant"/> is active on the accessor, invoking it via
    /// <paramref name="runCallbackAsync"/> so callers can assert success or failure. Asserts the tenant behavior
    /// common to both outcomes: the worker tenant remains active for the duration of the callback, the runner
    /// observed the dispatching tenant, and the accessor's tenant is restored to null afterward.
    /// </summary>
    private async Task DispatchAndRunUnderWorkerTenantAsync(
        string jobId,
        Tenant dispatchingTenant,
        Tenant workerTenant,
        Func<Func<CancellationToken, Task>, Task> runCallbackAsync)
    {
        await DispatchAsync(jobId, dispatchingTenant);

        var callback = Assert.Single(_queuedCallbacks);
        using (_tenantAccessor.PushContext(workerTenant))
        {
            await runCallbackAsync(callback);
            Assert.Same(workerTenant, _tenantAccessor.Tenant);
        }

        Assert.Equal(dispatchingTenant.Id, _runner.GetObservedTenantId(jobId));
        Assert.Null(_tenantAccessor.Tenant);
    }

    [Fact]
    public async Task DispatchAsync_WhenConcurrentJobsBelongToDifferentTenants_TenantsAreNotExchanged()
    {
        const string jobAId = "alteration-job-a";
        const string jobBId = "alteration-job-b";
        var tenantA = new Tenant { Id = "tenant-a", Name = "Tenant A" };
        var tenantB = new Tenant { Id = "tenant-b", Name = "Tenant B" };

        await DispatchAsync(jobAId, tenantA);
        await DispatchAsync(jobBId, tenantB);

        Assert.Equal(2, _queuedCallbacks.Count);
        var callbackA = _queuedCallbacks[0];
        var callbackB = _queuedCallbacks[1];

        await Task.WhenAll(callbackA(CancellationToken.None), callbackB(CancellationToken.None));

        Assert.Equal(tenantA.Id, _runner.GetObservedTenantId(jobAId));
        Assert.Equal(tenantB.Id, _runner.GetObservedTenantId(jobBId));
    }

    private async Task DispatchAsync(string jobId, Tenant? tenant)
    {
        using (_tenantAccessor.PushContext(tenant))
        using (var dispatchScope = _serviceProvider.CreateScope())
        {
            var dispatcher = dispatchScope.ServiceProvider.GetRequiredService<BackgroundAlterationJobDispatcher>();
            await dispatcher.DispatchAsync(jobId);
        }
    }

    private sealed class RecordingAlterationJobRunner(ITenantAccessor tenantAccessor) : IAlterationJobRunner
    {
        private readonly ConcurrentDictionary<string, string?> _observedTenantIdsByJobId = new();

        public Exception? ExceptionToThrow { get; set; }

        public string? GetObservedTenantId(string jobId) => _observedTenantIdsByJobId.TryGetValue(jobId, out var tenantId) ? tenantId : null;

        public async Task<AlterationJob> RunAsync(string jobId, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            _observedTenantIdsByJobId[jobId] = tenantAccessor.TenantId;

            if (ExceptionToThrow is not null)
                throw ExceptionToThrow;

            return new AlterationJob { Id = jobId };
        }
    }
}
