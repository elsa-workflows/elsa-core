using System.Collections.Concurrent;
using Elsa.Common.Features;
using Elsa.Common.Multitenancy;
using Elsa.Common.Services;
using Elsa.Extensions;
using Elsa.Http;
using Elsa.Scheduling;
using Elsa.Tenants.Options;
using Elsa.Testing.Shared;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Helpers;
using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Notifications;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Activities;
using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.Runtime.Filters;
using Elsa.Workflows.Runtime.Stimuli;
using Elsa.Mediator.Contracts;
using Elsa.Persistence.EFCore.Extensions;
using Elsa.Persistence.EFCore.Modules.Management;
using Elsa.Persistence.EFCore.Modules.Runtime;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;
using Timer = Elsa.Scheduling.Activities.Timer;

namespace Elsa.Workflows.IntegrationTests.Scenarios.TenantAgnosticTriggers;

/// <summary>
/// Triggers of tenant-agnostic ("*") workflow definitions on a multitenant host
/// (docs/adr/2026-11-20-tenant-agnostic-workflows-share-one-row-per-unscheduled-trigger.md): one shared row per unscheduled trigger,
/// one row per active tenant per scheduled trigger, tenant overrides, and the activation cleanup.
/// </summary>
public class Tests(ITestOutputHelper testOutputHelper) : IAsyncDisposable
{
    private const string SharedDefinitionId = "shared-definition";
    private static readonly string TimerName = ActivityTypeNameHelper.GenerateTypeName<Timer>();
    private static readonly string EventName = ActivityTypeNameHelper.GenerateTypeName<Event>();
    private readonly RecordingWorkflowScheduler _scheduler = new();
    private readonly MutableTenantsProvider _tenants = new();
    private readonly string _databasePath = Path.Join(Path.GetTempPath(), $"elsa-tenant-agnostic-triggers-{Guid.NewGuid():N}.db");
    private ServiceProvider? _services;

    [Theory(DisplayName = "An HTTP trigger of a tenant-agnostic workflow is stored once and matches exactly one trigger in each tenant")]
    [InlineData(false)]
    [InlineData(true)]
    public async Task HttpTriggerMatchesOncePerTenant(bool registerAgnosticTenant)
    {
        var services = Build(registerAgnosticTenant, elsa => elsa.AddWorkflow<HttpWorkflow>());
        await ActivateAsync(services);

        var row = Assert.Single((await RowsAsync(nameof(HttpWorkflow))));
        Assert.Equal(Tenant.AgnosticTenantId, row.TenantId);

        foreach (var tenantId in new[] { "a", "b" })
        {
            await InTenantAsync(tenantId, async sp =>
            {
                var result = await sp.GetRequiredService<IHttpWorkflowLookupService>().FindWorkflowAsync(row.Hash!);
                Assert.NotNull(result);
                Assert.Single(result.Triggers);
                Assert.NotNull(result.WorkflowGraph);
            });
        }
    }

    [Theory(DisplayName = "A stimulus for a tenant-agnostic workflow starts exactly one instance in the tenant that sends it")]
    [InlineData(false)]
    [InlineData(true)]
    public async Task StimulusStartsOneInstancePerTenant(bool registerAgnosticTenant)
    {
        var services = Build(registerAgnosticTenant, elsa => elsa.AddWorkflow<EventWorkflow>());
        await ActivateAsync(services);

        foreach (var tenantId in new[] { "a", "b" })
        {
            await InTenantAsync(tenantId, async sp =>
            {
                var result = await sp.GetRequiredService<IStimulusSender>().SendAsync(EventName, new EventStimulus(EventWorkflow.EventName));
                Assert.Single(result.WorkflowInstanceResponses);
            });
        }
    }

    [Fact(DisplayName = "A timer of a tenant-agnostic workflow is stored and scheduled once per active tenant, and not for a registered \"*\" tenant")]
    public async Task TimerIsScheduledPerActiveTenant()
    {
        var services = Build(true, elsa => elsa.AddWorkflow<TimerWorkflow>());
        await ActivateAsync(services);

        var rows = (await RowsAsync(nameof(TimerWorkflow)));
        Assert.Equal(["a", "b"], rows.Select(x => x.TenantId).Order());

        foreach (var row in rows)
            Assert.Contains(("schedule", row.Id, row.TenantId!), _scheduler.Calls);

        Assert.DoesNotContain(_scheduler.Calls, x => x.TenantId == Tenant.AgnosticTenantId);
    }

