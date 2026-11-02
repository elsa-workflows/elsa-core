using System.Reflection;
using Elsa.Common;
using Elsa.Common.Models;
using Elsa.Common.Multitenancy;
using Elsa.Common.RecurringTasks;
using Elsa.Common.Services;
using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Management.Models;
using Elsa.Workflows.Management.Stores;
using Elsa.Workflows.Runtime.Options;
using Elsa.Workflows.Runtime.StartupTasks;
using Elsa.Workflows.Runtime.Tasks;
using Elsa.Workflows.State;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Elsa.Workflows.Runtime.UnitTests.Services;

public class RestartInterruptedWorkflowsTaskTests
{
    [Fact(DisplayName = "ExecuteAsync restarts each workflow within its tenant context")]
    public async Task ExecuteAsync_RestartsWithinWorkflowTenantContext()
    {
        var workflowInstanceStore = Substitute.For<IWorkflowInstanceStore>();
        var tenantAccessor = new DefaultTenantAccessor();
        var tenantService = Substitute.For<ITenantService>();
        var workflowRestarter = Substitute.For<IWorkflowRestarter>();
        var clock = Substitute.For<ISystemClock>();
        var logger = Substitute.For<ILogger<RestartInterruptedWorkflowsTask>>();
        var now = DateTimeOffset.Parse("2026-04-14T12:00:00Z");
        var workflowInstances = new List<WorkflowInstanceSummary>
        {
            CreateWorkflowInstance("workflow-1", "tenant-a", now),
            CreateWorkflowInstance("workflow-2", "tenant-b", now)
        };
        var observedTenantIds = new List<string?>();

        clock.UtcNow.Returns(now);
        tenantService.FindAsync("tenant-a", Arg.Any<CancellationToken>()).Returns(new Tenant { Id = "tenant-a", Name = "Tenant A" });
        tenantService.FindAsync("tenant-b", Arg.Any<CancellationToken>()).Returns(new Tenant { Id = "tenant-b", Name = "Tenant B" });
        workflowInstanceStore
            .SummarizeManyAsync(Arg.Any<WorkflowInstanceFilter>(), Arg.Any<PageArgs>(), Arg.Any<CancellationToken>())
            .Returns(
                callInfo =>
                {
                    var pageArgs = callInfo.ArgAt<PageArgs>(1);
                    var items = pageArgs.Offset == 0 ? workflowInstances : new List<WorkflowInstanceSummary>();
                    return new ValueTask<Page<WorkflowInstanceSummary>>(Page.Of(items, workflowInstances.Count));
                });
        workflowRestarter
            .When(x => x.RestartWorkflowAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()))
            .Do(_ => observedTenantIds.Add(tenantAccessor.Tenant?.Id));

        var options = Microsoft.Extensions.Options.Options.Create(new RuntimeOptions
        {
            RestartInterruptedWorkflowsBatchSize = 10,
            InactivityThreshold = TimeSpan.FromMinutes(5)
        });
        var task = new RestartInterruptedWorkflowsTask(workflowRestarter, workflowInstanceStore, logger, options, clock, tenantService, tenantAccessor);

        await task.ExecuteAsync(CancellationToken.None);

