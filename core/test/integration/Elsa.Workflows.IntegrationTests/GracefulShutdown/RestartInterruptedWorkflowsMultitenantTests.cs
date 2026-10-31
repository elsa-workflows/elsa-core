using Elsa.Common;
using Elsa.Common.Features;
using Elsa.Common.Multitenancy;
using Elsa.Common.RecurringTasks;
using Elsa.Common.Services;
using Elsa.Extensions;
using Elsa.Features.Abstractions;
using Elsa.Features.Attributes;
using Elsa.Features.Services;
using Elsa.Testing.Shared;
using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Stores;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Features;
using Elsa.Workflows.Runtime.Options;
using Elsa.Workflows.Runtime.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xunit.Abstractions;

namespace Elsa.Workflows.IntegrationTests.GracefulShutdown;

/// <summary>
/// Integration tests for the multitenant interrupted-workflow recovery: the scan that runs once after all tenants are
/// activated and the <see cref="RestartInterruptedWorkflowsTask"/> that runs once per recurring cycle. Each run must
/// requeue every tenant's instances exactly once, under the instance's own tenant. The two flows select disjoint rows
/// (Interrupted and not executing versus Executing and stale), so each test only observes its own flow.
/// </summary>
public class RestartInterruptedWorkflowsMultitenantTests : IAsyncDisposable
{

    private const string OwnTenantId = "tenant-own";

    private static readonly string[] Expected =
    [
        "instance-a@tenant-a",
        "instance-agnostic@" + Tenant.DefaultTenantId,
        "instance-b@tenant-b",
        "instance-default@" + Tenant.DefaultTenantId,
    ];

    private readonly IServiceProvider _services;
    private readonly RecordingRestarter _restarter = new();
    private readonly MemoryWorkflowInstanceStore _ownStore = new(new MemoryStore<WorkflowInstance>(), new FixedTenantAccessor(OwnTenantId));
    private readonly ManualSchedule _schedule = new();

    public RestartInterruptedWorkflowsMultitenantTests(ITestOutputHelper testOutputHelper)
    {
        _services = new TestApplicationBuilder(testOutputHelper)
            .ConfigureElsa(elsa =>
            {
                elsa.UseWorkflowRuntime();
                elsa.Configure<MultitenancyFeature>(feature => feature.UseTenantsProvider(_ => new FixedTenantsProvider("tenant-a", "tenant-b", OwnTenantId)));
                elsa.Use<RecordingRestarterFeature>(feature => { feature.Restarter = _restarter; feature.OwnDatabaseStore = _ownStore; });
            })
            .Build();
    }

    [Fact(DisplayName = "ActivateTenantsAsync requeues each Interrupted instance once, under its own tenant")]
    public async Task RecoveryScanRequeuesEachInstanceOnceUnderItsOwnTenant()
    {
        await SeedAllAsync(WorkflowSubStatus.Interrupted, false, DateTimeOffset.UtcNow);

        await _services.GetRequiredService<ITenantService>().ActivateTenantsAsync();

        Assert.Equal(Expected, _restarter.Snapshot().Select(x => $"{x.InstanceId}@{x.TenantId}").Order());
    }

    [Fact(DisplayName = "One cycle restarts each stale instance once, under its own tenant")]
    public async Task RestartsEachStaleInstanceOnceUnderItsOwnTenantPerCycle()
    {
        using (var scope = _services.CreateScope())
        {
            var clock = scope.ServiceProvider.GetRequiredService<ISystemClock>();
            var threshold = scope.ServiceProvider.GetRequiredService<IOptions<RuntimeOptions>>().Value.InactivityThreshold;
            await SeedAllAsync(WorkflowSubStatus.Executing, true, clock.UtcNow - threshold - TimeSpan.FromMinutes(1));
        }

        _services.GetRequiredService<RecurringTaskScheduleManager>().ScheduledTasks[typeof(RestartInterruptedWorkflowsTask)] = _schedule;
        await _services.GetRequiredService<ITenantService>().ActivateTenantsAsync();

        await _schedule.TickAsync();
        await _restarter.WaitForRestartsAsync(Expected.Length);
        await DisposeAsync();

        Assert.Equal(Expected, _restarter.Snapshot().Select(x => $"{x.InstanceId}@{x.TenantId}").Order());
    }