    [Theory(DisplayName = "A republish that removes a timer and an HTTP trigger removes them for all active tenants and unschedules the timers")]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RepublishRemovesTriggersForAllTenants(bool entityFrameworkCore)
    {
        var services = Build(true, entityFrameworkCore: entityFrameworkCore);
        await ActivateAsync(services);
        await PublishSharedAsync(1, new Sequence { Id = "root", Activities = { HttpActivity("/republish"), TimerActivity(), EventActivity("kept") } });
        var timers = (await RowsAsync(SharedDefinitionId)).Where(x => x.Name == TimerName).ToList();
        var httpRow = Assert.Single((await RowsAsync(SharedDefinitionId)), x => x.Name == HttpStimulusNames.HttpEndpoint);
        Assert.Equal(["a", "b"], timers.Select(x => x.TenantId).Order());

        await PublishSharedAsync(2, new Sequence { Id = "root", Activities = { EventActivity("kept") } });

        var row = Assert.Single((await RowsAsync(SharedDefinitionId)));
        Assert.Equal((EventName, Tenant.AgnosticTenantId), (row.Name, row.TenantId));

        foreach (var timer in timers)
            Assert.Contains(("unschedule", timer.Id, timer.TenantId!), _scheduler.Calls);

        foreach (var tenantId in new[] { "a", "b" })
            await InTenantAsync(tenantId, async sp => Assert.Null(await sp.GetRequiredService<IHttpWorkflowLookupService>().FindWorkflowAsync(httpRow.Hash!)));
    }

    [Fact(DisplayName = "A tenant with a published definition of its own with the same ID sees only its own triggers; a draft alone does not override")]
    public async Task OwnPublishedDefinitionOverridesSharedDefinition()
    {
        var services = Build(true);
        await ActivateAsync(services);
        await PublishSharedAsync(1, new Sequence { Id = "root", Activities = { HttpActivity("/override"), TimerActivity() } });
        var httpHash = Assert.Single((await RowsAsync(SharedDefinitionId)), x => x.Name == HttpStimulusNames.HttpEndpoint).Hash!;
        var timerOfA = Assert.Single((await RowsAsync(SharedDefinitionId)), x => x.TenantId == "a");

        var draft = await SaveOwnDraftAsync("a");
        Assert.Single(await FindTriggersAsync("a", httpHash));
        Assert.Single((await RowsAsync(SharedDefinitionId)), x => x.TenantId == "a");

        await PublishOwnDefinitionAsync("a", draft);

        Assert.Empty(await FindTriggersAsync("a", httpHash));
        Assert.Single(await FindTriggersAsync("b", httpHash));
        Assert.Equal([EventName], (await RowsAsync(SharedDefinitionId)).Where(x => x.TenantId == "a").Select(x => x.Name));
        Assert.Contains(("unschedule", timerOfA.Id, "a"), _scheduler.Calls);
        Assert.Single((await RowsAsync(SharedDefinitionId)), x => x.TenantId == "b");
    }

    [Theory(DisplayName = "When a tenant stops overriding a tenant-agnostic definition, its triggers and scheduled rows are restored")]
    [InlineData(false)]
    [InlineData(true)]
    public async Task EndingOverrideRestoresSharedTriggers(bool delete)
    {
        var services = Build(true);
        await ActivateAsync(services);
        await PublishSharedAsync(1, new Sequence { Id = "root", Activities = { HttpActivity("/restore"), TimerActivity() } });
        var httpHash = Assert.Single((await RowsAsync(SharedDefinitionId)), x => x.Name == HttpStimulusNames.HttpEndpoint).Hash!;
        var own = await SaveOwnDraftAsync("a");
        await PublishOwnDefinitionAsync("a", own);
        Assert.Empty(await FindTriggersAsync("a", httpHash));

        await RetractOwnDefinitionAsync("a", own);

        if (delete)
            await InTenantAsync("a", sp => sp.GetRequiredService<IWorkflowDefinitionManager>().DeleteVersionAsync(own, CancellationToken.None));

        Assert.Single(await FindTriggersAsync("a", httpHash));
        var timer = Assert.Single((await RowsAsync(SharedDefinitionId)), x => x.TenantId == "a");
        Assert.Equal(TimerName, timer.Name);
        Assert.Contains(("schedule", timer.Id, "a"), _scheduler.Calls);
    }

