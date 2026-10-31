using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using Dapper;
using Elsa.Common.Multitenancy;
using Elsa.KeyValues.Contracts;
using Elsa.KeyValues.Entities;
using Elsa.Persistence.Dapper.Contracts;
using Elsa.Persistence.Dapper.Modules.Runtime.Records;
using Elsa.Persistence.Dapper.Modules.Runtime.Stores;
using Elsa.Persistence.Dapper.Services;
using Testcontainers.PostgreSql;

namespace Elsa.Persistence.Dapper.UnitTests;

public sealed class DapperKeyValueStoreOwnershipSqliteTests : DapperKeyValueStoreOwnershipTests
{
    private readonly string _path = Path.Combine(Path.GetTempPath(), $"key-owner-{Guid.NewGuid():N}.db");
    protected override IDbConnectionProvider Provider => new SqliteDbConnectionProvider($"Data Source={_path};Pooling=false;Default Timeout=30");

    public override async Task DisposeAsync()
    {
        await base.DisposeAsync();
        File.Delete(_path);
    }
}

public sealed class DapperKeyValueStoreOwnershipPostgreSqlTests(PostgresKeyOwnershipFixture fixture)
    : DapperKeyValueStoreOwnershipTests, IClassFixture<PostgresKeyOwnershipFixture>
{
    protected override IDbConnectionProvider Provider => new PostgreSqlDbConnectionProvider(fixture.ConnectionString);
}

public sealed class PostgresKeyOwnershipFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder().WithImage("postgres:16").Build();
    public string ConnectionString => _container.GetConnectionString();
    public Task InitializeAsync() => _container.StartAsync();
    public async Task DisposeAsync() => await _container.DisposeAsync();
}

public abstract class DapperKeyValueStoreOwnershipTests : IAsyncLifetime
{
    private readonly string _table = $"KeyValues_{Guid.NewGuid():N}";
    protected abstract IDbConnectionProvider Provider { get; }

    public async Task InitializeAsync()
    {
        using var connection = Provider.GetConnection();
        await connection.ExecuteAsync($"""create table "{_table}" ("Id" text not null primary key, "TenantId" text null, "Value" text null check ("Value" <> 'forbidden'))""");
    }

    public virtual async Task DisposeAsync()
    {
        using var connection = Provider.GetConnection();
        await connection.ExecuteAsync($"""drop table if exists "{_table}" """);
    }

    [Fact]
    public async Task SameOwner_CanInsertUpdateAndSaveUnchangedValue()
    {
        var store = CreateStore("alpha");
        await Save(store, "first");
        await Save(store, "second");
        await Save(store, "second");
        await AssertRow("alpha", "second");
    }

