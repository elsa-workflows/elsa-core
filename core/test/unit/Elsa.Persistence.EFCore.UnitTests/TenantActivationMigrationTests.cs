using System.Collections.Concurrent;
using System.Data.Common;
using Elsa.Common;
using Elsa.Common.Multitenancy;
using Elsa.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Elsa.Persistence.EFCore.UnitTests;

public class TenantActivationMigrationTests
{
    private const string OwnDatabase = "Data Source=own-tenant;Mode=Memory";
    private const string SharedDatabase = "Data Source=shared-tenants;Mode=Memory";
    private static readonly TimeSpan SafetyTimeout = TimeSpan.FromSeconds(10);

    [Fact]
    public async Task ActivateTenantsAsync_WithAnOwnDatabaseTenantListedFirst_SharedDatabaseTenantsWaitForTheSingleMigration()
    {
        var tenants = Enumerable.Range(0, 4).Select(i => new Tenant { Id = $"tenant-{i}", Name = $"Tenant {i}" }).ToList();
        var connectionStrings = tenants.ToDictionary(x => x.Id!, x => x == tenants[0] ? OwnDatabase : SharedDatabase);
        var releaseSharedMigration = NewGate();
        var connections = new ConnectionCounter(releaseSharedMigration.Task);
        var runner = new StartupTaskRunner();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<ITenantAccessor, DefaultTenantAccessor>();
        services.AddSingleton<ITenantScopeFactory, DefaultTenantScopeFactory>();
        services.AddSingleton<TenantEventsManager>();
        services.AddSingleton<ITenantActivatedEvent>(runner);
        services.AddSingleton<ITenantsProvider>(new FixedTenantsProvider(tenants));
        services.AddSingleton<ITenantService, DefaultTenantService>();
        services.Configure<TenantActivationOptions>(options => options.MaxDegreeOfParallelism = tenants.Count);
        services.AddSingleton<MigratedDatabaseRegistry>();
        services.Configure<MigrationOptions>(options => options.RunMigrations[typeof(TestDbContext)] = true);
        services.AddDbContextFactory<TestDbContext>((serviceProvider, options) =>
        {
            var tenantId = serviceProvider.GetRequiredService<ITenantAccessor>().Tenant!.Id!;
            options.UseSqlite(connectionStrings[tenantId]).AddInterceptors(connections);
        }, ServiceLifetime.Scoped);
        services.AddStartupTask<RunMigrationsStartupTask<TestDbContext>>();

        await using var serviceProvider = services.BuildServiceProvider();
        var tenantService = serviceProvider.GetRequiredService<ITenantService>();

        var activation = tenantService.ActivateTenantsAsync();
        await connections.SharedMigrationBlocked.WaitAsync(SafetyTimeout);
        await runner.AllReached(tenants.Count).WaitAsync(SafetyTimeout);
        await Task.Delay(200);

        // The own-database tenant is done, and no shared-database tenant moved on while the shared migration is still running.
        Assert.Equal([tenants[0].Id], runner.Completed.Order());

        releaseSharedMigration.SetResult();
        await activation.WaitAsync(SafetyTimeout);

        Assert.Equal(tenants.Count, runner.Completed.Count);
        var ownConnections = connections.OpenedFor("own-tenant");
        Assert.True(ownConnections > 0);
        Assert.Equal(ownConnections, connections.OpenedFor("shared-tenants"));
    }

    private static TaskCompletionSource NewGate() => new(TaskCreationOptions.RunContinuationsAsynchronously);

    private class TestDbContext(DbContextOptions<TestDbContext> options) : DbContext(options);

    private sealed class FixedTenantsProvider(IReadOnlyList<Tenant> tenants) : ITenantsProvider
    {
        public Task<IEnumerable<Tenant>> ListAsync(CancellationToken cancellationToken = default) => Task.FromResult<IEnumerable<Tenant>>(tenants);

        public Task<Tenant?> FindAsync(TenantFilter filter, CancellationToken cancellationToken = default) => Task.FromResult(filter.Apply(tenants.AsQueryable()).FirstOrDefault());
    }

    private sealed class StartupTaskRunner : ITenantActivatedEvent
    {
        private readonly ConcurrentQueue<string> _reached = new();
        private readonly ConcurrentQueue<string> _completed = new();

        public IReadOnlyCollection<string> Completed => _completed.ToArray();

        public async Task AllReached(int count)
        {
            while (_reached.Count < count)
                await Task.Delay(10);
        }

        public async Task TenantActivatedAsync(TenantActivatedEventArgs args)
        {
            _reached.Enqueue(args.Tenant.Id!);

            var startupTask = args.TenantScope.ServiceProvider.GetServices<IStartupTask>().Single();
            await startupTask.ExecuteAsync(args.CancellationToken);

            _completed.Enqueue(args.Tenant.Id!);
        }
    }

    private sealed class ConnectionCounter(Task releaseSharedMigration) : DbConnectionInterceptor
    {
        private readonly ConcurrentDictionary<string, int> _opened = new();
        private readonly TaskCompletionSource _sharedMigrationBlocked = NewGate();

        public Task SharedMigrationBlocked => _sharedMigrationBlocked.Task;

        public int OpenedFor(string database) => _opened.Where(x => x.Key.Contains(database)).Sum(x => x.Value);

        public override async Task ConnectionOpenedAsync(DbConnection connection, ConnectionEndEventData eventData, CancellationToken cancellationToken = default)
        {
            _opened.AddOrUpdate(connection.ConnectionString, 1, (_, count) => count + 1);

            if (!connection.ConnectionString.Contains("shared-tenants"))
                return;

            // Holds the shared migration open so that tenants which skip it instead of waiting would be seen completing early.
            _sharedMigrationBlocked.TrySetResult();
            await releaseSharedMigration.WaitAsync(cancellationToken);
        }
    }
}
