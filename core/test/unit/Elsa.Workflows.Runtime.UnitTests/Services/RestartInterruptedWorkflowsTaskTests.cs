using System.Reflection;
using Elsa.Common;
using Elsa.Common.Models;
using Elsa.Common.Multitenancy;
using Elsa.Common.RecurringTasks;
using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Management.Models;
using Elsa.Workflows.Runtime.Options;
using Elsa.Workflows.Runtime.StartupTasks;
using Elsa.Workflows.Runtime.Tasks;
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