    [Fact(DisplayName = "Activation removes and unschedules the scheduled rows of a tenant-agnostic definition deleted while the tenant was inactive")]
    public async Task ActivationRemovesRowsOfDefinitionDeletedWhileInactive()
    {
        var services = Build(true);
        await ActivateAsync(services);
        await PublishSharedAsync(1, new Sequence { Id = "root", Activities = { TimerActivity() } });
        var timerOfB = Assert.Single((await RowsAsync(SharedDefinitionId)), x => x.TenantId == "b");
        var tenantService = services.GetRequiredService<ITenantService>();

        _tenants.Remove("b");
        await tenantService.RefreshAsync();
        await InTenantAsync(Tenant.AgnosticTenantId, async sp =>
        {
            using var grant = SharedTenantWriteScope.Enter<WorkflowDefinition>();
            using var triggerGrant = SharedTenantWriteScope.Enter<StoredTrigger>();
            await sp.GetRequiredService<IWorkflowDefinitionManager>().DeleteByDefinitionIdAsync(SharedDefinitionId);
        });
        Assert.Equal([timerOfB.Id], (await RowsAsync(SharedDefinitionId)).Select(x => x.Id));
        var callsBeforeActivation = _scheduler.Calls.Count;

        _tenants.Add("b");
        await tenantService.RefreshAsync();

        Assert.Empty((await RowsAsync(SharedDefinitionId)));
        Assert.Contains(("unschedule", timerOfB.Id, "b"), _scheduler.Calls.Skip(callsBeforeActivation));
    }

    [Fact(DisplayName = "Activation replaces a tenant's own copies of a shared trigger with the shared row and keeps the rows of its overriding definition")]
    public async Task ActivationCleansUpCopiesButKeepsOverrideRows()
    {
        var services = Build(false);
        var sharedVersion = await SeedDefinitionAsync(Tenant.AgnosticTenantId, "shared-v1", new Event("shared-event") { Id = "event", CanStartWorkflow = true });
        var ownVersion = await SeedDefinitionAsync("a", "a-v1", new Event("own-event") { Id = "event", CanStartWorkflow = true });
        var copyOfA = SeedRow("a", sharedVersion, "shared-event");
        var copyOfB = SeedRow("b", sharedVersion, "shared-event");
        var ownRowOfA = SeedRow("a", ownVersion, "own-event");

        await ActivateAsync(services);

        var rows = await RowsAsync(SharedDefinitionId);
        Assert.DoesNotContain(rows, x => x.Id == copyOfA.Id || x.Id == copyOfB.Id);
        Assert.Contains(rows, x => x.Id == ownRowOfA.Id);
        Assert.Single(rows, x => x.TenantId == Tenant.AgnosticTenantId && x.WorkflowDefinitionVersionId == sharedVersion.Id);
        Assert.Single(await FindTriggersAsync("b", copyOfB.Hash!));
        Assert.Empty(await FindTriggersAsync("a", copyOfA.Hash!));
        Assert.Single(await FindTriggersAsync("a", ownRowOfA.Hash!));
    }

    [Fact(DisplayName = "Activation of a registered \"*\" tenant removes and unschedules its scheduled rows of tenant-agnostic definitions")]
    public async Task ActivationOfAgnosticTenantRemovesItsScheduledRows()
    {
        var services = Build(true);
        var sharedVersion = await SeedDefinitionAsync(Tenant.AgnosticTenantId, "shared-v1", TimerActivity());
        var legacyTimer = new StoredTrigger
        {
            Id = "legacy-timer",
            TenantId = Tenant.AgnosticTenantId,
            WorkflowDefinitionId = SharedDefinitionId,
            WorkflowDefinitionVersionId = sharedVersion.Id,
            Name = TimerName,
            ActivityId = "timer"
        };
        services.GetRequiredService<MemoryStore<StoredTrigger>>().Save(legacyTimer, x => x.Id);

        await ActivateAsync(services);

        Assert.Equal(["a", "b"], (await RowsAsync(SharedDefinitionId)).Select(x => x.TenantId).Order());
        Assert.Contains(("unschedule", legacyTimer.Id, Tenant.AgnosticTenantId), _scheduler.Calls);
    }

