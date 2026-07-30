using Elsa.Common.Multitenancy;
using Elsa.Common.Multitenancy.EventHandlers;
using Elsa.Common.RecurringTasks;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace Elsa.Common.UnitTests.Multitenancy;

/// <summary>
/// Tests for <see cref="DefaultTenantService"/>, including the fallback to activate
/// <see cref="Tenant.Default"/> when the tenant provider returns an empty list.
/// </summary>
public class DefaultTenantServiceTests
{
    [Fact]
    public async Task ActivateTenantsAsync_WhenProviderReturnsEmpty_ActivatesDefaultTenant()
    {
        // Arrange - provider returns no tenants
        var (tenantService, serviceProvider) = await CreateTenantServiceAsync(Array.Empty<Tenant>());

        try
        {
            // Act
            await tenantService.ActivateTenantsAsync();

            // Assert
            var tenants = (await tenantService.ListAsync()).ToList();
            Assert.Single(tenants);
            Assert.Same(Tenant.Default, tenants[0]);
            Assert.Equal(Tenant.DefaultTenantId, tenants[0].Id);
        }
        finally
        {
            if (tenantService is IAsyncDisposable disposable)
                await disposable.DisposeAsync();
            await serviceProvider.DisposeAsync();
        }
    }

    [Fact]
    public async Task ListAsync_WhenProviderReturnsEmpty_ReturnsDefaultTenant()
    {
        // Arrange - ListAsync triggers initialization when provider returns empty
        var (tenantService, serviceProvider) = await CreateTenantServiceAsync(Array.Empty<Tenant>());

        try
        {
            // Act
            var tenants = (await tenantService.ListAsync()).ToList();

            // Assert
            Assert.Single(tenants);
            Assert.Same(Tenant.Default, tenants[0]);
        }
        finally
        {
            if (tenantService is IAsyncDisposable disposable)
                await disposable.DisposeAsync();
            await serviceProvider.DisposeAsync();
        }
    }

    [Fact]
    public async Task ActivateTenantsAsync_WhenProviderReturnsTenants_ReturnsThoseTenants()
    {
        // Arrange - provider returns specific tenants
        var tenant1 = new Tenant { Id = "tenant-1", Name = "Tenant 1" };
        var tenant2 = new Tenant { Id = "tenant-2", Name = "Tenant 2" };
        var (tenantService, serviceProvider) = await CreateTenantServiceAsync([tenant1, tenant2]);

        try
        {
            // Act
            await tenantService.ActivateTenantsAsync();

            // Assert - should not use Tenant.Default fallback
            var tenants = (await tenantService.ListAsync()).ToList();
            Assert.Equal(2, tenants.Count);
            Assert.Contains(tenants, t => t.Id == "tenant-1");
            Assert.Contains(tenants, t => t.Id == "tenant-2");
        }
        finally
        {
            if (tenantService is IAsyncDisposable disposable)
                await disposable.DisposeAsync();
            await serviceProvider.DisposeAsync();
        }
    }

    [Fact]
    public async Task RefreshAsync_WhenProviderChangesFromTenantsToEmpty_KeepsDefaultTenant()
    {
        // Arrange - start with tenants, then provider returns empty (simulating config change)
        var tenant1 = new Tenant { Id = "tenant-1", Name = "Tenant 1" };
        var providerReturns = new List<Tenant> { tenant1 };
        var (tenantService, serviceProvider) = await CreateTenantServiceAsync(providerReturns, () => providerReturns);

        try
        {
            await tenantService.ActivateTenantsAsync();
            Assert.Single(await tenantService.ListAsync());

            // Simulate provider now returning empty (e.g., config removed all tenants)
            providerReturns.Clear();

            // Act
            await tenantService.RefreshAsync();

            // Assert - should fall back to Tenant.Default instead of having zero tenants
            var tenants = (await tenantService.ListAsync()).ToList();
            Assert.Single(tenants);
            Assert.Same(Tenant.Default, tenants[0]);
        }
        finally
        {
            if (tenantService is IAsyncDisposable disposable)
                await disposable.DisposeAsync();
            await serviceProvider.DisposeAsync();
        }
    }

