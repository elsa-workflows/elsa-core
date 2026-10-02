using Elsa.Common.Multitenancy;
using Elsa.KeyValues.Contracts;
using Elsa.KeyValues.Entities;
using Elsa.KeyValues.Models;
using Elsa.Persistence.MongoDb.Common;
using Elsa.Persistence.MongoDb.Modules.Runtime;
using MongoDB.Driver;
using Testcontainers.MongoDb;

namespace Elsa.MongoDb.UnitTests;

/// <summary>
/// Atomic <c>TryDeleteAsync</c> against a shared Mongo collection (#260).
/// Calls the Mongo store method directly; <c>IKeyValueStore.TryDeleteAsync</c> is not
/// in 3.10.0-preview.5722 yet (waiting for a core pin at or after c1c935ce).
/// </summary>
public sealed class MongoKeyValueStoreTryDeleteTests : IAsyncLifetime
{
    private const string LegacyKey = "elsa.quiescence.pause.default";
    private readonly MongoDbContainer _container = new MongoDbBuilder().WithImage("mongo:7.0.24").Build();
    private readonly TestTenantAccessor _tenants = new();
    private MongoClient? _client;
    private IMongoCollection<SerializedKeyValuePair> _collection = null!;
    private MongoDbStore<SerializedKeyValuePair> _mongoStore = null!;

    public async Task InitializeAsync()
    {
        try
        {
            await _container.StartAsync();
            _client = new MongoClient(_container.GetConnectionString());
            var database = _client.GetDatabase($"elsa-trydelete-{Guid.NewGuid():N}");
            _collection = database.GetCollection<SerializedKeyValuePair>("key_values");
            _mongoStore = new MongoDbStore<SerializedKeyValuePair>(_collection, _tenants);
        }
        catch
        {
            await DisposeAsync();
            throw;
        }
    }

    public async Task DisposeAsync()
    {
        try
        {
            _client?.Dispose();
        }
        finally
        {
            await _container.DisposeAsync();
        }
    }

    [Fact(DisplayName = "#260: two Mongo stores racing TryDeleteAsync: exactly one returns true")]
    public async Task TryDeleteAsync_TwoNodes_ExactlyOneReturnsTrue()
    {
        using var tenant = _tenants.PushContext(Tenant.Default);
        var nodeA = new MongoKeyValueStore(_mongoStore);
        var nodeB = new MongoKeyValueStore(_mongoStore);
        await nodeA.SaveAsync(Pair(LegacyKey, "legacy-maintenance"), CancellationToken.None);

        var results = await Task.WhenAll(nodeA.TryDeleteAsync(LegacyKey), nodeB.TryDeleteAsync(LegacyKey));

        Assert.Equal(1, results.Count(won => won));
        Assert.Equal(1, results.Count(won => !won));
        Assert.Null(await nodeA.FindAsync(new KeyValueFilter { Key = LegacyKey }, CancellationToken.None));
    }

    [Fact(DisplayName = "#260: the default find-then-delete lets both racers win")]
    public async Task DefaultTryDeleteAsync_TwoNodes_BothReturnTrue()
    {
        using var tenant = _tenants.PushContext(Tenant.Default);
        var inner = new MongoKeyValueStore(_mongoStore);
        await inner.SaveAsync(Pair(LegacyKey, "legacy-maintenance"), CancellationToken.None);
        var dim = new BarrierFindThenDelete(inner);

        var results = await Task.WhenAll(dim.TryDeleteAsync(LegacyKey), dim.TryDeleteAsync(LegacyKey));

        Assert.Equal(2, results.Count(won => won));
        Assert.Null(await inner.FindAsync(new KeyValueFilter { Key = LegacyKey }, CancellationToken.None));
    }

    [Fact(DisplayName = "#260: TryDeleteAsync returns false when the key is missing")]
    public async Task TryDeleteAsync_NotFound_ReturnsFalse()
    {
        using var tenant = _tenants.PushContext(Tenant.Default);
        var store = new MongoKeyValueStore(_mongoStore);

        Assert.False(await store.TryDeleteAsync(LegacyKey));
        Assert.False(await store.TryDeleteAsync(LegacyKey));
    }

    [Fact(DisplayName = "#260: a NULL TenantId legacy row is found and TryDeleted by the default tenant")]
    public async Task DefaultTenant_FindsAndTryDeletesNullTenantIdRow()
    {
        await _collection.InsertOneAsync(new SerializedKeyValuePair
        {
            Key = LegacyKey,
            SerializedValue = "legacy-null",
            TenantId = null
        });

        using var tenant = _tenants.PushContext(Tenant.Default);
        var store = new MongoKeyValueStore(_mongoStore);

        var found = await store.FindAsync(new KeyValueFilter { Key = LegacyKey }, CancellationToken.None);
        Assert.Equal("legacy-null", found?.SerializedValue);
        Assert.True(await store.TryDeleteAsync(LegacyKey));
        Assert.Null(await store.FindAsync(new KeyValueFilter { Key = LegacyKey }, CancellationToken.None));
    }

    private static SerializedKeyValuePair Pair(string key, string value) => new()
    {
        Key = key,
        SerializedValue = value
    };

    private sealed class TestTenantAccessor : ITenantAccessor
    {
        public string TenantId => Tenant?.Id ?? Tenant.DefaultTenantId;
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

    /// <summary>
    /// The core default <c>TryDeleteAsync</c> (find, then delete, always true if found).
    /// Both Finds complete before either Delete so the race is deterministic.
    /// </summary>
    private sealed class BarrierFindThenDelete(IKeyValueStore inner)
    {
        private readonly TaskCompletionSource _bothFound = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int _finds;

        public async Task<bool> TryDeleteAsync(string key)
        {
            var found = await inner.FindAsync(new KeyValueFilter { Key = key }, CancellationToken.None);
            if (Interlocked.Increment(ref _finds) == 2)
                _bothFound.TrySetResult();
            await _bothFound.Task;
            if (found is null)
                return false;
            await inner.DeleteAsync(key, CancellationToken.None);
            return true;
        }
    }
}