    [Fact(DisplayName = "Activating several tenants derives the triggers of a tenant-agnostic definition version once")]
    public async Task ActivationDerivesSharedTriggersOncePerProcess()
    {
        _tenants.Add("c");
        var services = Build(true, elsa => elsa.AddActivity<CountingTrigger>());
        await SeedDefinitionAsync(Tenant.AgnosticTenantId, "shared-v1", new Sequence { Id = "root", Activities = { new CountingTrigger { Id = "counting", CanStartWorkflow = true }, TimerActivity() } });
        CountingTrigger.Reset();

        await ActivateAsync(services);

        Assert.Equal(1, CountingTrigger.Derivations);
        var rows = await RowsAsync(SharedDefinitionId);
        Assert.Single(rows, x => x.TenantId == Tenant.AgnosticTenantId);
        Assert.Equal(["a", "b", "c"], rows.Where(x => x.Name == TimerName).Select(x => x.TenantId).Order());
    }

    public async ValueTask DisposeAsync()
    {
        if (_services != null)
            await _services.DisposeAsync();

        SqliteConnection.ClearAllPools();

        if (File.Exists(_databasePath))
            File.Delete(_databasePath);
    }

    /// <param name="registerAgnosticTenant">Whether the host registers a "*" tenant.</param>
    /// <param name="configure">Configures Elsa further.</param>
    /// <param name="entityFrameworkCore">Whether definitions and triggers are stored with EF Core, which enforces shared-write authority, instead of in memory.</param>
    private ServiceProvider Build(bool registerAgnosticTenant, Action<Elsa.Features.Services.IModule>? configure = null, bool entityFrameworkCore = false)
    {
        var connectionString = $"Data Source={_databasePath};Pooling=False";

        if (registerAgnosticTenant)
            _tenants.Add(Tenant.AgnosticTenantId);

        _tenants.Add("a");
        _tenants.Add("b");

        _services = (ServiceProvider)new TestApplicationBuilder(testOutputHelper)
            .ConfigureServices(services => services.Configure<TenantsOptions>(options => options.IsEnabled = true))
            .ConfigureElsa(elsa =>
            {
                elsa
                    .UseHttp()
                    .UseScheduling(scheduling => scheduling.WorkflowScheduler = _ => _scheduler)
                    .Configure<MultitenancyFeature>(feature => feature.UseTenantsProvider(_ => _tenants));

                if (entityFrameworkCore)
                {
                    elsa
                        .UseWorkflowManagement(management => management.UseWorkflowDefinitions(definitions => definitions.UseEntityFrameworkCore(ef => ef.UseSqlite(connectionString))))
                        .UseWorkflowRuntime(runtime => runtime.UseEntityFrameworkCore(ef => ef.UseSqlite(connectionString)));
                }

                configure?.Invoke(elsa);
            })
            .Build();
        return _services;
    }

    private static Task ActivateAsync(IServiceProvider services) => services.GetRequiredService<ITenantService>().ActivateTenantsAsync();

    /// <summary>
    /// Returns the trigger rows of the definition of every tenant.
    /// </summary>
    private async Task<List<StoredTrigger>> RowsAsync(string definitionId)
    {
        using var scope = _services!.CreateScope();
        var filter = new TriggerFilter { WorkflowDefinitionId = definitionId, TenantAgnostic = true };
        return (await scope.ServiceProvider.GetRequiredService<ITriggerStore>().FindManyAsync(filter)).ToList();
    }

    private async Task InTenantAsync(string tenantId, Func<IServiceProvider, Task> action)
    {
        await using var scope = _services!.GetRequiredService<ITenantScopeFactory>().CreateScope(new Tenant { Id = tenantId, Name = tenantId });
        await action(scope.ServiceProvider);
    }

    private async Task<List<StoredTrigger>> FindTriggersAsync(string tenantId, string hash)
    {
        var triggers = new List<StoredTrigger>();
        await InTenantAsync(tenantId, async sp => triggers.AddRange(await sp.GetRequiredService<ITriggerStore>().FindManyAsync(new TriggerFilter { Hash = hash })));
        return triggers;
    }