        Assert.Equal(new[] { "tenant-a", "tenant-b" }, observedTenantIds);
        Assert.Null(tenantAccessor.Tenant);
        await workflowInstanceStore.Received().SummarizeManyAsync(
            Arg.Is<WorkflowInstanceFilter>(f => f.TenantAgnostic && f.IsExecuting == true && f.WorkflowStatus == WorkflowStatus.Running && f.BeforeLastUpdated == now - TimeSpan.FromMinutes(5)),
            Arg.Any<PageArgs>(),
            Arg.Any<CancellationToken>());
        await workflowRestarter.Received(1).RestartWorkflowAsync("workflow-1", Arg.Any<CancellationToken>());
        await workflowRestarter.Received(1).RestartWorkflowAsync("workflow-2", Arg.Any<CancellationToken>());
    }

    [Theory(DisplayName = "ExecuteAsync restarts default or agnostic tenant instances under the default tenant context")]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("*")]
    public async Task ExecuteAsync_DefaultOrAgnosticTenant_PushesDefaultTenantContext(string? tenantId)
    {
        var workflowInstanceStore = Substitute.For<IWorkflowInstanceStore>();
        var tenantAccessor = new DefaultTenantAccessor();
        var tenantService = Substitute.For<ITenantService>();
        var workflowRestarter = Substitute.For<IWorkflowRestarter>();
        var clock = Substitute.For<ISystemClock>();
        var logger = Substitute.For<ILogger<RestartInterruptedWorkflowsTask>>();
        var now = DateTimeOffset.Parse("2026-04-14T12:00:00Z");
        var workflowInstances = new List<WorkflowInstanceSummary>
        {
            CreateWorkflowInstance("workflow-1", tenantId, now)
        };
        var observedTenantIds = new List<string?>();

        clock.UtcNow.Returns(now);
        workflowInstanceStore
            .SummarizeManyAsync(Arg.Any<WorkflowInstanceFilter>(), Arg.Any<PageArgs>(), Arg.Any<CancellationToken>())
            .Returns(
                callInfo =>
                {
                    var pageArgs = callInfo.ArgAt<PageArgs>(1);
                    var items = pageArgs.Offset == 0 ? workflowInstances : new List<WorkflowInstanceSummary>();
                    return new ValueTask<Page<WorkflowInstanceSummary>>(Page.Of(items, workflowInstances.Count));
                });
        workflowRestarter
            .When(x => x.RestartWorkflowAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()))
            .Do(_ => observedTenantIds.Add(tenantAccessor.Tenant?.Id));

        var options = Microsoft.Extensions.Options.Options.Create(new RuntimeOptions
        {
            RestartInterruptedWorkflowsBatchSize = 10,
            InactivityThreshold = TimeSpan.FromMinutes(5)
        });
        var task = new RestartInterruptedWorkflowsTask(workflowRestarter, workflowInstanceStore, logger, options, clock, tenantService, tenantAccessor);

        await task.ExecuteAsync(CancellationToken.None);

        Assert.Equal(new string?[] { Tenant.DefaultTenantId }, observedTenantIds);
        Assert.Null(tenantAccessor.Tenant);
        await workflowRestarter.Received(1).RestartWorkflowAsync("workflow-1", Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "ExecuteAsync continues restarting later workflows after a failure")]
    public async Task ExecuteAsync_ContinuesAfterFailure()
    {
        var workflowInstanceStore = Substitute.For<IWorkflowInstanceStore>();
        var tenantAccessor = new DefaultTenantAccessor();
        var tenantService = Substitute.For<ITenantService>();
        var workflowRestarter = Substitute.For<IWorkflowRestarter>();
        var clock = Substitute.For<ISystemClock>();
        var logger = Substitute.For<ILogger<RestartInterruptedWorkflowsTask>>();
        var now = DateTimeOffset.Parse("2026-04-14T12:00:00Z");
        var workflowInstances = new List<WorkflowInstanceSummary>
        {
            CreateWorkflowInstance("workflow-1", "tenant-a", now),
            CreateWorkflowInstance("workflow-2", "tenant-b", now)
        };

        clock.UtcNow.Returns(now);
        tenantService.FindAsync("tenant-a", Arg.Any<CancellationToken>()).Returns(_ => Task.FromException<Tenant?>(new InvalidOperationException("Transient tenant lookup failure")));
        tenantService.FindAsync("tenant-b", Arg.Any<CancellationToken>()).Returns(new Tenant { Id = "tenant-b", Name = "Tenant B" });
        workflowInstanceStore
            .SummarizeManyAsync(Arg.Any<WorkflowInstanceFilter>(), Arg.Any<PageArgs>(), Arg.Any<CancellationToken>())
            .Returns(
                callInfo =>
                {
                    var pageArgs = callInfo.ArgAt<PageArgs>(1);
                    var items = pageArgs.Offset == 0 ? workflowInstances : new List<WorkflowInstanceSummary>();
                    return new ValueTask<Page<WorkflowInstanceSummary>>(Page.Of(items, workflowInstances.Count));
                });

        var options = Microsoft.Extensions.Options.Options.Create(new RuntimeOptions
        {
            RestartInterruptedWorkflowsBatchSize = 10,
            InactivityThreshold = TimeSpan.FromMinutes(5)
        });
        var task = new RestartInterruptedWorkflowsTask(workflowRestarter, workflowInstanceStore, logger, options, clock, tenantService, tenantAccessor);

        await task.ExecuteAsync(CancellationToken.None);

        await workflowRestarter.DidNotReceive().RestartWorkflowAsync("workflow-1", Arg.Any<CancellationToken>());
        await workflowRestarter.Received(1).RestartWorkflowAsync("workflow-2", Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "ExecuteAsync restarts tenant-specific workflows when tenant services are unavailable")]
    public async Task ExecuteAsync_WithoutTenantServices_RestartsWorkflow()
    {
        var workflowInstanceStore = Substitute.For<IWorkflowInstanceStore>();
        var workflowRestarter = Substitute.For<IWorkflowRestarter>();
        var clock = Substitute.For<ISystemClock>();
        var logger = Substitute.For<ILogger<RestartInterruptedWorkflowsTask>>();
        var now = DateTimeOffset.Parse("2026-04-14T12:00:00Z");
        var workflowInstances = new List<WorkflowInstanceSummary>
        {
            CreateWorkflowInstance("workflow-1", "tenant-a", now)
        };

        clock.UtcNow.Returns(now);
        workflowInstanceStore
            .SummarizeManyAsync(Arg.Any<WorkflowInstanceFilter>(), Arg.Any<PageArgs>(), Arg.Any<CancellationToken>())
            .Returns(
                callInfo =>
                {
                    var pageArgs = callInfo.ArgAt<PageArgs>(1);
                    var items = pageArgs.Offset == 0 ? workflowInstances : new List<WorkflowInstanceSummary>();
                    return new ValueTask<Page<WorkflowInstanceSummary>>(Page.Of(items, workflowInstances.Count));
                });

        var options = Microsoft.Extensions.Options.Options.Create(new RuntimeOptions
        {
            RestartInterruptedWorkflowsBatchSize = 10,
            InactivityThreshold = TimeSpan.FromMinutes(5)
        });
        var task = new RestartInterruptedWorkflowsTask(workflowRestarter, workflowInstanceStore, logger, options, clock);

        await task.ExecuteAsync(CancellationToken.None);

        await workflowRestarter.Received(1).RestartWorkflowAsync("workflow-1", Arg.Any<CancellationToken>());
    }

    [Theory(DisplayName = "Recovery tasks take a host-wide single-node lock; the outbox task takes none")]
    [InlineData(typeof(RestartInterruptedWorkflowsTask), SingleNodeTaskScope.Host)]
    [InlineData(typeof(RecoverInterruptedWorkflowsStartupTask), SingleNodeTaskScope.Host)]
    [InlineData(typeof(ProcessWorkflowDispatchOutboxRecurringTask), null)]
    public void SingleNodeTaskScope_PerTask(Type taskType, SingleNodeTaskScope? expectedScope)
    {
        Assert.Equal(expectedScope, taskType.GetCustomAttribute<SingleNodeTaskAttribute>()?.Scope);
    }

    [Fact(DisplayName = "The restart task runs once per cycle for all tenants")]
    public void RestartTask_IsRunOnceForAllTenants()
    {
        Assert.NotNull(typeof(RestartInterruptedWorkflowsTask).GetCustomAttribute<RunOnceForAllTenantsAttribute>());
    }

    [Fact(DisplayName = "ExecuteAsync also restarts a tenant with its own database from its own scope")]
    public async Task ExecuteAsync_OwnDatabaseTenant_ScannedSeparately()
    {
        var now = DateTimeOffset.Parse("2026-04-14T12:00:00Z");
        var sharedStore = Substitute.For<IWorkflowInstanceStore>();
        var ownStore = Substitute.For<IWorkflowInstanceStore>();
        var sharedRestarter = Substitute.For<IWorkflowRestarter>();
        var ownRestarter = Substitute.For<IWorkflowRestarter>();
        var tenantAccessor = new DefaultTenantAccessor();
        var clock = Substitute.For<ISystemClock>();
        clock.UtcNow.Returns(now);
        var ownTenant = new Tenant
        {
            Id = "tenant-own",
            Name = "Own",
            Configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["ConnectionStrings:Sqlite"] = "Data Source=own.db" }).Build()
        };
        var tenantService = Substitute.For<ITenantService>();
        tenantService.ListAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult<IEnumerable<Tenant>>([ownTenant]));
        StubInstances(sharedStore, CreateWorkflowInstance("shared", Tenant.DefaultTenantId, now));
        StubInstances(ownStore, CreateWorkflowInstance("own", "tenant-own", now));
        var ownServices = new ServiceCollection().AddSingleton(ownStore).AddSingleton(ownRestarter).BuildServiceProvider();
        var scopeFactory = Substitute.For<ITenantScopeFactory>();
        scopeFactory.CreateScope(ownTenant).Returns(_ => new TenantScope(ownServices.CreateScope(), tenantAccessor, ownTenant));
        var options = Microsoft.Extensions.Options.Options.Create(new RuntimeOptions { RestartInterruptedWorkflowsBatchSize = 10, InactivityThreshold = TimeSpan.FromMinutes(5) });
        var task = new RestartInterruptedWorkflowsTask(sharedRestarter, sharedStore, Substitute.For<ILogger<RestartInterruptedWorkflowsTask>>(), options, clock, tenantService, tenantAccessor, scopeFactory);

        await task.ExecuteAsync(CancellationToken.None);

        await sharedRestarter.Received(1).RestartWorkflowAsync("shared", Arg.Any<CancellationToken>());
        await ownRestarter.Received(1).RestartWorkflowAsync("own", Arg.Any<CancellationToken>());
        await ownStore.Received().SummarizeManyAsync(
            Arg.Is<WorkflowInstanceFilter>(f => !f.TenantAgnostic && f.IsExecuting == true && f.BeforeLastUpdated == now - TimeSpan.FromMinutes(5)),
            Arg.Any<PageArgs>(),
            Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "The obsolete constructor still restarts instances found in the shared store")]
    public async Task ObsoleteConstructor_RestartsFromSharedStore()
    {
        var now = DateTimeOffset.Parse("2026-04-14T12:00:00Z");
        var store = Substitute.For<IWorkflowInstanceStore>();
        var restarter = Substitute.For<IWorkflowRestarter>();
        var clock = Substitute.For<ISystemClock>();
        clock.UtcNow.Returns(now);
        StubInstances(store, CreateWorkflowInstance("workflow-1", null, now));
        var options = Microsoft.Extensions.Options.Options.Create(new RuntimeOptions { RestartInterruptedWorkflowsBatchSize = 10, InactivityThreshold = TimeSpan.FromMinutes(5) });
        var task = new RestartInterruptedWorkflowsTask(restarter, store, Substitute.For<ILogger<RestartInterruptedWorkflowsTask>>(), options, clock);

        await task.ExecuteAsync(CancellationToken.None);

        await restarter.Received(1).RestartWorkflowAsync("workflow-1", Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "ExecuteAsync restarts a Running/Interrupted instance not updated within the inactivity threshold")]
    public async Task ExecuteAsync_RestartsStaleInterruptedInstance()
    {
        var now = DateTimeOffset.Parse("2026-04-14T12:00:00Z");
        var store = new MemoryWorkflowInstanceStore(new MemoryStore<WorkflowInstance>());
        await SaveInstanceAsync(store, "interrupted-stale", WorkflowStatus.Running, WorkflowSubStatus.Interrupted, isExecuting: false, now.AddMinutes(-10));
        await SaveInstanceAsync(store, "interrupted-recent", WorkflowStatus.Running, WorkflowSubStatus.Interrupted, isExecuting: false, now.AddMinutes(-1));
        await SaveInstanceAsync(store, "finished-interrupted", WorkflowStatus.Finished, WorkflowSubStatus.Interrupted, isExecuting: false, now.AddMinutes(-10));
        var restarted = new List<string>();
        var clock = Substitute.For<ISystemClock>();
        clock.UtcNow.Returns(now);
        var task = CreateTask(store, RecordingRestarter(restarted), clock);

        await task.ExecuteAsync(CancellationToken.None);

        Assert.Equal(["interrupted-stale"], restarted);
    }

    [Fact(DisplayName = "ExecuteAsync still restarts stale executing instances without changing their timestamp")]
    public async Task ExecuteAsync_StaleExecutingInstance_RestartedUntouched()
    {
        var now = DateTimeOffset.Parse("2026-04-14T12:00:00Z");
        var store = new MemoryWorkflowInstanceStore(new MemoryStore<WorkflowInstance>());
        await SaveInstanceAsync(store, "executing-stale", WorkflowStatus.Running, WorkflowSubStatus.Executing, isExecuting: true, now.AddMinutes(-10));
        await SaveInstanceAsync(store, "executing-recent", WorkflowStatus.Running, WorkflowSubStatus.Executing, isExecuting: true, now.AddMinutes(-1));
        var restarted = new List<string>();
        var clock = Substitute.For<ISystemClock>();
        clock.UtcNow.Returns(now);
        var task = CreateTask(store, RecordingRestarter(restarted), clock);

        await task.ExecuteAsync(CancellationToken.None);

        Assert.Equal(["executing-stale"], restarted);
        var instance = await store.FindAsync(new WorkflowInstanceFilter { Id = "executing-stale" });
        Assert.Equal(now.AddMinutes(-10), instance!.UpdatedAt);
    }

    [Fact(DisplayName = "The next cycle does not restart an Interrupted instance again while its restart is still in flight")]
    public async Task ExecuteAsync_InterruptedInstance_NotRestartedTwiceAcrossCycles()
    {
        var now = DateTimeOffset.Parse("2026-04-14T12:00:00Z");
        var threshold = TimeSpan.FromMinutes(5);
        var store = new MemoryWorkflowInstanceStore(new MemoryStore<WorkflowInstance>());
        await SaveInstanceAsync(store, "interrupted", WorkflowStatus.Running, WorkflowSubStatus.Interrupted, isExecuting: false, now.AddMinutes(-10));
        var restarted = new List<string>();
        var clock = Substitute.For<ISystemClock>();
        clock.UtcNow.Returns(now);
        // The restarter only records: the dispatched run has not touched the row yet when the next cycle starts.
        var task = CreateTask(store, RecordingRestarter(restarted), clock);

        await task.ExecuteAsync(CancellationToken.None);
        clock.UtcNow.Returns(now + threshold);
        await task.ExecuteAsync(CancellationToken.None);

        Assert.Equal(["interrupted"], restarted);
    }

    [Fact(DisplayName = "An Interrupted instance whose restart never ran is restarted again once the inactivity threshold passes")]
    public async Task ExecuteAsync_InterruptedInstance_RetriedAfterThreshold()
    {
        var now = DateTimeOffset.Parse("2026-04-14T12:00:00Z");
        var threshold = TimeSpan.FromMinutes(5);
        var store = new MemoryWorkflowInstanceStore(new MemoryStore<WorkflowInstance>());
        await SaveInstanceAsync(store, "interrupted", WorkflowStatus.Running, WorkflowSubStatus.Interrupted, isExecuting: false, now.AddMinutes(-10));
        var restarted = new List<string>();
        var clock = Substitute.For<ISystemClock>();
        clock.UtcNow.Returns(now);
        var task = CreateTask(store, RecordingRestarter(restarted), clock);

        await task.ExecuteAsync(CancellationToken.None);
        clock.UtcNow.Returns(now + threshold + TimeSpan.FromSeconds(1));
        await task.ExecuteAsync(CancellationToken.None);

        Assert.Equal(["interrupted", "interrupted"], restarted);
    }

    [Fact(DisplayName = "An Interrupted instance of a tenant with its own database is stamped in that database before it is restarted")]
    public async Task ExecuteAsync_OwnDatabaseTenant_InterruptedInstanceStampedInOwnStore()
    {
        var now = DateTimeOffset.Parse("2026-04-14T12:00:00Z");
        var sharedStore = Substitute.For<IWorkflowInstanceStore>();
        var ownStore = Substitute.For<IWorkflowInstanceStore>();
        var sharedRestarter = Substitute.For<IWorkflowRestarter>();
        var ownRestarter = Substitute.For<IWorkflowRestarter>();
        var tenantAccessor = new DefaultTenantAccessor();
        var clock = Substitute.For<ISystemClock>();
        clock.UtcNow.Returns(now);
        var ownTenant = new Tenant
        {
            Id = "tenant-own",
            Name = "Own",
            Configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["ConnectionStrings:Sqlite"] = "Data Source=own.db" }).Build()
        };
        var tenantService = Substitute.For<ITenantService>();
        tenantService.ListAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult<IEnumerable<Tenant>>([ownTenant]));
        StubInterruptedInstances(sharedStore, CreateWorkflowInstance("shared", Tenant.DefaultTenantId, now));
        StubInterruptedInstances(ownStore, CreateWorkflowInstance("own", "tenant-own", now));
        var ownServices = new ServiceCollection().AddSingleton(ownStore).AddSingleton(ownRestarter).BuildServiceProvider();
        var scopeFactory = Substitute.For<ITenantScopeFactory>();
        scopeFactory.CreateScope(ownTenant).Returns(_ => new TenantScope(ownServices.CreateScope(), tenantAccessor, ownTenant));
        var options = Microsoft.Extensions.Options.Options.Create(new RuntimeOptions { RestartInterruptedWorkflowsBatchSize = 10, InactivityThreshold = TimeSpan.FromMinutes(5) });
        var task = new RestartInterruptedWorkflowsTask(sharedRestarter, sharedStore, Substitute.For<ILogger<RestartInterruptedWorkflowsTask>>(), options, clock, tenantService, tenantAccessor, scopeFactory);

        await task.ExecuteAsync(CancellationToken.None);

        Received.InOrder(() =>
        {
            ownStore.UpdateUpdatedTimestampAsync("own", now, Arg.Any<CancellationToken>());
            ownRestarter.RestartWorkflowAsync("own", Arg.Any<CancellationToken>());
        });
        Received.InOrder(() =>
        {
            sharedStore.UpdateUpdatedTimestampAsync("shared", now, Arg.Any<CancellationToken>());
            sharedRestarter.RestartWorkflowAsync("shared", Arg.Any<CancellationToken>());
        });
        await sharedStore.DidNotReceive().UpdateUpdatedTimestampAsync("own", Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>());
        await ownStore.Received().SummarizeManyAsync(
            Arg.Is<WorkflowInstanceFilter>(f => !f.TenantAgnostic && f.WorkflowSubStatus == WorkflowSubStatus.Interrupted && f.BeforeLastUpdated == now - TimeSpan.FromMinutes(5)),
            Arg.Any<PageArgs>(),
            Arg.Any<CancellationToken>());
    }

    private static RestartInterruptedWorkflowsTask CreateTask(IWorkflowInstanceStore store, IWorkflowRestarter restarter, ISystemClock clock)
    {
        var options = Microsoft.Extensions.Options.Options.Create(new RuntimeOptions { RestartInterruptedWorkflowsBatchSize = 10, InactivityThreshold = TimeSpan.FromMinutes(5) });
        return new RestartInterruptedWorkflowsTask(restarter, store, Substitute.For<ILogger<RestartInterruptedWorkflowsTask>>(), options, clock, Substitute.For<ITenantService>(), new DefaultTenantAccessor(), null);
    }

    private static IWorkflowRestarter RecordingRestarter(List<string> restarted)
    {
        var restarter = Substitute.For<IWorkflowRestarter>();
        restarter.When(x => x.RestartWorkflowAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())).Do(ci => restarted.Add(ci.ArgAt<string>(0)));
        return restarter;
    }

    private static async Task SaveInstanceAsync(IWorkflowInstanceStore store, string id, WorkflowStatus status, WorkflowSubStatus subStatus, bool isExecuting, DateTimeOffset updatedAt)
    {
        await store.SaveAsync(new WorkflowInstance
        {
            Id = id,
            TenantId = Tenant.DefaultTenantId,
            DefinitionId = "definition",
            DefinitionVersionId = "definition:1",
            Version = 1,
            Status = status,
            SubStatus = subStatus,
            IsExecuting = isExecuting,
            CreatedAt = updatedAt,
            UpdatedAt = updatedAt,
            WorkflowState = new WorkflowState { Id = id, DefinitionId = "definition", DefinitionVersionId = "definition:1", Status = status, SubStatus = subStatus }
        });
    }

    private static void StubInterruptedInstances(IWorkflowInstanceStore store, params WorkflowInstanceSummary[] instances)
    {
        store
            .SummarizeManyAsync(Arg.Any<WorkflowInstanceFilter>(), Arg.Any<PageArgs>(), Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                var interrupted = ci.ArgAt<WorkflowInstanceFilter>(0).WorkflowSubStatus == WorkflowSubStatus.Interrupted && ci.ArgAt<PageArgs>(1).Offset == 0;
                return new ValueTask<Page<WorkflowInstanceSummary>>(Page.Of<WorkflowInstanceSummary>(interrupted ? instances : [], interrupted ? instances.Length : 0));
            });
    }

    private static void StubInstances(IWorkflowInstanceStore store, params WorkflowInstanceSummary[] instances)
    {
        store
            .SummarizeManyAsync(Arg.Any<WorkflowInstanceFilter>(), Arg.Any<PageArgs>(), Arg.Any<CancellationToken>())
            .Returns(ci => new ValueTask<Page<WorkflowInstanceSummary>>(Page.Of<WorkflowInstanceSummary>(ci.ArgAt<PageArgs>(1).Offset == 0 ? instances : [], instances.Length)));
    }

    private static WorkflowInstanceSummary CreateWorkflowInstance(string id, string? tenantId, DateTimeOffset updatedAt)
    {
        return new WorkflowInstanceSummary
        {
            Id = id,
            TenantId = tenantId,
            DefinitionId = "definition",
            DefinitionVersionId = "definition:1",
            Status = WorkflowStatus.Running,
            SubStatus = WorkflowSubStatus.Pending,
            CreatedAt = updatedAt.AddMinutes(-10),
            UpdatedAt = updatedAt.AddMinutes(-10)
        };
    }
}