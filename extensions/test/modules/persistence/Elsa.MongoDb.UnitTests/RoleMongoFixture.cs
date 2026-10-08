using Elsa.Common.Multitenancy;
using Testcontainers.MongoDb;

namespace Elsa.MongoDb.UnitTests;

/// <summary>
/// The MongoDB server for the role store and role index tests. It starts a Testcontainers MongoDB unless
/// <c>ELSA_MONGO_TEST_CONNECTION</c> points at an existing server, which lets the tests run where Docker is not
/// available. Each test uses its own database either way.
/// </summary>
public sealed class RoleMongoFixture : IAsyncLifetime
{
    private const string ConnectionVariable = "ELSA_MONGO_TEST_CONNECTION";
    private readonly string? _externalConnectionString = Environment.GetEnvironmentVariable(ConnectionVariable);
    private MongoDbContainer? _container;

    public string ConnectionString => _externalConnectionString ?? _container?.GetConnectionString() ?? throw new InvalidOperationException("The MongoDB fixture has not been initialized.");

    public async Task InitializeAsync()
    {
        if (!string.IsNullOrWhiteSpace(_externalConnectionString))
        {
            return;
        }

        _container = new MongoDbBuilder().WithImage("mongo:7.0.24").Build();
        await _container.StartAsync();
    }

    public async Task DisposeAsync()
    {
        if (_container != null)
        {
            await _container.DisposeAsync();
        }
    }
}

/// <summary>
/// A tenant accessor whose tenant can be switched with <see cref="PushContext"/>.
/// </summary>
internal sealed class RoleTestTenantAccessor : ITenantAccessor
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
