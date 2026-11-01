using System.Collections.Concurrent;
using Elsa.Common.Multitenancy;
using Elsa.Common.Multitenancy.EventHandlers;
using Elsa.Common.RecurringTasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
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
    public async Task ActivateTenantsAsync_WhenActivationFails_RetriesOnNextActivation()
    {
        var activatedEvent = Substitute.For<ITenantActivatedEvent>();
        activatedEvent.TenantActivatedAsync(Arg.Any<TenantActivatedEventArgs>())
            .Returns(Task.FromException(new InvalidOperationException("Migration failed")), Task.CompletedTask);
        var (tenantService, serviceProvider) = await CreateTenantServiceAsync(Array.Empty<Tenant>(), activatedEvent: activatedEvent);

        try
        {
            await Assert.ThrowsAsync<InvalidOperationException>(() => tenantService.ActivateTenantsAsync());

            await tenantService.ActivateTenantsAsync();

            var tenants = (await tenantService.ListAsync()).ToList();
            Assert.Same(Tenant.Default, Assert.Single(tenants));
            await activatedEvent.Received(2).TenantActivatedAsync(Arg.Any<TenantActivatedEventArgs>());
        }
        finally
        {
            await DisposeAsync(tenantService, serviceProvider);
        }
    }

    [Fact]
    public async Task ActivateTenantsAsync_WhenLaterTenantFails_DeactivatesTenantsThatActivated()
    {
        var tenantA = new Tenant { Id = "a", Name = "A" };
        var tenantB = new Tenant { Id = "b", Name = "B" };
        var activatedEvent = Substitute.For<ITenantActivatedEvent>();
        activatedEvent.TenantActivatedAsync(Arg.Is<TenantActivatedEventArgs>(x => x.Tenant.Id == "b"))
            .Returns(Task.FromException(new InvalidOperationException("Migration failed")));
        var deactivatedEvent = Substitute.For<ITenantDeactivatedEvent>();
        var allTenantsActivatedEvent = Substitute.For<IAllTenantsActivatedEvent>();
        var (tenantService, serviceProvider) = await CreateTenantServiceAsync([tenantA, tenantB], activatedEvent: activatedEvent, deactivatedEvent: deactivatedEvent, allTenantsActivatedEvent: allTenantsActivatedEvent);

        try
        {
            await Assert.ThrowsAsync<InvalidOperationException>(() => tenantService.ActivateTenantsAsync());
            await allTenantsActivatedEvent.DidNotReceive().AllTenantsActivatedAsync(Arg.Any<CancellationToken>());

            await deactivatedEvent.Received(1).TenantDeactivatedAsync(Arg.Is<TenantDeactivatedEventArgs>(x => x.Tenant.Id == "a"));
            await deactivatedEvent.DidNotReceive().TenantDeactivatedAsync(Arg.Is<TenantDeactivatedEventArgs>(x => x.Tenant.Id == "b"));
        }
        finally
        {
            await DisposeAsync(tenantService, serviceProvider);
        }
    }

    [Fact]
    public async Task ActivateTenantsAsync_WithMultipleTenants_RaisesAllTenantsActivatedOnceAfterAllTenantsActivated()
    {
        var calls = new List<string>();
        var activatedEvent = Substitute.For<ITenantActivatedEvent>();
        activatedEvent.TenantActivatedAsync(Arg.Any<TenantActivatedEventArgs>()).Returns(call =>
        {
            lock (calls)
                calls.Add($"activated:{call.Arg<TenantActivatedEventArgs>().Tenant.Id}");

            return Task.CompletedTask;
        });
        var allTenantsActivatedEvent = Substitute.For<IAllTenantsActivatedEvent>();
        allTenantsActivatedEvent.AllTenantsActivatedAsync(Arg.Any<CancellationToken>()).Returns(_ =>
        {
            lock (calls)
                calls.Add("all-activated");

            return Task.CompletedTask;
        });
        var tenantA = new Tenant { Id = "a", Name = "A" };
        var tenantB = new Tenant { Id = "b", Name = "B" };
        var (tenantService, serviceProvider) = await CreateTenantServiceAsync([tenantA, tenantB], activatedEvent: activatedEvent, allTenantsActivatedEvent: allTenantsActivatedEvent);

        try
        {
            await tenantService.ActivateTenantsAsync();

            await allTenantsActivatedEvent.Received(1).AllTenantsActivatedAsync(Arg.Any<CancellationToken>());
            Assert.Equal(3, calls.Count);
            Assert.Equal("all-activated", calls[^1]);
            Assert.Contains("activated:a", calls);
            Assert.Contains("activated:b", calls);
        }
        finally
        {
            await DisposeAsync(tenantService, serviceProvider);
        }
    }

    [Fact]
    public async Task RefreshAsync_WhenTenantIsAdded_DoesNotRaiseAllTenantsActivated()
    {
        var tenantA = new Tenant { Id = "a", Name = "A" };
        var tenantB = new Tenant { Id = "b", Name = "B" };
        var providerReturns = new List<Tenant> { tenantA };
        var allTenantsActivatedEvent = Substitute.For<IAllTenantsActivatedEvent>();
        var (tenantService, serviceProvider) = await CreateTenantServiceAsync(providerReturns, () => providerReturns, allTenantsActivatedEvent: allTenantsActivatedEvent);

        try
        {
            await tenantService.ActivateTenantsAsync();
            allTenantsActivatedEvent.ClearReceivedCalls();
            providerReturns.Add(tenantB);

            await tenantService.RefreshAsync();

            Assert.Equal(2, (await tenantService.ListAsync()).Count());
            await allTenantsActivatedEvent.DidNotReceive().AllTenantsActivatedAsync(Arg.Any<CancellationToken>());
        }
        finally
        {
            await DisposeAsync(tenantService, serviceProvider);
        }
    }

    [Fact]
    public async Task ActivateTenantsAsync_ActivatesAtMostMaxDegreeOfParallelismTenantsAtOnce()
    {
        var tenants = CreateTenants(7);
        var sync = new object();
        var current = 0;
        var peak = 0;
        var limitReached = NewGate();
        var release = NewGate();
        var handler = new DelegateActivationHandler(async args =>
        {
            int now;

            lock (sync)
            {
                now = ++current;
                peak = Math.Max(peak, now);
            }

            if (now == 2)
                limitReached.TrySetResult();

            await release.Task;

            lock (sync)
                current--;
        });
        var (tenantService, serviceProvider) = await CreateTenantServiceAsync(tenants, activatedEvent: handler, maxDegreeOfParallelism: 2);

        try
        {
            var activation = tenantService.ActivateTenantsAsync();
            await limitReached.Task.WaitAsync(SafetyTimeout);

            // Gives a wrongly started third activation the chance to show up before the gate opens.
            await Task.Delay(100);
            release.SetResult();
            await activation.WaitAsync(SafetyTimeout);

            Assert.Equal(2, peak);
            Assert.Equal(tenants.Count, (await tenantService.ListAsync()).Count());
        }
        finally
        {
            await DisposeAsync(tenantService, serviceProvider);
        }
    }

    [Fact]
    public async Task ActivateTenantsAsync_WithTheDefaultOptions_ActivatesOneTenantAtATime()
    {
        var tenants = CreateTenants(4);
        var current = 0;
        var peak = 0;
        var handler = new DelegateActivationHandler(async _ =>
        {
            var now = Interlocked.Increment(ref current);
            peak = Math.Max(peak, now);
            await Task.Delay(20);
            Interlocked.Decrement(ref current);
        });
        var (tenantService, serviceProvider) = await CreateTenantServiceAsync(tenants, activatedEvent: handler);

        try
        {
            await tenantService.ActivateTenantsAsync();

            Assert.Equal(1, new TenantActivationOptions().MaxDegreeOfParallelism);
            Assert.Equal(1, peak);
            Assert.Equal(tenants.Count, handler.Started.Count);
        }
        finally
        {
            await DisposeAsync(tenantService, serviceProvider);
        }
    }

    [Fact]
    public async Task ActivateTenantsAsync_WithUnboundedMaxDegreeOfParallelism_ActivatesAllTenantsAtOnce()
    {
        var tenants = CreateTenants(Environment.ProcessorCount * 2 + 2);
        var started = 0;
        var allStarted = NewGate();
        var release = NewGate();
        var handler = new DelegateActivationHandler(async args =>
        {
            if (Interlocked.Increment(ref started) == tenants.Count)
                allStarted.TrySetResult();

            await release.Task;
        });
        var (tenantService, serviceProvider) = await CreateTenantServiceAsync(tenants, activatedEvent: handler, maxDegreeOfParallelism: -1);

        try
        {
            var activation = tenantService.ActivateTenantsAsync();
            await allStarted.Task.WaitAsync(SafetyTimeout);
            release.SetResult();
            await activation.WaitAsync(SafetyTimeout);

            Assert.Equal(tenants.Count, (await tenantService.ListAsync()).Count());
        }
        finally
        {
            await DisposeAsync(tenantService, serviceProvider);
        }
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-2)]
    public void MaxDegreeOfParallelism_WhenInvalid_FailsValidation(int value)
    {
        using var serviceProvider = BuildOptionsServiceProvider(value);

        Assert.Throws<OptionsValidationException>(() => serviceProvider.GetRequiredService<IOptions<TenantActivationOptions>>().Value);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(1)]
    [InlineData(8)]
    public void MaxDegreeOfParallelism_WhenValid_PassesValidation(int value)
    {
        using var serviceProvider = BuildOptionsServiceProvider(value);

        Assert.Equal(value, serviceProvider.GetRequiredService<IOptions<TenantActivationOptions>>().Value.MaxDegreeOfParallelism);
    }

    [Fact]
    public async Task ActivateTenantsAsync_RaisesAllTenantsActivatedOnlyAfterEveryActivationCompleted()
    {
        var tenants = CreateTenants(6);
        var completed = 0;
        var completedWhenAllActivated = new ConcurrentQueue<int>();
        var handler = new DelegateActivationHandler(async _ =>
        {
            await Task.Yield();
            Interlocked.Increment(ref completed);
        });
        var allTenantsActivatedEvent = Substitute.For<IAllTenantsActivatedEvent>();
        allTenantsActivatedEvent.AllTenantsActivatedAsync(Arg.Any<CancellationToken>()).Returns(_ =>
        {
            completedWhenAllActivated.Enqueue(Volatile.Read(ref completed));
            return Task.CompletedTask;
        });
        var (tenantService, serviceProvider) = await CreateTenantServiceAsync(tenants, activatedEvent: handler, allTenantsActivatedEvent: allTenantsActivatedEvent);

        try
        {
            await tenantService.ActivateTenantsAsync();

            Assert.Equal(tenants.Count, Assert.Single(completedWhenAllActivated));
        }
        finally
        {
            await DisposeAsync(tenantService, serviceProvider);
        }
    }

    [Fact]
    public async Task ListAsync_WhenActivationFailsInParallelPhase_RollsBackAndRetriesFromScratch()
    {
        var tenants = CreateTenants(3);
        var scenario = new FailingAfterOtherStarted(tenants[1], tenants[2]);
        var handler = new DelegateActivationHandler(scenario.HandleAsync);
        var deactivated = new RecordingDeactivatedHandler();
        var (tenantService, serviceProvider) = await CreateTenantServiceAsync(tenants, activatedEvent: handler, deactivatedEvent: deactivated, maxDegreeOfParallelism: 2);
        var scopeFactory = serviceProvider.GetRequiredService<RecordingTenantScopeFactory>();

        try
        {
            await Assert.ThrowsAsync<InvalidOperationException>(() => tenantService.ListAsync());

            Assert.True(scenario.WaitingCompleted.Task.IsCompleted);
            Assert.Equal(tenants.Take(3).Select(x => x.Id).Order(), handler.Started.Order());
            Assert.Equal([tenants[0].Id, tenants[2].Id], deactivated.Deactivated.Order());
            var firstAttemptScopes = scopeFactory.Created;
            Assert.Equal(tenants.Count, firstAttemptScopes.Count);
            Assert.All(firstAttemptScopes.Where(x => x.Tenant != tenants[0] && x.Tenant != tenants[2]), x => Assert.True(RecordingTenantScopeFactory.IsDisposed(x.Scope)));

            scenario.Enabled = false;
            var listed = (await tenantService.ListAsync()).ToList();

            Assert.Equal(tenants.Count, listed.Count);
            Assert.Equal(2, deactivated.Deactivated.Count);
            Assert.Equal(tenants.Count * 2, scopeFactory.Created.Count);
            Assert.All(scopeFactory.Created.Skip(tenants.Count), x => Assert.False(RecordingTenantScopeFactory.IsDisposed(x.Scope)));
        }
        finally
        {
            await DisposeAsync(tenantService, serviceProvider);
        }
    }

    [Fact]
    public async Task RefreshAsync_WhenActivationFailsInParallelPhase_KeepsActivatedTenantsAndRetriesTheRest()
    {
        var tenants = CreateTenants(4);
        var providerReturns = new List<Tenant> { tenants[0] };
        var scenario = new FailingAfterOtherStarted(tenants[2], tenants[3]) { Enabled = false };
        var handler = new DelegateActivationHandler(scenario.HandleAsync);
        var deactivated = new RecordingDeactivatedHandler();
        var allTenantsActivatedEvent = Substitute.For<IAllTenantsActivatedEvent>();
        var (tenantService, serviceProvider) = await CreateTenantServiceAsync(providerReturns, () => providerReturns, activatedEvent: handler, deactivatedEvent: deactivated, allTenantsActivatedEvent: allTenantsActivatedEvent, maxDegreeOfParallelism: 2);
        var scopeFactory = serviceProvider.GetRequiredService<RecordingTenantScopeFactory>();

        try
        {
            await tenantService.ActivateTenantsAsync();
            allTenantsActivatedEvent.ClearReceivedCalls();
            providerReturns.AddRange(tenants.Skip(1));
            scenario.Enabled = true;

            await Assert.ThrowsAsync<InvalidOperationException>(() => tenantService.ActivateTenantsAsync());

            Assert.True(scenario.WaitingCompleted.Task.IsCompleted);
            await allTenantsActivatedEvent.DidNotReceive().AllTenantsActivatedAsync(Arg.Any<CancellationToken>());
            Assert.Equal([tenants[0].Id, tenants[1].Id, tenants[3].Id], (await tenantService.ListAsync()).Select(x => x.Id).Order());
            Assert.Empty(deactivated.Deactivated);
            Assert.All(scopeFactory.Created.Where(x => x.Tenant == tenants[2]), x => Assert.True(RecordingTenantScopeFactory.IsDisposed(x.Scope)));
            Assert.False(RecordingTenantScopeFactory.IsDisposed(scopeFactory.Created.Single(x => x.Tenant == tenants[3]).Scope));

            scenario.Enabled = false;
            await tenantService.RefreshAsync();

            Assert.Equal(tenants.Count, (await tenantService.ListAsync()).Count());
        }
        finally
        {
            await DisposeAsync(tenantService, serviceProvider);
        }
    }

    [Fact]
    public async Task ListAsync_WhenActivationFails_StartsNoFurtherTenant()
    {
        var tenants = CreateTenants(5);
        var fail = true;
        var handler = new DelegateActivationHandler(async args =>
        {
            await Task.Yield();

            if (fail && args.Tenant == tenants[1])
                throw new InvalidOperationException("Migration failed");
        });
        var deactivated = new RecordingDeactivatedHandler();
        var (tenantService, serviceProvider) = await CreateTenantServiceAsync(tenants, activatedEvent: handler, deactivatedEvent: deactivated, maxDegreeOfParallelism: 1);
        var scopeFactory = serviceProvider.GetRequiredService<RecordingTenantScopeFactory>();

        try
        {
            await Assert.ThrowsAsync<InvalidOperationException>(() => tenantService.ListAsync());

            Assert.Equal([tenants[0].Id, tenants[1].Id], handler.Started);
            Assert.Equal([tenants[0].Id], deactivated.Deactivated);
            Assert.All(scopeFactory.Created.Skip(1), x => Assert.True(RecordingTenantScopeFactory.IsDisposed(x.Scope)));

            fail = false;

            Assert.Equal(tenants.Count, (await tenantService.ListAsync()).Count());
        }
        finally
        {
            await DisposeAsync(tenantService, serviceProvider);
        }
    }

    [Fact]
    public async Task RefreshAsync_WhenActivationFails_StartsNoFurtherTenantAndRetriesThem()
    {
        var tenants = CreateTenants(5);
        var providerReturns = new List<Tenant> { tenants[0] };
        var fail = false;
        var handler = new DelegateActivationHandler(async args =>
        {
            await Task.Yield();

            if (fail && args.Tenant == tenants[2])
                throw new InvalidOperationException("Migration failed");
        });
        var (tenantService, serviceProvider) = await CreateTenantServiceAsync(providerReturns, () => providerReturns, activatedEvent: handler, maxDegreeOfParallelism: 1);
        var scopeFactory = serviceProvider.GetRequiredService<RecordingTenantScopeFactory>();

        try
        {
            await tenantService.ActivateTenantsAsync();
            providerReturns.AddRange(tenants.Skip(1));
            fail = true;

            await Assert.ThrowsAsync<InvalidOperationException>(() => tenantService.RefreshAsync());

            Assert.Equal(tenants.Take(3).Select(x => x.Id), handler.Started);
            Assert.Equal(tenants.Take(2).Select(x => x.Id), (await tenantService.ListAsync()).Select(x => x.Id).Order());
            Assert.All(scopeFactory.Created.Skip(2), x => Assert.True(RecordingTenantScopeFactory.IsDisposed(x.Scope)));

            fail = false;
            await tenantService.RefreshAsync();

            Assert.Equal(tenants.Count, (await tenantService.ListAsync()).Count());
        }
        finally
        {
            await DisposeAsync(tenantService, serviceProvider);
        }
    }

    [Fact]
    public async Task ListAsync_WhenActivationFailsInParallelPhase_DoesNotCancelInFlightSibling()
    {
        var tenants = CreateTenants(3);
        var scenario = new FailingAfterOtherStarted(tenants[1], tenants[2]);
        var handler = new DelegateActivationHandler(scenario.HandleAsync);
        var deactivated = new RecordingDeactivatedHandler();
        var (tenantService, serviceProvider) = await CreateTenantServiceAsync(tenants, activatedEvent: handler, deactivatedEvent: deactivated, maxDegreeOfParallelism: 2);

        try
        {
            await Assert.ThrowsAsync<InvalidOperationException>(() => tenantService.ListAsync());

            Assert.True(scenario.WaitingCompleted.Task.IsCompleted);
            Assert.False(scenario.WaitingWasCancelled);
            Assert.Contains(tenants[2].Id, deactivated.Deactivated);
        }
        finally
        {
            await DisposeAsync(tenantService, serviceProvider);
        }
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-2)]
    public async Task ActivateTenantsAsync_WithInvalidMaxDegreeOfParallelism_FailsBeforeAnyActivation(int value)
    {
        var handler = new DelegateActivationHandler(_ => Task.CompletedTask);
        var (tenantService, serviceProvider) = await CreateTenantServiceAsync(CreateTenants(3), activatedEvent: handler, maxDegreeOfParallelism: value);

        try
        {
            await Assert.ThrowsAsync<OptionsValidationException>(() => tenantService.ActivateTenantsAsync());

            Assert.Empty(handler.Started);
        }
        finally
        {
            await DisposeAsync(tenantService, serviceProvider);
        }
    }

    [Fact]
    public async Task ActivateTenantsAsync_WhileInitializationIsRunning_WaitsForInitialization()
    {
        var tenants = CreateTenants(2);
        var secondStarted = NewGate();
        var gate = NewGate();
        var handler = new DelegateActivationHandler(async args =>
        {
            if (args.Tenant != tenants[1])
                return;

            secondStarted.TrySetResult();
            await gate.Task;
        });
        var allTenantsActivatedEvent = Substitute.For<IAllTenantsActivatedEvent>();
        var (tenantService, serviceProvider) = await CreateTenantServiceAsync(tenants, activatedEvent: handler, allTenantsActivatedEvent: allTenantsActivatedEvent);

        try
        {
            var listing = tenantService.ListAsync();
            await secondStarted.Task.WaitAsync(SafetyTimeout);

            var activation = tenantService.ActivateTenantsAsync();

            Assert.False(activation.IsCompleted);
            await allTenantsActivatedEvent.DidNotReceive().AllTenantsActivatedAsync(Arg.Any<CancellationToken>());

            gate.SetResult();
            await Task.WhenAll(listing, activation).WaitAsync(SafetyTimeout);

            await allTenantsActivatedEvent.Received(1).AllTenantsActivatedAsync(Arg.Any<CancellationToken>());
        }
        finally
        {
            await DisposeAsync(tenantService, serviceProvider);
        }
    }

    [Fact]
    public async Task DeactivateTenantsAsync_WhileInitializationIsRunning_WaitsForInitialization()
    {
        var tenants = CreateTenants(2);
        var secondStarted = NewGate();
        var gate = NewGate();
        var handler = new DelegateActivationHandler(async args =>
        {
            if (args.Tenant != tenants[1])
                return;

            secondStarted.TrySetResult();
            await gate.Task;
        });
        var deactivated = new RecordingDeactivatedHandler();
        var (tenantService, serviceProvider) = await CreateTenantServiceAsync(tenants, activatedEvent: handler, deactivatedEvent: deactivated);

        try
        {
            var listing = tenantService.ListAsync();
            await secondStarted.Task.WaitAsync(SafetyTimeout);

            var deactivation = tenantService.DeactivateTenantsAsync();

            Assert.False(deactivation.IsCompleted);
            Assert.Empty(deactivated.Deactivated);

            gate.SetResult();
            await Task.WhenAll(listing, deactivation).WaitAsync(SafetyTimeout);

            Assert.Equal(tenants.Select(x => x.Id), deactivated.Deactivated.Order());
        }
        finally
        {
            await DisposeAsync(tenantService, serviceProvider);
        }
    }

    [Fact]
    public async Task ListAsync_WhenCallerCancelsDuringParallelPhase_RollsBackAndRetries()
    {
        var tenants = CreateTenants(6);
        using var cancellation = new CancellationTokenSource();
        var block = true;
        var started = 0;
        var bothStarted = NewGate();
        var handler = new DelegateActivationHandler(async args =>
        {
            if (!block || args.Tenant == tenants[0])
                return;

            if (Interlocked.Increment(ref started) == 2)
                bothStarted.TrySetResult();

            await Task.Delay(Timeout.Infinite, args.CancellationToken);
        });
        var deactivated = new RecordingDeactivatedHandler();
        var (tenantService, serviceProvider) = await CreateTenantServiceAsync(tenants, activatedEvent: handler, deactivatedEvent: deactivated, maxDegreeOfParallelism: 2);
        var scopeFactory = serviceProvider.GetRequiredService<RecordingTenantScopeFactory>();

        try
        {
            var listing = tenantService.ListAsync(cancellation.Token);
            await bothStarted.Task.WaitAsync(SafetyTimeout);
            await cancellation.CancelAsync();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => listing);

            Assert.Equal([tenants[0].Id], deactivated.Deactivated);
            Assert.All(scopeFactory.Created.Skip(1), x => Assert.True(RecordingTenantScopeFactory.IsDisposed(x.Scope)));

            block = false;

            Assert.Equal(tenants.Count, (await tenantService.ListAsync()).Count());
            Assert.Equal(tenants.Count * 2, scopeFactory.Created.Count);
        }
        finally
        {
            await DisposeAsync(tenantService, serviceProvider);
        }
    }

    [Fact]
    public async Task DeactivateTenantsAsync_WhileRefreshIsActivating_WaitsForTheRefresh()
    {
        var tenants = CreateTenants(2);
        var providerReturns = new List<Tenant> { tenants[0] };
        var secondStarted = NewGate();
        var gate = NewGate();
        var handler = new DelegateActivationHandler(async args =>
        {
            if (args.Tenant != tenants[1])
                return;

            secondStarted.TrySetResult();
            await gate.Task;
        });
        var deactivated = new RecordingDeactivatedHandler();
        var (tenantService, serviceProvider) = await CreateTenantServiceAsync(providerReturns, () => providerReturns, activatedEvent: handler, deactivatedEvent: deactivated);

        try
        {
            await tenantService.ActivateTenantsAsync();
            providerReturns.Add(tenants[1]);
            var refresh = tenantService.RefreshAsync();
            await secondStarted.Task.WaitAsync(SafetyTimeout);

            var deactivate = tenantService.DeactivateTenantsAsync();

            Assert.False(deactivate.IsCompleted);
            Assert.Empty(deactivated.Deactivated);

            gate.SetResult();
            await Task.WhenAll(refresh, deactivate).WaitAsync(SafetyTimeout);

            Assert.Equal(tenants.Select(x => x.Id), deactivated.Deactivated.Order());
        }
        finally
        {
            await DisposeAsync(tenantService, serviceProvider);
        }
    }

    [Fact]
    public async Task RefreshAsync_RunningConcurrentlyWithDeactivateTenantsAsync_DoesNotThrow()
    {
        var tenants = CreateTenants(5);
        var calls = 0;
        var handler = new DelegateActivationHandler(async _ => await Task.Yield());
        var (tenantService, serviceProvider) = await CreateTenantServiceAsync(
            tenants,
            () => tenants.Take(Interlocked.Increment(ref calls) % 2 == 0 ? 3 : 5).ToList(),
            activatedEvent: handler);

        try
        {
            var refreshing = Task.Run(async () =>
            {
                for (var i = 0; i < 50; i++)
                    await tenantService.RefreshAsync();
            });
            var deactivating = Task.Run(async () =>
            {
                for (var i = 0; i < 50; i++)
                    await tenantService.DeactivateTenantsAsync();
            });

            await Task.WhenAll(refreshing, deactivating).WaitAsync(TimeSpan.FromSeconds(30));
        }
        finally
        {
            await DisposeAsync(tenantService, serviceProvider);
        }
    }

    [Fact]
    public async Task ActivateTenantsAsync_LookupFromInsideActivation_SeesOnlyTenantsWhoseActivationStarted()
    {
        var tenants = CreateTenants(3);
        var ownLookups = new ConcurrentDictionary<string, Tenant?>();
        var laterLookups = new ConcurrentDictionary<string, Tenant?>();
        ITenantService? service = null;
        var handler = new DelegateActivationHandler(async args =>
        {
            ownLookups[args.Tenant.Id!] = await service!.FindAsync(args.Tenant.Id!);

            if (args.Tenant == tenants[0])
                laterLookups[args.Tenant.Id!] = await service.FindAsync(tenants[^1].Id);
        });
        var (tenantService, serviceProvider) = await CreateTenantServiceAsync(tenants, activatedEvent: handler, maxDegreeOfParallelism: 1);
        service = tenantService;

        try
        {
            await tenantService.ActivateTenantsAsync().WaitAsync(SafetyTimeout);

            Assert.All(tenants, tenant => Assert.Same(tenant, ownLookups[tenant.Id!]));
            Assert.Null(laterLookups[tenants[0].Id!]);
            Assert.Same(tenants[^1], await tenantService.FindAsync(tenants[^1].Id));
        }
        finally
        {
            await DisposeAsync(tenantService, serviceProvider);
        }
    }

    [Fact]
    public async Task RefreshAndDeactivate_WhenLookupAlreadySeesPublishedDictionary_WaitForInitialization()
    {
        var tenants = CreateTenants(2);
        var secondStarted = NewGate();
        var gate = NewGate();
        var handler = new DelegateActivationHandler(async args =>
        {
            if (args.Tenant != tenants[1])
                return;

            secondStarted.TrySetResult();
            await gate.Task;
        });
        var deactivated = new RecordingDeactivatedHandler();
        var allTenantsActivatedEvent = Substitute.For<IAllTenantsActivatedEvent>();
        var (tenantService, serviceProvider) = await CreateTenantServiceAsync(tenants, activatedEvent: handler, deactivatedEvent: deactivated, allTenantsActivatedEvent: allTenantsActivatedEvent);

        try
        {
            var initialization = tenantService.ListAsync();
            await secondStarted.Task.WaitAsync(SafetyTimeout);

            Assert.Same(tenants[0], await tenantService.FindAsync(tenants[0].Id).WaitAsync(SafetyTimeout));

            var activation = tenantService.ActivateTenantsAsync();
            var deactivation = tenantService.DeactivateTenantsAsync();

            await Task.Delay(100);
            Assert.False(activation.IsCompleted);
            Assert.False(deactivation.IsCompleted);
            Assert.Empty(deactivated.Deactivated);
            await allTenantsActivatedEvent.DidNotReceive().AllTenantsActivatedAsync(Arg.Any<CancellationToken>());

            gate.SetResult();
            await Task.WhenAll(initialization, activation, deactivation).WaitAsync(SafetyTimeout);

            await allTenantsActivatedEvent.Received(1).AllTenantsActivatedAsync(Arg.Any<CancellationToken>());
        }
        finally
        {
            await DisposeAsync(tenantService, serviceProvider);
        }
    }

    private static readonly TimeSpan SafetyTimeout = TimeSpan.FromSeconds(10);

    private static TaskCompletionSource NewGate() => new(TaskCreationOptions.RunContinuationsAsynchronously);

    private static List<Tenant> CreateTenants(int count) =>
        Enumerable.Range(0, count).Select(i => new Tenant { Id = $"tenant-{i}", Name = $"Tenant {i}" }).ToList();

    private static async Task DisposeAsync(ITenantService tenantService, ServiceProvider serviceProvider)
    {
        if (tenantService is IAsyncDisposable disposable)
            await disposable.DisposeAsync();
        await serviceProvider.DisposeAsync();
    }

    private static ServiceProvider BuildOptionsServiceProvider(int maxDegreeOfParallelism)
    {
        var services = new ServiceCollection();
        new ShellFeatures.MultitenancyFeature().ConfigureServices(services);
        services.Configure<TenantActivationOptions>(x => x.MaxDegreeOfParallelism = maxDegreeOfParallelism);
        return services.BuildServiceProvider();
    }

    private static Task<(ITenantService TenantService, ServiceProvider ServiceProvider)> CreateTenantServiceAsync(
        IEnumerable<Tenant> tenants,
        Func<List<Tenant>>? tenantsFactory = null,
        ITenantActivatedEvent? activatedEvent = null,
        ITenantDeactivatedEvent? deactivatedEvent = null,
        IAllTenantsActivatedEvent? allTenantsActivatedEvent = null,
        int? maxDegreeOfParallelism = null)
    {
        var tenantList = tenants.ToList();
        var getTenants = tenantsFactory ?? (() => tenantList);

        var tenantsProvider = Substitute.For<ITenantsProvider>();
        tenantsProvider.ListAsync(Arg.Any<CancellationToken>()).Returns(_ => getTenants());

        var services = new ServiceCollection();
        services.AddSingleton(_ => tenantsProvider);
        services.AddSingleton<RecordingTenantScopeFactory>();
        services.AddSingleton<ITenantScopeFactory>(sp => sp.GetRequiredService<RecordingTenantScopeFactory>());
        services.AddSingleton<ITenantAccessor, DefaultTenantAccessor>();
        services.AddSingleton(activatedEvent ?? Substitute.For<ITenantActivatedEvent>());
        services.AddSingleton(deactivatedEvent ?? Substitute.For<ITenantDeactivatedEvent>());
        services.AddSingleton<ITenantDeletedEvent>(Substitute.For<ITenantDeletedEvent>());
        services.AddSingleton(allTenantsActivatedEvent ?? Substitute.For<IAllTenantsActivatedEvent>());
        services.AddSingleton<TenantEventsManager>();
        services.AddSingleton<RecurringTaskScheduleManager>();
        services.AddSingleton<ITenantService, DefaultTenantService>();
        services.AddLogging();
        services.AddOptions<TenantActivationOptions>().Services.TryAddEnumerable(ServiceDescriptor.Singleton<IValidateOptions<TenantActivationOptions>, TenantActivationOptionsValidator>());

        if (maxDegreeOfParallelism != null)
            services.Configure<TenantActivationOptions>(x => x.MaxDegreeOfParallelism = maxDegreeOfParallelism.Value);

        var serviceProvider = services.BuildServiceProvider();
        return Task.FromResult((serviceProvider.GetRequiredService<ITenantService>(), serviceProvider));
    }

    private sealed class DelegateActivationHandler(Func<TenantActivatedEventArgs, Task> handler) : ITenantActivatedEvent
    {
        public ConcurrentQueue<string> Started { get; } = new();

        public Task TenantActivatedAsync(TenantActivatedEventArgs args)
        {
            Started.Enqueue(args.Tenant.Id!);
            return handler(args);
        }
    }

    private sealed class RecordingDeactivatedHandler : ITenantDeactivatedEvent
    {
        public ConcurrentQueue<string> Deactivated { get; } = new();

        public Task TenantDeactivatedAsync(TenantDeactivatedEventArgs args)
        {
            Deactivated.Enqueue(args.Tenant.Id!);
            return Task.CompletedTask;
        }
    }

    private sealed class FailingAfterOtherStarted(Tenant failing, Tenant waiting)
    {
        private readonly TaskCompletionSource _waitingStarted = NewGate();

        private readonly TaskCompletionSource _failed = NewGate();

        public bool Enabled { get; set; } = true;
        public bool WaitingWasCancelled { get; private set; }
        public TaskCompletionSource WaitingCompleted { get; } = NewGate();

        public async Task HandleAsync(TenantActivatedEventArgs args)
        {
            if (!Enabled)
                return;

            if (args.Tenant == failing)
            {
                await _waitingStarted.Task.WaitAsync(SafetyTimeout);
                _failed.SetResult();
                throw new InvalidOperationException("Migration failed");
            }

            if (args.Tenant == waiting)
            {
                _waitingStarted.TrySetResult();
                await _failed.Task.WaitAsync(SafetyTimeout);
                await Task.Yield();
                WaitingWasCancelled = args.CancellationToken.IsCancellationRequested;
                WaitingCompleted.TrySetResult();
            }
        }
    }

    private sealed class RecordingTenantScopeFactory(ITenantAccessor tenantAccessor, IServiceScopeFactory serviceScopeFactory) : ITenantScopeFactory
    {
        private readonly ConcurrentQueue<(Tenant Tenant, TenantScope Scope)> _created = new();

        public IReadOnlyList<(Tenant Tenant, TenantScope Scope)> Created => _created.ToArray();

        public TenantScope CreateScope(Tenant? tenant)
        {
            var scope = new DefaultTenantScopeFactory(tenantAccessor, serviceScopeFactory).CreateScope(tenant);
            _created.Enqueue((tenant!, scope));
            return scope;
        }

        public static bool IsDisposed(TenantScope scope)
        {
            try
            {
                scope.ServiceProvider.GetService<ITenantAccessor>();
                return false;
            }
            catch (ObjectDisposedException)
            {
                return true;
            }
        }
    }
}
