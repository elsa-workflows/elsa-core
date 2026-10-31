using Elsa.Common.Multitenancy;
using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Models;
using Elsa.Workflows.Runtime.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Elsa.Workflows.Runtime.UnitTests.Recovery;

public class OwnDatabaseTenantRecoveryTests
{
    private const string SharedKey = "shared";
    private readonly DefaultTenantAccessor _tenantAccessor = new();
    private readonly ITenantService _tenantService = Substitute.For<ITenantService>();
    private readonly ITenantScopeFactory _scopeFactory = Substitute.For<ITenantScopeFactory>();
    private readonly ILogger _logger = Substitute.For<ILogger>();
    private readonly Dictionary<string, Database> _databases = new();
    private readonly List<string> _scopedTenantIds = [];
    private readonly List<(string InstanceId, string? TenantId, string Database)> _restarts = [];
    private readonly List<(string Database, bool TenantAgnostic)> _queries = [];

    public OwnDatabaseTenantRecoveryTests()
    {
        _databases[SharedKey] = CreateDatabase(SharedKey);
        _tenantService.FindAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(ci => Task.FromResult<Tenant?>(new Tenant { Id = ci.ArgAt<string>(0), Name = ci.ArgAt<string>(0) }));
        _scopeFactory.CreateScope(Arg.Any<Tenant?>()).Returns(ci =>
        {
            var tenant = ci.Arg<Tenant?>();
            _scopedTenantIds.Add(tenant!.Id);
            var database = _databases[ReferenceEquals(tenant, Tenant.Default) ? "default-scope" : tenant.Id];
            var services = new ServiceCollection().AddSingleton(database.Store).AddSingleton(database.Restarter).BuildServiceProvider();
            return new TenantScope(services.CreateScope(), _tenantAccessor, tenant);
        });
    }

    [Theory(DisplayName = "HasOwnDatabase is true only for a tenant with a non-blank connection string")]
    [InlineData("t1", null, false)]
    [InlineData("t1", "", false)]
    [InlineData("t1", "  ", false)]
    [InlineData("", "Data Source=a.db", true)]
    [InlineData("t1", "Data Source=a.db", true)]
    public void HasOwnDatabase(string tenantId, string? connectionString, bool expected)
    {
        var values = new Dictionary<string, string?>();

        if (connectionString is not null)
            values["ConnectionStrings:Sqlite"] = connectionString;

        var tenant = new Tenant { Id = tenantId, Name = tenantId, Configuration = new ConfigurationBuilder().AddInMemoryCollection(values).Build() };

        Assert.Equal(expected, tenant.HasOwnDatabase());
    }

    [Fact(DisplayName = "A tenant with its own database is scanned in its own scope, tenant-scoped, and restarted under its tenant")]
    public async Task OwnDatabaseTenant_ScannedInOwnScope()
    {
        var tenant = OwnDatabaseTenant("tenant-own");
        UseTenants(tenant);
        AddInstance(tenant.Id, "x", Tenant.AgnosticTenantId);

        var count = await RequeueAsync();

        Assert.Equal(1, count);
        Assert.Equal([SharedKey, "tenant-own"], _queries.Select(x => x.Database));
        Assert.Equal([true, false], _queries.Select(x => x.TenantAgnostic));
        Assert.Equal([("x", "tenant-own", "tenant-own")], _restarts);
    }

    [Fact(DisplayName = "Tenants sharing the default database never get a scope and the agnostic scan runs once")]
    public async Task SharedDatabaseTenants_NoScope()
    {
        UseTenants(new Tenant { Id = "t1", Name = "t1" }, new Tenant { Id = "t2", Name = "t2" });
        AddInstance(SharedKey, "a", "t1");

        var count = await RequeueAsync();

        Assert.Equal(1, count);
        Assert.Equal([(SharedKey, true)], _queries);
        _scopeFactory.DidNotReceiveWithAnyArgs().CreateScope(default);
    }

    [Fact(DisplayName = "An instance id found in both the shared and a tenant database is restarted once")]
    public async Task SameIdInBothScans_RestartedOnce()
    {
        var tenant = OwnDatabaseTenant("tenant-own");
        UseTenants(tenant);
        AddInstance(SharedKey, "x", "tenant-own");
        AddInstance(tenant.Id, "x", "tenant-own");

        var count = await RequeueAsync();

        Assert.Equal(1, count);
        Assert.Single(_restarts);
    }

    [Fact(DisplayName = "A failing tenant does not stop other tenants or the shared pass")]
    public async Task FailingTenant_OthersStillRecovered()
    {
        var failing = OwnDatabaseTenant("tenant-failing");
        var healthy = OwnDatabaseTenant("tenant-healthy");
        UseTenants(failing, healthy);
        AddInstance(SharedKey, "shared-1", "t1");
        AddInstance(healthy.Id, "healthy-1", "tenant-healthy");
        _databases[failing.Id].Throws = true;

        var count = await RequeueAsync();

        Assert.Equal(2, count);
        Assert.Equal(["shared-1", "healthy-1"], _restarts.Select(x => x.InstanceId));
    }

    [Fact(DisplayName = "A default tenant with its own database is scanned in its own scope at startup, without an ambient tenant")]
    public async Task DefaultTenantWithOwnDatabase_ScannedAtStartup()
    {
        var defaultTenant = OwnDatabaseTenant(Tenant.DefaultTenantId);
        UseTenants(defaultTenant);
        AddInstance(SharedKey, "shared", Tenant.AgnosticTenantId);
        AddInstance(Tenant.DefaultTenantId, "own-default", Tenant.DefaultTenantId);

        var count = await RequeueAsync();

        Assert.Equal(2, count);
        Assert.Equal([(SharedKey, true), (Tenant.DefaultTenantId, false)], _queries);
        Assert.Contains(("own-default", Tenant.DefaultTenantId, Tenant.DefaultTenantId), _restarts);
    }

