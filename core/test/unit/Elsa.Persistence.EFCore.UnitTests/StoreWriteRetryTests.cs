using Elsa.Persistence.EFCore;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Persistence.EFCore.UnitTests;

public sealed class StoreWriteRetryTests
{
    [Theory]
    [InlineData(1205)]
    [InlineData(1213)]
    public async Task ExecuteWriteWithRetryAsync_WhenMySqlLockFailureOccurs_RetriesWholeOperationWithFreshContextAndTransaction(int errorNumber)
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var factory = new TrackingDbContextFactory(CreateOptions(connection));
        var store = new Store<RetryDbContext, RetryEntity>(factory, new ServiceCollection().BuildServiceProvider());
        var attempts = 0;
        var transactionCount = 0;

        await store.ExecuteWriteWithRetryAsync(
            async (dbContext, cancellationToken) =>
            {
                var attempt = Interlocked.Increment(ref attempts);
                await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
                Interlocked.Increment(ref transactionCount);

                if (attempt == 1)
                    throw new MySqlConnector.MySqlException(errorNumber);

                await transaction.CommitAsync(cancellationToken);
            },
            CancellationToken.None);

        Assert.Equal(2, attempts);
        Assert.Equal(2, transactionCount);
        Assert.Equal(2, factory.Contexts.Count);
        Assert.NotSame(factory.Contexts[0], factory.Contexts[1]);
        Assert.NotEqual(factory.Contexts[0].ContextId.InstanceId, factory.Contexts[1].ContextId.InstanceId);
    }

    [Fact]
    public async Task ExecuteWriteWithRetryAsync_WhenMySqlDuplicateKeyOccurs_DoesNotRetry()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var factory = new TrackingDbContextFactory(CreateOptions(connection));
        var store = new Store<RetryDbContext, RetryEntity>(factory, new ServiceCollection().BuildServiceProvider());
        var attempts = 0;

        await Assert.ThrowsAsync<MySqlConnector.MySqlException>(() => store.ExecuteWriteWithRetryAsync(
            (_, _) =>
            {
                Interlocked.Increment(ref attempts);
                throw new MySqlConnector.MySqlException(1062);
            },
            CancellationToken.None));

        Assert.Equal(1, attempts);
        Assert.Single(factory.Contexts);
    }

    private static DbContextOptions<RetryDbContext> CreateOptions(SqliteConnection connection) =>
        new DbContextOptionsBuilder<RetryDbContext>()
            .UseSqlite(connection)
            .ReplaceService<IDatabaseProvider, FakeMySqlDatabaseProvider>()
            .Options;

    private sealed class RetryDbContext(DbContextOptions<RetryDbContext> options) : DbContext(options);

    private sealed class RetryEntity
    {
        public string Id { get; set; } = string.Empty;
    }

    private sealed class TrackingDbContextFactory(DbContextOptions<RetryDbContext> options) : IDbContextFactory<RetryDbContext>
    {
        public List<RetryDbContext> Contexts { get; } = [];

        public RetryDbContext CreateDbContext()
        {
            var context = new RetryDbContext(options);
            Contexts.Add(context);
            return context;
        }

        public Task<RetryDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(CreateDbContext());
        }
    }

    private sealed class FakeMySqlDatabaseProvider : IDatabaseProvider
    {
        public string Name => "Pomelo.EntityFrameworkCore.MySql";

        public bool IsConfigured(IDbContextOptions options) => true;
    }
}
