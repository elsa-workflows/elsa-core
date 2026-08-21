using Elsa.Alterations.Core.Contracts;
using Elsa.Alterations.Core.Entities;
using Elsa.Alterations.Services;
using Elsa.Common.Multitenancy;
using Elsa.Mediator.Contracts;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace Elsa.Alterations.IntegrationTests;

public class BackgroundAlterationJobDispatcherTests
{
    [Fact]
    public async Task DispatchAsync_WhenQueuedWorkRunsAfterDispatchScopeEnds_PreservesTenant()
    {
        const string jobId = "alteration-job";
        Func<CancellationToken, Task>? queuedCallback = null;
        var jobQueue = Substitute.For<IJobQueue>();
        jobQueue
            .Enqueue(Arg.Any<Func<CancellationToken, Task>>())
            .Returns(callInfo =>
            {
                queuedCallback = callInfo.Arg<Func<CancellationToken, Task>>();
                return "queued-job";
            });

        var tenantAccessor = new DefaultTenantAccessor();
        var runner = new RecordingAlterationJobRunner(tenantAccessor);
        var services = new ServiceCollection()
            .AddSingleton<IJobQueue>(jobQueue)
            .AddSingleton<ITenantAccessor>(tenantAccessor)
            .AddSingleton<ITenantScopeFactory, DefaultTenantScopeFactory>()
            .AddScoped<IAlterationJobRunner>(_ => runner)
            .AddScoped<BackgroundAlterationJobDispatcher>();
        await using var serviceProvider = services.BuildServiceProvider(validateScopes: true);
        var dispatchingTenant = new Tenant { Id = "tenant-a", Name = "Tenant A" };
        var workerTenant = new Tenant { Id = "tenant-b", Name = "Tenant B" };

        using (tenantAccessor.PushContext(dispatchingTenant))
        using (var dispatchScope = serviceProvider.CreateScope())
        {
            var dispatcher = dispatchScope.ServiceProvider.GetRequiredService<BackgroundAlterationJobDispatcher>();
            await dispatcher.DispatchAsync(jobId);
        }

        var callback = Assert.IsType<Func<CancellationToken, Task>>(queuedCallback);
        using (tenantAccessor.PushContext(workerTenant))
        {
            await callback(CancellationToken.None);
            Assert.Same(workerTenant, tenantAccessor.Tenant);
        }

        Assert.Equal(dispatchingTenant.Id, runner.ObservedTenantId);
        Assert.Null(tenantAccessor.Tenant);
    }

    private sealed class RecordingAlterationJobRunner(ITenantAccessor tenantAccessor) : IAlterationJobRunner
    {
        public string? ObservedTenantId { get; private set; }

        public Task<AlterationJob> RunAsync(string jobId, CancellationToken cancellationToken = default)
        {
            ObservedTenantId = tenantAccessor.TenantId;
            return Task.FromResult(new AlterationJob { Id = jobId });
        }
    }
}