    [Fact]
    public async Task DeactivateTenantsAsync_WhenRefreshIsInProgress_WaitsForRefreshBeforeDeactivating()
    {
        var tenant = new Tenant { Id = "tenant-1", Name = "Tenant 1" };
        var timeout = TimeSpan.FromSeconds(5);
        var refreshStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var completeRefresh = new TaskCompletionSource<IEnumerable<Tenant>>(TaskCreationOptions.RunContinuationsAsynchronously);
        var listRequests = 0;
        var tenantsProvider = Substitute.For<ITenantsProvider>();
        tenantsProvider.ListAsync(Arg.Any<CancellationToken>()).Returns(async _ =>
        {
            if (Interlocked.Increment(ref listRequests) == 1)
            {
                return new[] { tenant };
            }

            refreshStarted.TrySetResult();
            return await completeRefresh.Task;
        });

        var (tenantService, serviceProvider) = await CreateTenantServiceAsync(tenantsProvider);
        Task? refreshTask = null;
        Task? deactivateTask = null;

        try
        {
            await tenantService.ListAsync();

            refreshTask = tenantService.RefreshAsync();
            await refreshStarted.Task.WaitAsync(timeout);

            deactivateTask = tenantService.DeactivateTenantsAsync();

            Assert.False(deactivateTask.IsCompleted);

            completeRefresh.SetResult([tenant]);
            await refreshTask.WaitAsync(timeout);
            await deactivateTask.WaitAsync(timeout);

            Assert.Empty(await tenantService.ListAsync());
        }
        finally
        {
            completeRefresh.TrySetResult([tenant]);

            if (refreshTask != null)
            {
                await refreshTask.WaitAsync(timeout);
            }

            if (deactivateTask != null)
            {
                await deactivateTask.WaitAsync(timeout);
            }

            if (tenantService is IAsyncDisposable disposable)
            {
                await disposable.DisposeAsync();
            }

            await serviceProvider.DisposeAsync();
        }
    }

    [Fact]
    public async Task DeactivateTenantsAsync_WhenInitializationIsInProgress_WaitsForInitializationBeforeDeactivating()
    {
        var tenant = new Tenant { Id = "tenant-1", Name = "Tenant 1" };
        var timeout = TimeSpan.FromSeconds(5);
        var initializationStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var completeInitialization = new TaskCompletionSource<IEnumerable<Tenant>>(TaskCreationOptions.RunContinuationsAsynchronously);
        var tenantsProvider = Substitute.For<ITenantsProvider>();
        tenantsProvider.ListAsync(Arg.Any<CancellationToken>()).Returns(async _ =>
        {
            initializationStarted.TrySetResult();
            return await completeInitialization.Task;
        });

        var (tenantService, serviceProvider) = await CreateTenantServiceAsync(tenantsProvider);
        Task<IEnumerable<Tenant>>? initializationTask = null;
        Task? deactivateTask = null;

        try
        {
            initializationTask = tenantService.ListAsync();
            await initializationStarted.Task.WaitAsync(timeout);

            deactivateTask = tenantService.DeactivateTenantsAsync();

            Assert.False(deactivateTask.IsCompleted);

            completeInitialization.SetResult([tenant]);
            await initializationTask.WaitAsync(timeout);
            await deactivateTask.WaitAsync(timeout);

            Assert.Empty(await tenantService.ListAsync());
        }
        finally
        {
            completeInitialization.TrySetResult([tenant]);

            if (initializationTask != null)
            {
                await initializationTask.WaitAsync(timeout);
            }

            if (deactivateTask != null)
            {
                await deactivateTask.WaitAsync(timeout);
            }

            if (tenantService is IAsyncDisposable disposable)
            {
                await disposable.DisposeAsync();
            }

            await serviceProvider.DisposeAsync();
        }
    }

    private static Task<(ITenantService TenantService, ServiceProvider ServiceProvider)> CreateTenantServiceAsync(IEnumerable<Tenant> tenants, Func<List<Tenant>>? tenantsFactory = null)
    {
        var tenantList = tenants.ToList();
        var getTenants = tenantsFactory ?? (() => tenantList);

        var tenantsProvider = Substitute.For<ITenantsProvider>();
        tenantsProvider.ListAsync(Arg.Any<CancellationToken>()).Returns(_ => getTenants());

        return CreateTenantServiceAsync(tenantsProvider);
    }

    private static Task<(ITenantService TenantService, ServiceProvider ServiceProvider)> CreateTenantServiceAsync(ITenantsProvider tenantsProvider)
    {
        var services = new ServiceCollection();
        services.AddSingleton(_ => tenantsProvider);
        services.AddSingleton<ITenantScopeFactory, DefaultTenantScopeFactory>();
        services.AddSingleton<ITenantAccessor, DefaultTenantAccessor>();
        services.AddSingleton<ITenantActivatedEvent>(Substitute.For<ITenantActivatedEvent>());
        services.AddSingleton<ITenantDeactivatedEvent>(Substitute.For<ITenantDeactivatedEvent>());
        services.AddSingleton<ITenantDeletedEvent>(Substitute.For<ITenantDeletedEvent>());
        services.AddSingleton<TenantEventsManager>();
        services.AddSingleton<RecurringTaskScheduleManager>();
        services.AddSingleton<ITenantService, DefaultTenantService>();
        services.AddLogging();

        var serviceProvider = services.BuildServiceProvider();
        return Task.FromResult((serviceProvider.GetRequiredService<ITenantService>(), serviceProvider));
    }
}
