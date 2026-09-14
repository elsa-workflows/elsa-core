using Elsa.Persistence.EFCore;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Persistence.EFCore.UnitTests;

public sealed class StoreWriteRetryTests
{
    [Test]
    [Arguments(1205)]
    [Arguments(1213)]
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

        await Assert.That(attempts).IsEqualTo(2);
        await Assert.That(transactionCount).IsEqualTo(2);
        await Assert.That(factory.Contexts.Count).IsEqualTo(2);
        await Assert.That(factory.Contexts[1]).IsNotSameReferenceAs(factory.Contexts[0]);
        await Assert.That(factory.Contexts[1].ContextId.InstanceId).IsNotEqualTo(factory.Contexts[0].ContextId.InstanceId);
    }

    [Test]
    public async Task ExecuteWriteWithRetryAsync_WhenMySqlDuplicateKeyOccurs_DoesNotRetry()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var factory = new TrackingDbContextFactory(CreateOptions(connection));
        var store = new Store<RetryDbContext, RetryEntity>(factory, new ServiceCollection().BuildServiceProvider());
        var attempts = 0;

        await Assert.ThrowsExactlyAsync<MySqlConnector.MySqlException>(() => store.ExecuteWriteWithRetryAsync(
            (_, _) =>
            {
                Interlocked.Increment(ref attempts);
                throw new MySqlConnector.MySqlException(1062);
            },
            CancellationToken.None));

        await Assert.That(attempts).IsEqualTo(1);
        await Assert.That(factory.Contexts).HasSingleItem();
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