    [Fact(DisplayName = "ActivateTenantsAsync requeues an Interrupted instance of a tenant with its own database once, under that tenant")]
    public async Task RecoveryScanRequeuesOwnDatabaseTenantInstanceOnce()
    {
        await SeedOwnDatabaseAsync(WorkflowSubStatus.Interrupted, false, DateTimeOffset.UtcNow);

        await _services.GetRequiredService<ITenantService>().ActivateTenantsAsync();

        Assert.Equal(["instance-own@" + OwnTenantId], _restarter.Snapshot().Select(x => $"{x.InstanceId}@{x.TenantId}"));
    }

    [Fact(DisplayName = "One cycle restarts a stale instance of a tenant with its own database once, under that tenant")]
    public async Task RestartsOwnDatabaseTenantInstanceOncePerCycle()
    {
        using (var scope = _services.CreateScope())
        {
            var clock = scope.ServiceProvider.GetRequiredService<ISystemClock>();
            var threshold = scope.ServiceProvider.GetRequiredService<IOptions<RuntimeOptions>>().Value.InactivityThreshold;
            await SeedOwnDatabaseAsync(WorkflowSubStatus.Executing, true, clock.UtcNow - threshold - TimeSpan.FromMinutes(1));
        }

        _services.GetRequiredService<RecurringTaskScheduleManager>().ScheduledTasks[typeof(RestartInterruptedWorkflowsTask)] = _schedule;
        await _services.GetRequiredService<ITenantService>().ActivateTenantsAsync();

        await _schedule.TickAsync();
        await _restarter.WaitForRestartsAsync(1);
        await DisposeAsync();

        Assert.Equal(["instance-own@" + OwnTenantId], _restarter.Snapshot().Select(x => $"{x.InstanceId}@{x.TenantId}"));
    }

    public async ValueTask DisposeAsync()
    {
        if (_services is IAsyncDisposable disposable)
            await disposable.DisposeAsync();
    }

    private async Task SeedAllAsync(WorkflowSubStatus subStatus, bool isExecuting, DateTimeOffset timestamp)
    {
        using var scope = _services.CreateScope();
        var store = scope.ServiceProvider.GetRequiredService<IWorkflowInstanceStore>();
        await SeedInstanceAsync(store, "instance-a", "tenant-a", WorkflowStatus.Running, subStatus, isExecuting, timestamp);
        await SeedInstanceAsync(store, "instance-b", "tenant-b", WorkflowStatus.Running, subStatus, isExecuting, timestamp);
        await SeedInstanceAsync(store, "instance-agnostic", Tenant.AgnosticTenantId, WorkflowStatus.Running, subStatus, isExecuting, timestamp);
        await SeedInstanceAsync(store, "instance-default", Tenant.DefaultTenantId, WorkflowStatus.Running, subStatus, isExecuting, timestamp);
    }

    private Task SeedOwnDatabaseAsync(WorkflowSubStatus subStatus, bool isExecuting, DateTimeOffset timestamp) =>
        SeedInstanceAsync(_ownStore, "instance-own", OwnTenantId, WorkflowStatus.Running, subStatus, isExecuting, timestamp);

    private static async Task SeedInstanceAsync(IWorkflowInstanceStore store, string id, string tenantId, WorkflowStatus status, WorkflowSubStatus subStatus, bool isExecuting, DateTimeOffset timestamp)
    {
        await store.SaveAsync(new WorkflowInstance
        {
            Id = id,
            TenantId = tenantId,
            DefinitionId = "def-1",
            DefinitionVersionId = "ver-1",
            Version = 1,
            Status = status,
            SubStatus = subStatus,
            IsExecuting = isExecuting,
            CreatedAt = timestamp,
            UpdatedAt = timestamp,
            WorkflowState = new Workflows.State.WorkflowState
            {
                Id = id,
                DefinitionId = "def-1",
                DefinitionVersionId = "ver-1",
                Status = status,
                SubStatus = subStatus,
            },
        }, CancellationToken.None);
    }