    /// <summary>
    /// Publishes a version of the shared definition the way a host does that authors shared definitions in a "*" session.
    /// </summary>
    private Task PublishSharedAsync(int version, IActivity root) => InTenantAsync(Tenant.AgnosticTenantId, async sp =>
    {
        using var definitionGrant = SharedTenantWriteScope.Enter<WorkflowDefinition>();
        using var triggerGrant = SharedTenantWriteScope.Enter<StoredTrigger>();
        var publisher = sp.GetRequiredService<IWorkflowDefinitionPublisher>();
        var definition = publisher.New(root);
        definition.DefinitionId = SharedDefinitionId;
        definition.Version = version;
        definition.TenantId = Tenant.AgnosticTenantId;
        var result = await publisher.PublishAsync(definition);
        Assert.True(result.Succeeded);
    });

    /// <summary>
    /// Saves a draft of the tenant's own definition with the ID of the shared definition. It is saved and published through the store and
    /// the publication notifications, because the publisher would also retract the shared versions that the tenant sees.
    /// </summary>
    private async Task<WorkflowDefinition> SaveOwnDraftAsync(string tenantId)
    {
        WorkflowDefinition definition = null!;
        await InTenantAsync(tenantId, async sp =>
        {
            definition = sp.GetRequiredService<IWorkflowDefinitionPublisher>().New(EventActivity("own"));
            definition.DefinitionId = SharedDefinitionId;
            definition.TenantId = tenantId;
            await sp.GetRequiredService<IWorkflowDefinitionStore>().SaveAsync(definition);
        });
        return definition;
    }

    private Task PublishOwnDefinitionAsync(string tenantId, WorkflowDefinition definition) => InTenantAsync(tenantId, async sp =>
    {
        definition.IsPublished = true;
        await sp.GetRequiredService<IWorkflowDefinitionStore>().SaveAsync(definition);
        await sp.GetRequiredService<INotificationSender>().SendAsync(new WorkflowDefinitionPublished(definition, new([])));
    });

    private Task RetractOwnDefinitionAsync(string tenantId, WorkflowDefinition definition) => InTenantAsync(tenantId, async sp =>
    {
        definition.IsPublished = false;
        await sp.GetRequiredService<IWorkflowDefinitionStore>().SaveAsync(definition);
        await sp.GetRequiredService<INotificationSender>().SendAsync(new WorkflowDefinitionRetracted(definition));
    });

    private async Task<WorkflowDefinition> SeedDefinitionAsync(string tenantId, string versionId, IActivity root)
    {
        WorkflowDefinition definition = null!;
        await InTenantAsync(tenantId, async sp =>
        {
            using var grant = SharedTenantWriteScope.Enter<WorkflowDefinition>();
            definition = sp.GetRequiredService<IWorkflowDefinitionPublisher>().New(root);
            definition.Id = versionId;
            definition.DefinitionId = SharedDefinitionId;
            definition.TenantId = tenantId;
            definition.IsPublished = true;
            await sp.GetRequiredService<IWorkflowDefinitionStore>().SaveAsync(definition);
        });
        return definition;
    }

    /// <summary>
    /// Stores the row that the trigger indexer wrote before tenant-agnostic definitions shared their unscheduled triggers.
    /// </summary>
    private StoredTrigger SeedRow(string tenantId, WorkflowDefinition definition, string eventName)
    {
        var hasher = _services!.GetRequiredService<IStimulusHasher>();
        var row = new StoredTrigger
        {
            Id = $"{tenantId}-{definition.Id}",
            TenantId = tenantId,
            WorkflowDefinitionId = definition.DefinitionId,
            WorkflowDefinitionVersionId = definition.Id,
            Name = EventName,
            ActivityId = "event",
            Hash = hasher.Hash(EventName, new EventStimulus(eventName)),
            Payload = new EventStimulus(eventName)
        };
        _services!.GetRequiredService<MemoryStore<StoredTrigger>>().Save(row, x => x.Id);
        return row;
    }

