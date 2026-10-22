using Dapper;
using Elsa.Common.Multitenancy;
using Elsa.Persistence.Dapper.Services;
using Microsoft.Data.Sqlite;

namespace Elsa.Persistence.Dapper.UnitTests;

/// <summary>
/// Tenant-scoped Dapper <see cref="Store{T}"/> upserts (#9010): a cross-tenant ID collision
/// throws and leaves the original row unchanged; a same-tenant save still updates.
/// </summary>
public sealed class DapperStoreTenantUpsertTests : IDisposable
{
    private readonly string _databasePath = Path.Join(Path.GetTempPath(), $"elsa-dapper-store-upsert-{Guid.NewGuid():N}.db");
    private readonly string _connectionString;
    private readonly Store<ItemRecord> _store;
    private readonly RoleTestTenantAccessor _tenantAccessor = new();

    public DapperStoreTenantUpsertTests()
    {
        _connectionString = new SqliteConnectionStringBuilder { DataSource = _databasePath, Pooling = false }.ToString();

        using var connection = new SqliteConnection(_connectionString);
        connection.Open();
        connection.Execute("""
                           create table Items (
                               Id text not null primary key,
                               TenantId text null,
                               Name text not null
                           );
                           """);

        _store = new Store<ItemRecord>(new SqliteDbConnectionProvider(_connectionString), _tenantAccessor, "Items");
    }

    [Fact]
    public async Task CrossTenantIdCollision_Throws_AndLeavesTenantARowUnchanged()
    {
        using (_tenantAccessor.PushContext(TenantA()))
            await _store.SaveAsync(Item("shared", "owned-by-a"));

        Exception? exception;
        using (_tenantAccessor.PushContext(TenantB()))
            exception = await Record.ExceptionAsync(() => _store.SaveAsync(Item("shared", "taken-by-b")));

        var invalidOperation = Assert.IsType<InvalidOperationException>(exception);
        Assert.Contains("another tenant", invalidOperation.Message, StringComparison.Ordinal);
        Assert.Equal([("shared", "tenant-a", "owned-by-a")], LoadItems());
    }

    [Fact]
    public async Task SameTenantUpsert_UpdatesTheExistingRow()
    {
        using (_tenantAccessor.PushContext(TenantA()))
        {
            await _store.SaveAsync(Item("shared", "original"));
            await _store.SaveAsync(Item("shared", "updated"));
        }

        Assert.Equal([("shared", "tenant-a", "updated")], LoadItems());
    }

    public void Dispose() => File.Delete(_databasePath);

    private IReadOnlyList<(string Id, string? TenantId, string Name)> LoadItems()
    {
        using var connection = new SqliteConnection(_connectionString);
        return connection.Query<(string, string?, string)>("select Id, TenantId, Name from Items order by Id").ToList();
    }

    private static Tenant TenantA() => new() { Id = "tenant-a" };
    private static Tenant TenantB() => new() { Id = "tenant-b" };

    private static ItemRecord Item(string id, string name) => new()
    {
        Id = id,
        Name = name
    };

    internal sealed class ItemRecord : Elsa.Persistence.Dapper.Records.Record
    {
        public string Name { get; set; } = null!;
    }
}