    [DependsOn(typeof(WorkflowRuntimeFeature))]
    private sealed class RecordingRestarterFeature(IModule module) : FeatureBase(module)
    {
        public RecordingRestarter Restarter { get; set; } = null!;
        public IWorkflowInstanceStore OwnDatabaseStore { get; set; } = null!;

        public override void Apply()
        {
            Services.Replace(ServiceDescriptor.Scoped<IWorkflowRestarter>(sp => Restarter.Bind(sp.GetRequiredService<ITenantAccessor>())));
            Services.Replace(ServiceDescriptor.Scoped<IWorkflowInstanceStore>(sp => sp.GetRequiredService<ITenantAccessor>().Tenant?.Configuration["ConnectionStrings:Sqlite"] is null ? sp.GetRequiredService<MemoryWorkflowInstanceStore>() : OwnDatabaseStore));
        }
    }

    private sealed class ManualSchedule : ISchedule
    {
        private volatile Func<Task>? _action;

        public Task TickAsync() => (_action?.Invoke() ?? Task.CompletedTask).WaitAsync(TimeSpan.FromSeconds(5));

        public ScheduledTimer CreateTimer(Func<Task> action, ILogger? logger = null)
        {
            _action = action;
            return new(action, () => Timeout.InfiniteTimeSpan, logger);
        }
    }

    private sealed class FixedTenantAccessor(string tenantId) : ITenantAccessor
    {
        public string TenantId => tenantId;

        public Tenant? Tenant => null;

        public IDisposable PushContext(Tenant? tenant) => throw new NotSupportedException();
    }

    private sealed class FixedTenantsProvider(params string[] tenantIds) : ITenantsProvider
    {
        public Task<IEnumerable<Tenant>> ListAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(tenantIds.Select(id => new Tenant { Id = id, Name = id, Configuration = ConfigurationFor(id) }));
        }

        private static IConfiguration ConfigurationFor(string id) =>
            new ConfigurationBuilder().AddInMemoryCollection(id == OwnTenantId ? new Dictionary<string, string?> { ["ConnectionStrings:Sqlite"] = "Data Source=own.db" } : new Dictionary<string, string?>()).Build();

        public async Task<Tenant?> FindAsync(TenantFilter filter, CancellationToken cancellationToken = default)
        {
            var tenants = await ListAsync(cancellationToken);
            return filter.Apply(tenants.AsQueryable()).FirstOrDefault();
        }
    }

    /// <summary>Records each restart together with the tenant that is current when it happens. Safe for concurrent tenants.</summary>
    private sealed class RecordingRestarter : IWorkflowRestarter
    {
        private readonly object _lock = new();
        private readonly List<(string InstanceId, string? TenantId)> _calls = [];
        private readonly SemaphoreSlim _restarted = new(0);
        private ITenantAccessor? _tenantAccessor;

        public async Task WaitForRestartsAsync(int count)
        {
            for (var i = 0; i < count; i++)
                Assert.True(await _restarted.WaitAsync(TimeSpan.FromSeconds(10)));
        }

        public (string InstanceId, string? TenantId)[] Snapshot()
        {
            lock (_lock)
                return _calls.ToArray();
        }

        public RecordingRestarter Bind(ITenantAccessor tenantAccessor)
        {
            _tenantAccessor = tenantAccessor;
            return this;
        }

        public Task RestartWorkflowAsync(string workflowInstanceId, CancellationToken cancellationToken = default)
        {
            var tenantId = _tenantAccessor?.Tenant?.Id;

            lock (_lock)
                _calls.Add((workflowInstanceId, tenantId));

            _restarted.Release();
            return Task.CompletedTask;
        }
    }
}