    private static HttpEndpoint HttpActivity(string path) => new() { Id = "http", Path = new(path), CanStartWorkflow = true };
    private static Timer TimerActivity() => new(TimeSpan.FromHours(1)) { Id = "timer", CanStartWorkflow = true };
    private static Event EventActivity(string name) => new(name) { Id = $"event-{name}", CanStartWorkflow = true };

    /// <summary>
    /// A trigger that counts how often the trigger indexer derives its payload.
    /// </summary>
    public class CountingTrigger : Trigger
    {
        private static int _derivations;

        public static int Derivations => _derivations;

        public static void Reset() => Interlocked.Exchange(ref _derivations, 0);

        protected override object GetTriggerPayload(TriggerIndexingContext context)
        {
            Interlocked.Increment(ref _derivations);
            return new EventStimulus("counted");
        }
    }

    private class HttpWorkflow : WorkflowBase
    {
        protected override void Build(IWorkflowBuilder builder) => builder.Root = new HttpEndpoint { Path = new("/tenant-agnostic-http"), CanStartWorkflow = true };
    }

    private class EventWorkflow : WorkflowBase
    {
        public const string EventName = "tenant-agnostic-event";
        protected override void Build(IWorkflowBuilder builder) => builder.Root = new Event(EventName) { CanStartWorkflow = true };
    }

    private class TimerWorkflow : WorkflowBase
    {
        protected override void Build(IWorkflowBuilder builder) => builder.Root = new Timer(TimeSpan.FromHours(1)) { CanStartWorkflow = true };
    }

    private sealed class MutableTenantsProvider : ITenantsProvider
    {
        private readonly ConcurrentDictionary<string, Tenant> _tenants = new();

        public void Add(string id) => _tenants[id] = new() { Id = id, Name = id };
        public void Remove(string id) => _tenants.TryRemove(id, out _);

        public Task<IEnumerable<Tenant>> ListAsync(CancellationToken cancellationToken = default) => Task.FromResult<IEnumerable<Tenant>>(_tenants.Values.ToList());

        public Task<Tenant?> FindAsync(TenantFilter filter, CancellationToken cancellationToken = default) =>
            Task.FromResult(filter.Apply(_tenants.Values.AsQueryable()).FirstOrDefault());
    }

    /// <summary>
    /// Records the tasks that the trigger scheduler schedules and unschedules, with the tenant they are scheduled in.
    /// </summary>
    private sealed class RecordingWorkflowScheduler : IWorkflowScheduler
    {
        private readonly DefaultTenantAccessor _tenantAccessor = new();
        private readonly ConcurrentQueue<(string Action, string TaskName, string TenantId)> _calls = new();

        public IReadOnlyList<(string Action, string TaskName, string TenantId)> Calls => _calls.ToList();

        public ValueTask ScheduleAtAsync(string taskName, ScheduleNewWorkflowInstanceRequest request, DateTimeOffset at, CancellationToken cancellationToken = default) => Record("schedule", taskName);
        public ValueTask ScheduleAtAsync(string taskName, ScheduleExistingWorkflowInstanceRequest request, DateTimeOffset at, CancellationToken cancellationToken = default) => Record("schedule", taskName);
        public ValueTask ScheduleRecurringAsync(string taskName, ScheduleNewWorkflowInstanceRequest request, DateTimeOffset startAt, TimeSpan interval, CancellationToken cancellationToken = default) => Record("schedule", taskName);
        public ValueTask ScheduleRecurringAsync(string taskName, ScheduleExistingWorkflowInstanceRequest request, DateTimeOffset startAt, TimeSpan interval, CancellationToken cancellationToken = default) => Record("schedule", taskName);
        public ValueTask ScheduleCronAsync(string taskName, ScheduleNewWorkflowInstanceRequest request, string cronExpression, CancellationToken cancellationToken = default) => Record("schedule", taskName);
        public ValueTask ScheduleCronAsync(string taskName, ScheduleExistingWorkflowInstanceRequest request, string cronExpression, CancellationToken cancellationToken = default) => Record("schedule", taskName);
        public ValueTask UnscheduleAsync(string taskName, CancellationToken cancellationToken = default) => Record("unschedule", taskName);

        private ValueTask Record(string action, string taskName)
        {
            _calls.Enqueue((action, taskName, _tenantAccessor.TenantId));
            return ValueTask.CompletedTask;
        }
    }
}