    [Theory]
    [InlineData("alpha")]
    [InlineData("*")]
    public async Task ForeignOwner_SaveCannotReplaceOwnerOrValue(string owner)
    {
        await Seed(owner, "original");
        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => Save(CreateStore("beta"), "replacement"));
        Assert.Contains("tenant", error.Message, StringComparison.OrdinalIgnoreCase);
        await AssertRow(owner, "original");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task DefaultTenant_SavePreservesLegacyOwnerRepresentation(string? owner)
    {
        await Seed(owner, "original");
        await Save(CreateStore(""), "replacement");
        await AssertRow(owner, "replacement");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ForgedIncomingTenant_CannotRehomeKey(bool existing)
    {
        if (existing)
        {
            await Seed("alpha", "original");
        }
        await Save(CreateStore("alpha"), "replacement", "forged");
        await AssertRow("alpha", "replacement");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CompetingInserts_PreserveOneConsistentOwner(bool sameOwner)
    {
        var barrier = new InsertBarrier();
        var provider = new InterceptingProvider(Provider, barrier.BeforeInsert);
        var first = Save(CreateStore("alpha", provider), "first");
        var secondOwner = sameOwner ? "alpha" : "beta";
        var second = Save(CreateStore(secondOwner, provider), "second");
        var results = await Task.WhenAll(Capture(first), Capture(second));
        var row = await ReadRow();
        if (sameOwner)
        {
            Assert.All(results, Assert.Null);
            Assert.Equal("alpha", row.TenantId);
            Assert.Contains(row.Value, new[] { "first", "second" });
        }
        else
        {
            Assert.Single(results, x => x == null);
            Assert.IsType<InvalidOperationException>(Assert.Single(results, x => x != null));
            var firstWon = results[0] == null;
            Assert.Equal(firstWon ? "alpha" : "beta", row.TenantId);
            Assert.Equal(firstWon ? "first" : "second", row.Value);
        }
    }

    [Fact]
    public async Task ZeroAffectedRows_UnchangedValueIsSuccessful()
    {
        await Seed("alpha", "requested");
        var provider = new InterceptingProvider(Provider, afterUpdate: _ => Task.FromResult(0));
        await Save(CreateStore("alpha", provider), "requested");
        await AssertRow("alpha", "requested");
    }

    [Fact]
    public async Task MissedUpdate_ConcurrentOwnedInsertWithDifferentValueIsUpdated()
    {
        var updates = 0;
        var provider = new InterceptingProvider(Provider, afterUpdate: async count =>
        {
            if (Interlocked.Increment(ref updates) == 1 && count == 0)
            {
                await Seed("alpha", "concurrent");
            }
            return count;
        });
        await Save(CreateStore("alpha", provider), "requested");
        Assert.True(updates >= 2);
        await AssertRow("alpha", "requested");
    }

    [Fact]
    public async Task SustainedOwnedDeleteAndReinsert_SaveTerminatesWithoutCancellation()
    {
        var updates = 0;
        var provider = new InterceptingProvider(Provider, beforeUpdate: async () =>
        {
            // Keep the regression bounded before the fix without leaking a background save.
            if (++updates > 12)
            {
                throw new ChurnSafetyException();
            }
            using var connection = Provider.GetConnection();
            await connection.ExecuteAsync($"""delete from "{_table}" where "Id" = 'shared' """);
        }, afterUpdate: async count =>
        {
            Assert.Equal(0, count);
            await Seed("alpha", "concurrent");
            return count;
        });

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => Save(CreateStore("alpha", provider), "requested"));

        Assert.Contains("shared", error.Message, StringComparison.Ordinal);
        Assert.Contains("sustained concurrent changes", error.Message, StringComparison.OrdinalIgnoreCase);
        await AssertRow("alpha", "concurrent");
    }

    private sealed class ChurnSafetyException : Exception;

    [Fact]
    public async Task UnrelatedInsertFailure_Propagates()
    {
        await Assert.ThrowsAnyAsync<DbException>(() => Save(CreateStore("alpha"), "forbidden"));
        using var connection = Provider.GetConnection();
        Assert.Equal(0, await connection.ExecuteScalarAsync<int>($"""select count(*) from "{_table}" """));
    }

    [Fact]
    public async Task NullKey_DoesNotUpdateOtherOwnedRows()
    {
        await Seed("alpha", "original");
        var pair = new SerializedKeyValuePair { Id = null!, SerializedValue = "replacement" };
        await Assert.ThrowsAsync<ArgumentNullException>(() => CreateStore("alpha").SaveAsync(pair, CancellationToken.None));
        await AssertRow("alpha", "original");
    }

    private IKeyValueStore CreateStore(string tenantId, IDbConnectionProvider? provider = null) =>
        new DapperKeyValueStore(new Store<KeyValuePairRecord>(provider ?? Provider, new FixedTenantAccessor(tenantId), _table));

    private static Task Save(IKeyValueStore store, string value, string? incomingTenant = null) =>
        store.SaveAsync(new SerializedKeyValuePair { Id = "shared", SerializedValue = value, TenantId = incomingTenant }, CancellationToken.None);

    private async Task Seed(string? tenantId, string value)
    {
        using var connection = Provider.GetConnection();
        await connection.ExecuteAsync($"""insert into "{_table}" ("Id", "TenantId", "Value") values ('shared', @tenantId, @value)""", new { tenantId, value });
    }

    private async Task<KeyValuePairRecord> ReadRow()
    {
        using var connection = Provider.GetConnection();
        return await connection.QuerySingleAsync<KeyValuePairRecord>($"""select * from "{_table}" """);
    }

    private async Task AssertRow(string? owner, string value)
    {
        var row = await ReadRow();
        Assert.Equal(owner, row.TenantId);
        Assert.Equal(value, row.Value);
    }

    private static async Task<Exception?> Capture(Task save)
    {
        try
        {
            await save;
            return null;
        }
        catch (Exception error)
        {
            return error;
        }
    }

    private sealed class FixedTenantAccessor(string tenantId) : ITenantAccessor
    {
        public string TenantId => tenantId;
        public Tenant Tenant { get; } = new() { Id = tenantId };
        public IDisposable PushContext(Tenant? tenant) => throw new NotSupportedException();
    }

    private sealed class InsertBarrier
    {
        private readonly TaskCompletionSource _ready = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int _arrivals;
        public async Task BeforeInsert()
        {
            if (Interlocked.Increment(ref _arrivals) == 2)
            {
                _ready.SetResult();
            }
            await _ready.Task.WaitAsync(TimeSpan.FromSeconds(30));
        }
    }

    // Intercept actual database commands to reproduce insert races and providers reporting zero changed rows.
    private sealed class InterceptingProvider(IDbConnectionProvider inner, Func<Task>? beforeInsert = null, Func<int, Task<int>>? afterUpdate = null, Func<Task>? beforeUpdate = null) : IDbConnectionProvider
    {
        public string GetConnectionString() => inner.GetConnectionString();
        public ISqlDialect Dialect => inner.Dialect;
        public IDbConnection GetConnection() => new InterceptingConnection((DbConnection)inner.GetConnection(), beforeInsert, afterUpdate, beforeUpdate);
    }

    private sealed class InterceptingConnection(DbConnection inner, Func<Task>? beforeInsert, Func<int, Task<int>>? afterUpdate, Func<Task>? beforeUpdate) : DbConnection
    {
        [AllowNull] public override string ConnectionString { get => inner.ConnectionString; set => inner.ConnectionString = value; }
        public override string Database => inner.Database;
        public override string DataSource => inner.DataSource;
        public override string ServerVersion => inner.ServerVersion;
        public override ConnectionState State => inner.State;
        public override void ChangeDatabase(string databaseName) => inner.ChangeDatabase(databaseName);
        public override void Close() => inner.Close();
        public override void Open() => inner.Open();
        public override Task OpenAsync(CancellationToken cancellationToken) => inner.OpenAsync(cancellationToken);
        protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel) => inner.BeginTransaction(isolationLevel);
        protected override DbCommand CreateDbCommand() => new InterceptingCommand(inner.CreateCommand(), beforeInsert, afterUpdate, beforeUpdate);
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                inner.Dispose();
            }
            base.Dispose(disposing);
        }
    }

    private sealed class InterceptingCommand(DbCommand inner, Func<Task>? beforeInsert, Func<int, Task<int>>? afterUpdate, Func<Task>? beforeUpdate) : DbCommand
    {
        [AllowNull] public override string CommandText { get => inner.CommandText; set => inner.CommandText = value; }
        public override int CommandTimeout { get => inner.CommandTimeout; set => inner.CommandTimeout = value; }
        public override CommandType CommandType { get => inner.CommandType; set => inner.CommandType = value; }
        public override bool DesignTimeVisible { get => inner.DesignTimeVisible; set => inner.DesignTimeVisible = value; }
        public override UpdateRowSource UpdatedRowSource { get => inner.UpdatedRowSource; set => inner.UpdatedRowSource = value; }
        protected override DbConnection? DbConnection { get => inner.Connection; set => inner.Connection = value; }
        protected override DbTransaction? DbTransaction { get => inner.Transaction; set => inner.Transaction = value; }
        protected override DbParameterCollection DbParameterCollection => inner.Parameters;
        public override void Cancel() => inner.Cancel();
        public override int ExecuteNonQuery() => inner.ExecuteNonQuery();
        public override object? ExecuteScalar() => inner.ExecuteScalar();
        public override void Prepare() => inner.Prepare();
        protected override DbParameter CreateDbParameter() => inner.CreateParameter();
        protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior) => inner.ExecuteReader(behavior);
        protected override Task<DbDataReader> ExecuteDbDataReaderAsync(CommandBehavior behavior, CancellationToken cancellationToken) => inner.ExecuteReaderAsync(behavior, cancellationToken);
        public override async Task<int> ExecuteNonQueryAsync(CancellationToken cancellationToken)
        {
            if (beforeInsert != null && CommandText.StartsWith("insert", StringComparison.OrdinalIgnoreCase))
            {
                await beforeInsert();
            }
            if (beforeUpdate != null && CommandText.StartsWith("update", StringComparison.OrdinalIgnoreCase))
            {
                await beforeUpdate();
            }
            var count = await inner.ExecuteNonQueryAsync(cancellationToken);
            return afterUpdate != null && CommandText.StartsWith("update", StringComparison.OrdinalIgnoreCase) ? await afterUpdate(count) : count;
        }
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                inner.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