    [Fact(DisplayName = "A run under a default tenant with its own database scans the shared database through the default tenant scope")]
    public async Task DefaultTenantWithOwnDatabase_RecurringRunScansBothDatabases()
    {
        var defaultTenant = OwnDatabaseTenant(Tenant.DefaultTenantId);
        UseTenants(defaultTenant);
        _databases["default-scope"] = CreateDatabase("default-scope");
        AddInstance("default-scope", "shared", Tenant.DefaultTenantId);
        AddInstance(Tenant.DefaultTenantId, "own-default", Tenant.DefaultTenantId);

        int count;
        using (_tenantAccessor.PushContext(defaultTenant))
            count = await RequeueAsync();

        Assert.Equal(2, count);
        Assert.Equal([("default-scope", true), (Tenant.DefaultTenantId, false)], _queries);
    }

    [Fact(DisplayName = "A failing shared pass still scans tenants with their own database")]
    public async Task FailingSharedPass_OwnDatabaseTenantsStillScanned()
    {
        var tenant = OwnDatabaseTenant("tenant-own");
        UseTenants(tenant);
        AddInstance(tenant.Id, "own", "tenant-own");
        _databases[SharedKey].Throws = true;

        var count = await RequeueAsync();

        Assert.Equal(1, count);
        Assert.Equal([("own", "tenant-own", "tenant-own")], _restarts);
    }

    [Fact(DisplayName = "A failing tenant listing still runs the shared pass")]
    public async Task FailingTenantListing_SharedPassStillRuns()
    {
        _tenantService.ListAsync(Arg.Any<CancellationToken>()).Returns(Task.FromException<IEnumerable<Tenant>>(new InvalidOperationException("boom")));
        AddInstance(SharedKey, "shared", Tenant.DefaultTenantId);

        var count = await RequeueAsync();

        Assert.Equal(1, count);
    }

    [Fact(DisplayName = "A cancelled run is not swallowed per tenant")]
    public async Task Cancellation_Propagates()
    {
        var tenant = OwnDatabaseTenant("tenant-own");
        UseTenants(tenant);
        using var cts = new CancellationTokenSource();
        AddInstance(tenant.Id, "x", "tenant-own");
        _databases[tenant.Id].OnQuery = cts.Cancel;

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => RequeueAsync(cts.Token));
    }

    [Fact(DisplayName = "Under an own-database ambient tenant the shared pass uses the default tenant scope")]
    public async Task AmbientOwnDatabaseTenant_SharedPassUsesDefaultScope()
    {
        var tenant = OwnDatabaseTenant("tenant-own");
        UseTenants(tenant);
        _databases["default-scope"] = CreateDatabase("default-scope");
        AddInstance("default-scope", "d", Tenant.DefaultTenantId);
        AddInstance(SharedKey, "ignored", Tenant.DefaultTenantId);

        int count;
        using (_tenantAccessor.PushContext(tenant))
            count = await RequeueAsync();

        Assert.Equal(1, count);
        Assert.Equal([Tenant.DefaultTenantId, "tenant-own"], _scopedTenantIds);
        Assert.Equal([("default-scope", true), ("tenant-own", false)], _queries);
        Assert.Equal([("d", Tenant.DefaultTenantId, "default-scope")], _restarts);
    }

    private static Tenant OwnDatabaseTenant(string id) => new()
    {
        Id = id,
        Name = id,
        Configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["ConnectionStrings:Sqlite"] = $"Data Source={id}.db" }).Build()
    };

    private Database CreateDatabase(string name)
    {
        var restarter = Substitute.For<IWorkflowRestarter>();
        restarter
            .When(x => x.RestartWorkflowAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()))
            .Do(ci => _restarts.Add((ci.ArgAt<string>(0), _tenantAccessor.Tenant?.Id, name)));
        return new(name, Substitute.For<IWorkflowInstanceStore>(), restarter);
    }

    private void UseTenants(params Tenant[] tenants)
    {
        foreach (var tenant in tenants.Where(x => x.HasOwnDatabase()))
            _databases[tenant.Id] = CreateDatabase(tenant.Id);

        _tenantService.ListAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult<IEnumerable<Tenant>>(tenants));
    }

    private void AddInstance(string database, string id, string tenantId) =>
        _databases[database].Instances.Add(new WorkflowInstanceSummary { Id = id, TenantId = tenantId });

    private Task<int> RequeueAsync(CancellationToken cancellationToken = default)
    {
        var shared = _databases[SharedKey];
        return OwnDatabaseTenantRecovery.RequeueAsync(
            (store, tenantAgnostic) => Enumerate(store, tenantAgnostic),
            shared.Store,
            shared.Restarter,
            _tenantService,
            _tenantAccessor,
            _scopeFactory,
            _logger,
            cancellationToken);
    }

    private async IAsyncEnumerable<WorkflowInstanceSummary> Enumerate(IWorkflowInstanceStore store, bool tenantAgnostic)
    {
        var database = _databases.Values.Single(x => x.Store == store);
        _queries.Add((database.Name, tenantAgnostic));
        database.OnQuery?.Invoke();

        if (database.Throws)
            throw new InvalidOperationException("boom");

        foreach (var instance in database.Instances.ToList())
            yield return instance;

        await Task.CompletedTask;
    }

    private sealed record Database(string Name, IWorkflowInstanceStore Store, IWorkflowRestarter Restarter)
    {
        public List<WorkflowInstanceSummary> Instances { get; } = [];
        public bool Throws { get; set; }
        public Action? OnQuery { get; set; }
    }
}
