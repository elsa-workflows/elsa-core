using Elsa.Common.Models;
using Elsa.Common.Multitenancy;
using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Management.Models;
using Elsa.Workflows.Runtime.Options;
using Elsa.Workflows.Runtime.Services;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Elsa.Workflows.Runtime.UnitTests.Recovery;

public class InterruptedRecoveryScannerTests
{
    private readonly IWorkflowRestarter _restarter = Substitute.For<IWorkflowRestarter>();
    private readonly IWorkflowInstanceStore _instanceStore = Substitute.For<IWorkflowInstanceStore>();
    private readonly ILogger<InterruptedRecoveryScanner> _logger = Substitute.For<ILogger<InterruptedRecoveryScanner>>();
    private readonly RuntimeOptions _runtimeOptions = new() { RestartInterruptedWorkflowsBatchSize = 10 };

    [Fact(DisplayName = "Scan filters by SubStatus = Interrupted")]
    public async Task FiltersBySubStatus()
    {
        StubInstances(new[] { Summary("a") });
        var sut = BuildSut();

        await sut.ScanAndRequeueAsync(CancellationToken.None);

        await _instanceStore.Received().SummarizeManyAsync(
            Arg.Is<WorkflowInstanceFilter>(f => f.WorkflowSubStatus == WorkflowSubStatus.Interrupted),
            Arg.Any<PageArgs>(),
            Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "Scan calls IWorkflowRestarter once per matching instance and returns the count")]
    public async Task RestartsEachInstance()
    {
        StubInstances(new[] { Summary("a"), Summary("b"), Summary("c") });
        var sut = BuildSut();

        var count = await sut.ScanAndRequeueAsync(CancellationToken.None);

        Assert.Equal(3, count);
        await _restarter.Received(1).RestartWorkflowAsync("a", Arg.Any<CancellationToken>());
        await _restarter.Received(1).RestartWorkflowAsync("b", Arg.Any<CancellationToken>());
        await _restarter.Received(1).RestartWorkflowAsync("c", Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "Per-instance failures are swallowed; remaining instances still requeued")]
    public async Task SwallowsPerInstanceFailures()
    {
        StubInstances(new[] { Summary("a"), Summary("b"), Summary("c") });
        _restarter.RestartWorkflowAsync("b", Arg.Any<CancellationToken>())
            .Returns(_ => throw new InvalidOperationException("boom"));
        var sut = BuildSut();

        var count = await sut.ScanAndRequeueAsync(CancellationToken.None);

        Assert.Equal(2, count); // 'b' failed
        await _restarter.Received(1).RestartWorkflowAsync("a", Arg.Any<CancellationToken>());
        await _restarter.Received(1).RestartWorkflowAsync("c", Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "No interrupted instances → returns 0 without invoking restarter")]
    public async Task EmptyResultSet()
    {
        StubInstances(Array.Empty<WorkflowInstanceSummary>());
        var sut = BuildSut();

        var count = await sut.ScanAndRequeueAsync(CancellationToken.None);

        Assert.Equal(0, count);
        await _restarter.DidNotReceive().RestartWorkflowAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "Tenant context is pushed for tenant-scoped instances")]
    public async Task PushesTenantContext()
    {
        StubInstances(new[] { Summary("a", tenantId: "tenant-1") });
        var tenantService = Substitute.For<ITenantService>();
        tenantService.FindAsync("tenant-1", Arg.Any<CancellationToken>()).Returns(new Tenant { Id = "tenant-1", Name = "Tenant 1" });
        var tenantAccessor = new DefaultTenantAccessor();
        var observedTenant = (string?)null;
        _restarter
            .When(x => x.RestartWorkflowAsync("a", Arg.Any<CancellationToken>()))
            .Do(_ => observedTenant = tenantAccessor.Tenant?.Id);

        var sut = BuildSut(tenantService, tenantAccessor);
        await sut.ScanAndRequeueAsync(CancellationToken.None);

        Assert.Equal("tenant-1", observedTenant);
        Assert.Null(tenantAccessor.Tenant); // popped after the call
    }

    [Theory(DisplayName = "Default or agnostic tenant ids do NOT push tenant context")]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("*")]
    public async Task DefaultOrAgnosticTenant(string? tenantId)
    {
        StubInstances(new[] { Summary("a", tenantId: tenantId) });
        var tenantService = Substitute.For<ITenantService>();
        var tenantAccessor = new DefaultTenantAccessor();
        var observedTenant = (Tenant?)tenantAccessor.Tenant;
        _restarter
            .When(x => x.RestartWorkflowAsync("a", Arg.Any<CancellationToken>()))
            .Do(_ => observedTenant = tenantAccessor.Tenant);

        var sut = BuildSut(tenantService, tenantAccessor);
        await sut.ScanAndRequeueAsync(CancellationToken.None);

        Assert.Null(observedTenant);
        await tenantService.DidNotReceive().FindAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    private InterruptedRecoveryScanner BuildSut(ITenantService? tenantService = null, ITenantAccessor? tenantAccessor = null) =>
        new(_restarter, _instanceStore, Microsoft.Extensions.Options.Options.Create(_runtimeOptions), _logger, tenantService, tenantAccessor);

    private void StubInstances(IReadOnlyList<WorkflowInstanceSummary> all)
    {
        _instanceStore
            .SummarizeManyAsync(Arg.Any<WorkflowInstanceFilter>(), Arg.Any<PageArgs>(), Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                var pageArgs = ci.ArgAt<PageArgs>(1);
                ICollection<WorkflowInstanceSummary> items = pageArgs.Offset == 0
                    ? all.ToList()
                    : new List<WorkflowInstanceSummary>();
                return new ValueTask<Page<WorkflowInstanceSummary>>(Page.Of(items, all.Count));
            });
    }

    private static WorkflowInstanceSummary Summary(string id, string? tenantId = null) => new()
    {
        Id = id,
        TenantId = tenantId,
        DefinitionId = "def-" + id,
        DefinitionVersionId = "ver-" + id,
        Version = 1,
        SubStatus = WorkflowSubStatus.Interrupted,
        CreatedAt = DateTimeOffset.UtcNow,
        UpdatedAt = DateTimeOffset.UtcNow,
    };
}
