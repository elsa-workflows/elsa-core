using System.Data.Common;
using Elsa.Common.Entities;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Elsa.Persistence.EFCore.UnitTests;

public class StoreWriteLockTests
{
    [Fact]
    public async Task AcquireById_DoesNotQueryTheDatabase()
    {
        await using var harness = await Harness.CreateAsync();
        var commands = 0;
        harness.OnCommand = () => Interlocked.Increment(ref commands);

        using var held = await harness.AcquireAsync("Order-A");

        Assert.Equal(0, commands);
    }

    [Fact]
    public async Task DistinctEligibleIds_ProceedConcurrently()
    {
        await using var harness = await Harness.CreateAsync();
        using var first = await harness.AcquireAsync("Order-A");
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        using var independent = await harness.AcquireAsync("Order-B", timeout.Token);
    }

    [Fact]
    public async Task FoldedIdVariants_ShareAReservation()
    {
        await using var harness = await Harness.CreateAsync();
        using var first = await harness.AcquireAsync("Order-A");
        foreach (var overlapping in new[] { "order-a", "Order_A", "ORDER-A" })
        {
            using var cancel = new CancellationTokenSource(TimeSpan.FromMilliseconds(500));
            await Assert.ThrowsAnyAsync<OperationCanceledException>(
                () => harness.AcquireAsync(overlapping, cancel.Token).AsTask());
        }
    }

    [Fact]
    public async Task IneligibleId_TakesExclusiveAccess()
    {
        await using var harness = await Harness.CreateAsync();
        using var first = await harness.AcquireAsync("Order-A");
        using var cancel = new CancellationTokenSource(TimeSpan.FromMilliseconds(500));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => harness.AcquireAsync("Order A", cancel.Token).AsTask());
    }

    [Fact]
    public async Task ExclusiveReservation_BlocksIndependentIds()
    {
        await using var harness = await Harness.CreateAsync();
        using var first = await harness.AcquireAsync("Order A");
        using var cancel = new CancellationTokenSource(TimeSpan.FromMilliseconds(500));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => harness.AcquireAsync("Order-B", cancel.Token).AsTask());
    }

    [Fact]
    public async Task BatchReservesAllKeysTogether()
    {
        await using var harness = await Harness.CreateAsync();
        using var first = await harness.AcquireAsync("Order-A");
        using var cancel = new CancellationTokenSource(TimeSpan.FromMilliseconds(500));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => harness.AcquireAsync(["Unused-B", "Order-A"], cancel.Token).AsTask());
    }

    [Fact]
    public async Task CancelingAWaitingBatch_ReleasesUnrelatedKeys()
    {
        await using var harness = await Harness.CreateAsync();
        using var first = await harness.AcquireAsync("Order-A");
        using var cancel = new CancellationTokenSource(TimeSpan.FromMilliseconds(200));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => harness.AcquireAsync(["Order-A", "Order-B"], cancel.Token).AsTask());

        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        using var independent = await harness.AcquireAsync("Order-B", timeout.Token);
    }

    [Fact]
    public async Task AmbientTransaction_TakesExclusiveAccess()
    {
        await using var harness = await Harness.CreateAsync();
        using var first = await harness.AcquireAsync("Order-A");
        await using var transaction = await harness.Db.Database.BeginTransactionAsync();
        using var cancel = new CancellationTokenSource(TimeSpan.FromMilliseconds(500));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => harness.AcquireAsync("Order-B", cancel.Token).AsTask());
    }

    private sealed class Harness : IAsyncDisposable
    {
        private readonly SqliteConnection _connection;
        private readonly StoreWriteLock _coordinator = new();

        private Harness(SqliteConnection connection, TestContext db)
        {
            _connection = connection;
            Db = db;
        }

        public TestContext Db { get; }
        public Action? OnCommand { get; set; }

        public static async Task<Harness> CreateAsync()
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();
            var harness = default(Harness);
            var options = new DbContextOptionsBuilder()
                .UseSqlite(connection)
                .EnableServiceProviderCaching(false)
                .AddInterceptors(new CallbackInterceptor(() => harness?.OnCommand?.Invoke()))
                .Options;
            var db = new TestContext(options);
            harness = new Harness(connection, db);
            return harness;
        }

        public ValueTask<IDisposable> AcquireAsync(string id, CancellationToken cancellationToken = default) =>
            AcquireAsync([id], cancellationToken);

        public ValueTask<IDisposable> AcquireAsync(string[] ids, CancellationToken cancellationToken) =>
            _coordinator.AcquireByIdAsync(Db, ids.Select(id => new Row { Id = id }).ToArray(), cancellationToken);

        public async ValueTask DisposeAsync()
        {
            await Db.DisposeAsync();
            await _connection.DisposeAsync();
        }
    }

    private sealed class Row : Entity;

    private sealed class TestContext(DbContextOptions options) : DbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder builder) => builder.Entity<Row>();
    }

    private sealed class CallbackInterceptor(Action onCommand) : DbCommandInterceptor
    {
        public override InterceptionResult<DbDataReader> ReaderExecuting(DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result)
        {
            onCommand();
            return base.ReaderExecuting(command, eventData, result);
        }

        public override InterceptionResult<int> NonQueryExecuting(DbCommand command, CommandEventData eventData, InterceptionResult<int> result)
        {
            onCommand();
            return base.NonQueryExecuting(command, eventData, result);
        }

        public override InterceptionResult<object> ScalarExecuting(DbCommand command, CommandEventData eventData, InterceptionResult<object> result)
        {
            onCommand();
            return base.ScalarExecuting(command, eventData, result);
        }
    }
}
