using Elsa.Common.Multitenancy;
using Elsa.Common.Entities;
using Elsa.Common.Models;
using Elsa.Persistence.Dapper.Extensions;
using Elsa.Persistence.Dapper.Models;
using Elsa.Persistence.Dapper.Services;
using Dapper;
using Microsoft.Data.Sqlite;

namespace Elsa.Persistence.Dapper.UnitTests;

public sealed class ParameterizedQueryBuilderExtensionsTests : IDisposable
{
    private readonly string _databasePath = Path.Combine(Path.GetTempPath(), $"elsa-dapper-{Guid.NewGuid():N}.db");
    private readonly Store<TestRecord> _store;
    private readonly TestTenantAccessor _tenantAccessor = new();

    public ParameterizedQueryBuilderExtensionsTests()
    {
        var connectionString = new SqliteConnectionStringBuilder { DataSource = _databasePath, Pooling = false }.ToString();
        var connectionProvider = new SqliteDbConnectionProvider(connectionString);

        using var connection = new SqliteConnection(connectionString);
        connection.Open();
        connection.Execute("""
                          create table TestRecords (
                              Id text not null,
                              TenantId text null,
                              Value text not null
                          );
                          insert into TestRecords (Id, TenantId, Value) values ('a1', 'tenant-a', 'value-a1');
                          insert into TestRecords (Id, TenantId, Value) values ('a2', 'tenant-a', 'value-a2');
                          insert into TestRecords (Id, TenantId, Value) values ('b1', 'tenant-b', 'value-b1');
                          """);

        _store = new Store<TestRecord>(connectionProvider, _tenantAccessor, "TestRecords");
    }

    [Fact]
    public async Task In_WithNullValues_PreservesOtherFilters()
    {
        using var tenantScope = _tenantAccessor.PushContext(new Tenant { Id = "tenant-a" });

        var result = await FindManyAsync(query => query
            .Is(nameof(TestRecord.Value), "value-a2")
            .In(nameof(TestRecord.Id), (IEnumerable<object>?)null));

        Assert.Equal(["a2"], result.Items.Select(x => x.Id));
        Assert.Equal(1, result.TotalCount);
    }

    [Fact]
    public async Task In_WithEmptyValues_ReturnsNoRows()
    {
        using var tenantScope = _tenantAccessor.PushContext(new Tenant { Id = "tenant-a" });

        var result = await FindManyAsync(query => query.In(nameof(TestRecord.Id), Array.Empty<object>()));

        Assert.Empty(result.Items);
        Assert.Equal(0, result.TotalCount);
    }

    [Fact]
    public async Task In_WithValues_ReturnsOnlyMatchingRowsForCurrentTenant()
    {
        using var tenantScope = _tenantAccessor.PushContext(new Tenant { Id = "tenant-a" });

        var result = await FindManyAsync(query => query.In(nameof(TestRecord.Id), new object[] { "a2", "b1" }));

        Assert.Equal(["a2"], result.Items.Select(x => x.Id));
        Assert.Equal(["tenant-a"], result.Items.Select(x => x.TenantId));
        Assert.Equal(1, result.TotalCount);
    }

    [Fact]
    public async Task In_WithValues_DoesNotMatchAnotherTenant()
    {
        using var tenantScope = _tenantAccessor.PushContext(new Tenant { Id = "tenant-a" });

        var result = await FindManyAsync(query => query.In(nameof(TestRecord.Id), new object[] { "b1" }));

        Assert.Empty(result.Items);
        Assert.Equal(0, result.TotalCount);
    }

    [Fact]
    public async Task In_WithTenantAgnosticFilter_ReturnsMatchingRowsAcrossTenants()
    {
        using var tenantScope = _tenantAccessor.PushContext(new Tenant { Id = "tenant-a" });

        var result = await FindManyAsync(
            query => query.In(nameof(TestRecord.Id), new object[] { "a2", "b1" }),
            tenantAgnostic: true);

        Assert.Equal(["a2", "b1"], result.Items.Select(x => x.Id));
        Assert.Equal(2, result.TotalCount);
    }

    public void Dispose()
    {
        File.Delete(_databasePath);
    }

    private Task<Page<TestRecord>> FindManyAsync(Action<ParameterizedQuery> filter, bool tenantAgnostic = false)
    {
        return _store.FindManyAsync(
            filter,
            PageArgs.All,
            nameof(TestRecord.Id),
            OrderDirection.Ascending,
            tenantAgnostic,
            cancellationToken: CancellationToken.None);
    }

    private sealed class TestRecord
    {
        public string Id { get; init; } = null!;
        public string TenantId { get; init; } = null!;
        public string Value { get; init; } = null!;
    }

    private sealed class TestTenantAccessor : ITenantAccessor
    {
        public string TenantId => Tenant?.Id ?? Elsa.Common.Multitenancy.Tenant.DefaultTenantId;
        public Tenant? Tenant { get; private set; }

        public IDisposable PushContext(Tenant? tenant)
        {
            var previousTenant = Tenant;
            Tenant = tenant;
            return new Restore(() => Tenant = previousTenant);
        }

        private sealed class Restore(Action restore) : IDisposable
        {
            public void Dispose() => restore();
        }
    }
}
