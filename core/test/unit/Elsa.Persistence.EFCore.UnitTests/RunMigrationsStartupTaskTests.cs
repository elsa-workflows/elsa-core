using System.Collections.Concurrent;
using System.Data.Common;
using Elsa.Common;
using Elsa.Common.DistributedHosting.DistributedLocks;
using Elsa.Extensions;
using Medallion.Threading;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Persistence.EFCore.UnitTests;

public class RunMigrationsStartupTaskTests
{
    private const string DatabaseA = "Data Source=a;Mode=Memory";
    private const string DatabaseB = "Data Source=b;Mode=Memory";

    private readonly MigratedDatabaseRegistry _registry = new();
    private readonly ConnectionCounter _connections = new();

    [Fact]
    public async Task ExecuteAsync_ForTheSameDatabase_MigratesOnce()
    {
        await RunAsync<FirstDbContext>(DatabaseA);
        var opened = _connections.Opened;

        await RunAsync<FirstDbContext>(DatabaseA);

        Assert.True(opened > 0);
        Assert.Equal(opened, _connections.Opened);
    }

    [Fact]
    public async Task ExecuteAsync_ForADifferentDatabase_MigratesAgain()
    {
        await RunAsync<FirstDbContext>(DatabaseA);
        var opened = _connections.Opened;

        await RunAsync<FirstDbContext>(DatabaseB);

        Assert.True(_connections.Opened > opened);
    }

    [Fact]
    public async Task ExecuteAsync_ForADifferentDbContextOnTheSameDatabase_MigratesAgain()
    {
        await RunAsync<FirstDbContext>(DatabaseA);
        var opened = _connections.Opened;

        await RunAsync<SecondDbContext>(DatabaseA);

        Assert.True(_connections.Opened > opened);
    }

    [Fact]
    public async Task ExecuteAsync_AfterAFailedMigration_MigratesAgain()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(() => RunAsync<FirstDbContext>(DatabaseA, new FailingConnectionInterceptor()));
        var opened = _connections.Opened;

        await RunAsync<FirstDbContext>(DatabaseA);

        Assert.True(_connections.Opened > opened);
    }

    [Fact]
    public async Task ExecuteAsync_WithoutARegisteredRegistry_MigratesEveryTime()
    {
        await RunAsync<FirstDbContext>(DatabaseA, registerRegistry: false);
        var opened = _connections.Opened;

        await RunAsync<FirstDbContext>(DatabaseA, registerRegistry: false);

        Assert.True(opened > 0);
        Assert.Equal(opened * 2, _connections.Opened);
    }

    [Fact]
    public async Task ExecuteAsync_ForTheSameDatabaseAtTheSameTime_MigratesOnce()
    {
        await RunAsync<FirstDbContext>(DatabaseB);
        var openedByOneMigration = _connections.Opened;

        var tasks = Enumerable.Range(0, 8).Select(_ => RunAsync<FirstDbContext>(DatabaseA)).ToArray();
        await Task.WhenAll(tasks);

        Assert.True(openedByOneMigration > 0);
        Assert.Equal(openedByOneMigration * 2, _connections.Opened);
    }

    [Fact]
    public async Task ExecuteAsync_TakesADistributedLockPerDatabaseWithoutExposingTheConnectionString()
    {
        var locks = new RecordingLockProvider();

        await RunAsync<FirstDbContext>(DatabaseA, lockProvider: locks);
        await RunAsync<FirstDbContext>(DatabaseB, lockProvider: locks);
        await RunAsync<SecondDbContext>(DatabaseB, lockProvider: locks);

        Assert.Equal(3, locks.Names.Distinct().Count());
        Assert.All(locks.Names, name => Assert.DoesNotContain("Data Source", name));
    }

    private async Task RunAsync<TDbContext>(string connectionString, IInterceptor? interceptor = null, bool registerRegistry = true, IDistributedLockProvider? lockProvider = null) where TDbContext : DbContext
    {
        var services = new ServiceCollection();
        services.AddDbContextFactory<TDbContext>(options =>
        {
            options.UseSqlite(connectionString).AddInterceptors(_connections);

            if (interceptor != null)
                options.AddInterceptors(interceptor);
        });
        services.Configure<MigrationOptions>(options => options.RunMigrations[typeof(TDbContext)] = true);
        services.AddStartupTask<RunMigrationsStartupTask<TDbContext>>();

        if (registerRegistry)
            services.AddSingleton(_registry);

        if (lockProvider != null)
            services.AddSingleton(lockProvider);

        await using var serviceProvider = services.BuildServiceProvider();
        await using var scope = serviceProvider.CreateAsyncScope();
        var task = Assert.Single(scope.ServiceProvider.GetServices<IStartupTask>());
        await task.ExecuteAsync(CancellationToken.None);
    }

    private class RecordingLockProvider : IDistributedLockProvider
    {
        private readonly ConcurrentQueue<string> _names = new();

        public IReadOnlyCollection<string> Names => _names.ToArray();

        public IDistributedLock CreateLock(string name)
        {
            _names.Enqueue(name);
            return new NoopDistributedSynchronizationProvider().CreateLock(name);
        }
    }

    private class FirstDbContext(DbContextOptions<FirstDbContext> options) : DbContext(options);

    private class SecondDbContext(DbContextOptions<SecondDbContext> options) : DbContext(options);

    private class ConnectionCounter : DbConnectionInterceptor
    {
        private int _opened;

        public int Opened => _opened;

        public override void ConnectionOpened(DbConnection connection, ConnectionEndEventData eventData) => Interlocked.Increment(ref _opened);

        public override Task ConnectionOpenedAsync(DbConnection connection, ConnectionEndEventData eventData, CancellationToken cancellationToken = default)
        {
            Interlocked.Increment(ref _opened);
            return Task.CompletedTask;
        }
    }

    private class FailingConnectionInterceptor : DbConnectionInterceptor
    {
        public override InterceptionResult ConnectionOpening(DbConnection connection, ConnectionEventData eventData, InterceptionResult result) =>
            throw new InvalidOperationException("Migration failed.");

        public override ValueTask<InterceptionResult> ConnectionOpeningAsync(DbConnection connection, ConnectionEventData eventData, InterceptionResult result, CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("Migration failed.");
    }
}
