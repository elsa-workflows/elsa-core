using Elsa.Common;
using Elsa.Common.Models;
using Elsa.Common.Multitenancy;
using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Management.Models;
using Elsa.Workflows.Runtime.Options;
using Elsa.Workflows.Runtime.Tasks;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Elsa.Workflows.Runtime.UnitTests.Services;

public class RestartInterruptedWorkflowsTaskTests
{
    [Test]
    [DisplayName("ExecuteAsync restarts each workflow within its tenant context")]
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

        await Assert.That(observedTenantIds).IsEquivalentTo(new string?[] { "tenant-a", "tenant-b" }, TUnit.Assertions.Enums.CollectionOrdering.Matching);
        await Assert.That(tenantAccessor.Tenant).IsNull();
        await workflowRestarter.Received(1).RestartWorkflowAsync("workflow-1", Arg.Any<CancellationToken>());
        await workflowRestarter.Received(1).RestartWorkflowAsync("workflow-2", Arg.Any<CancellationToken>());
    }

    [Test]
    [Arguments(null, DisplayName = "ExecuteAsync does not push tenant context for a null tenant id")]
    [Arguments("", DisplayName = "ExecuteAsync does not push tenant context for an empty tenant id")]
    [Arguments("*", DisplayName = "ExecuteAsync does not push tenant context for an agnostic tenant id")]
    public async Task ExecuteAsync_DefaultOrAgnosticTenant_DoesNotPushContext(string? tenantId)
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

        await Assert.That(observedTenantIds).IsEquivalentTo(new string?[] { null }, TUnit.Assertions.Enums.CollectionOrdering.Matching);
        await Assert.That(tenantAccessor.Tenant).IsNull();
        await tenantService.DidNotReceive().FindAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        await workflowRestarter.Received(1).RestartWorkflowAsync("workflow-1", Arg.Any<CancellationToken>());
    }

    [Test]
    [DisplayName("ExecuteAsync continues restarting later workflows after a failure")]
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

    [Test]
    [DisplayName("ExecuteAsync restarts tenant-specific workflows when tenant services are unavailable")]
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
