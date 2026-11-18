using Elsa.Common.Multitenancy;
using Elsa.Common.Serialization;
using Elsa.Common.Services;
using Elsa.Persistence.Dapper.Contracts;
using Elsa.Persistence.Dapper.Extensions;
using Elsa.Persistence.Dapper.Modules.Runtime.Records;
using Elsa.Persistence.Dapper.Modules.Runtime.Stores;
using Elsa.Persistence.Dapper.Services;
using Elsa.Workflows;

using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.Runtime.Filters;
using Elsa.Workflows.Runtime.Messages;
using Elsa.Workflows.Runtime.Options;
using Elsa.Workflows.Serialization.Serializers;
using Elsa.Workflows.Services;
using FluentMigrator.Runner;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Elsa.Persistence.Dapper.UnitTests;

public sealed class DapperBookmarkQueueRecoveryTests
{
    private const string PostgreSqlConnectionVariable = "ELSA_DAPPER_QUEUE_POSTGRES_TEST_CONNECTION";
    private const string SqlServerConnectionVariable = "ELSA_DAPPER_QUEUE_SQLSERVER_TEST_CONNECTION";

    [Fact]
    public async Task SqliteMovesUnreadableItemsOutOfAQueueAndRetainsTheirPayload()
    {
        var path = Path.Combine(Path.GetTempPath(), $"elsa-dapper-queue-{Guid.NewGuid():N}.db");
        var connectionString = new SqliteConnectionStringBuilder { DataSource = path, Pooling = false }.ToString();

        try
        {
            await ExerciseRecoveryAsync(new SqliteDbConnectionProvider(connectionString), connectionString, Provider.Sqlite);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [NativePostgreSqlFact]
    public Task PostgreSqlMovesUnreadableItemsOutOfAQueueAndRetainsTheirPayload()
    {
        var connectionString = Environment.GetEnvironmentVariable(PostgreSqlConnectionVariable)!;
        return ExerciseRecoveryAsync(new PostgreSqlDbConnectionProvider(connectionString), connectionString, Provider.PostgreSql);
    }

    [NativeSqlServerFact]
    public Task SqlServerMovesUnreadableItemsOutOfAQueueAndRetainsTheirPayload()
    {
        var connectionString = Environment.GetEnvironmentVariable(SqlServerConnectionVariable)!;
        return ExerciseRecoveryAsync(new SqlServerDbConnectionProvider(connectionString), connectionString, Provider.SqlServer);
    }

    private static async Task ExerciseRecoveryAsync(IDbConnectionProvider connectionProvider, string connectionString, Provider provider)
    {
        await using var services = new ServiceCollection()
            .AddLogging()
            .AddSingleton(SerializationTypeRegistry.CreateDefault())
            .AddFluentMigratorCore()
            .ConfigureRunner(builder =>
            {
                switch (provider)
                {
                    case Provider.PostgreSql:
                        builder.AddPostgres();
                        break;
                    case Provider.SqlServer:
                        builder.AddSqlServer();
                        break;
                    default:
                        builder.AddSQLite();
                        break;
                }

                builder.WithGlobalConnectionString(connectionString)
                    .ScanIn(typeof(Migrations.Runtime.Initial).Assembly).For.Migrations();
            }).BuildServiceProvider();
        using var scope = services.CreateScope();
        var runner = scope.ServiceProvider.GetRequiredService<IMigrationRunner>();
        runner.MigrateUp(20008);

        var key = Guid.NewGuid().ToString("N");
        var tenantId = $"queue-{key}";
        var tenantAccessor = new DefaultTenantAccessor();
        using var tenant = tenantAccessor.PushContext(new Tenant { Id = tenantId, Name = tenantId });
        var activeRecords = new Store<BookmarkQueueItemRecord>(connectionProvider, tenantAccessor, "BookmarkQueueItems");
        var deadLetterRecords = new Store<BookmarkQueueDeadLetterRecord>(connectionProvider, tenantAccessor, "BookmarkQueueDeadLetterItems");
        const string unreadable = """{"input":""";
        using (var connection = connectionProvider.GetConnection())
        {
            var dialect = connectionProvider.Dialect;
            var table = dialect.QuoteIdentifier("BookmarkQueueItems");
            var columns = new[] { "Id", "TenantId", "BookmarkId", "CreatedAt", "SerializedOptions" }.Select(dialect.QuoteIdentifier);
            const string parameters = "@Id, @TenantId, @BookmarkId, @CreatedAt, @SerializedOptions";
            await global::Dapper.SqlMapper.ExecuteAsync(connection, $"INSERT INTO {table} ({string.Join(", ", columns)}) VALUES ({parameters})",
                new { Id = key, TenantId = tenantId, BookmarkId = "first", CreatedAt = DateTimeOffset.UnixEpoch, SerializedOptions = unreadable });
        }

        runner.MigrateUp();
        var serializer = new JsonPayloadSerializer(scope.ServiceProvider);
        var active = new DapperBookmarkQueueStore(activeRecords, serializer);
        var deadLetter = new DapperBookmarkQueueDeadLetterStore(deadLetterRecords, serializer);
        await active.AddAsync(new()
        {
            Id = $"{key}-valid",
            BookmarkId = "second",
            CreatedAt = DateTimeOffset.UnixEpoch.AddMinutes(1),
            Options = new() { Input = new Dictionary<string, object> { ["name"] = "Alice" } }
        });

        var resumer = Substitute.For<IWorkflowResumer>();
        resumer.ResumeAsync(Arg.Any<BookmarkFilter>(), Arg.Any<ResumeBookmarkOptions>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IEnumerable<RunWorkflowInstanceResponse>>([new()]));
        var clock = new SystemClock();
        var manager = new BookmarkQueueDeadLetterManager(deadLetter, active, Substitute.For<IBookmarkQueueSignaler>(),
            clock, new GuidIdentityGenerator(), NullLogger<BookmarkQueueDeadLetterManager>.Instance);
        var processor = new BookmarkQueueProcessor(active, manager, resumer, clock, Options.Create(new BookmarkQueuePurgeOptions()),
            NullLogger<BookmarkQueueProcessor>.Instance);

        await processor.ProcessAsync();

        await resumer.Received(1).ResumeAsync(Arg.Is<BookmarkFilter>(filter => filter.BookmarkId == "second"),
            Arg.Is<ResumeBookmarkOptions>(options => options.Input!["name"].Equals("Alice")), Arg.Any<CancellationToken>());
        Assert.Empty(await active.FindManyAsync(new BookmarkQueueFilter { Ids = [key, $"{key}-valid"] }));
        var result = await deadLetter.FindAsync(new() { OriginalQueueItemId = key });
        Assert.NotNull(result);
        Assert.Equal("UnreadableOptions", result.Reason);
        Assert.False(result.CanReplay);
        Assert.Null(await deadLetter.TryMarkReplayedAsync(result.Id, "replay", DateTimeOffset.UnixEpoch));
        var stored = await deadLetterRecords.FindAsync(query => query.Is(nameof(BookmarkQueueDeadLetterRecord.Id), result.Id), false, CancellationToken.None);
        Assert.Equal(unreadable, stored?.SerializedOptions);

        var replayable = new BookmarkQueueDeadLetterItem
        {
            Id = $"{key}-replayable",
            OriginalQueueItemId = $"{key}-historical",
            BookmarkId = "replay-bookmark",
            OriginalCreatedAt = DateTimeOffset.UnixEpoch,
            DeadLetteredAt = DateTimeOffset.UnixEpoch.AddMinutes(2),
            Reason = "Failed",
            Options = new() { Input = new Dictionary<string, object> { ["name"] = "Alice" } }
        };
        await deadLetter.AddOrGetExistingAsync(replayable);
        var replay = await manager.ReplayAsync(replayable.Id);
        Assert.True(replay.Succeeded);
        Assert.False((await manager.ReplayAsync(replayable.Id)).Succeeded);
        var replayed = await active.FindAsync(new BookmarkQueueFilter { Id = replay.QueueItemId });
        Assert.Equal("Alice", replayed?.Options?.Input?["name"]);
    }

    private enum Provider
    {
        Sqlite,
        PostgreSql,
        SqlServer
    }

    public sealed class NativeSqlServerFactAttribute : FactAttribute
    {
        public NativeSqlServerFactAttribute()
        {
            if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(SqlServerConnectionVariable)))
                Skip = $"Set {SqlServerConnectionVariable} to a disposable SQL Server database.";
        }
    }

    public sealed class NativePostgreSqlFactAttribute : FactAttribute
    {
        public NativePostgreSqlFactAttribute()
        {
            if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(PostgreSqlConnectionVariable)))
                Skip = $"Set {PostgreSqlConnectionVariable} to a disposable PostgreSQL database.";
        }
    }
}
