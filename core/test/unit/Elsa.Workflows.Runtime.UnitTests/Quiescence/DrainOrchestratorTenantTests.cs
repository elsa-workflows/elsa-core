using Elsa.Common.Multitenancy;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Runtime.Entities;
using NSubstitute;

namespace Elsa.Workflows.Runtime.UnitTests.Quiescence;

/// <summary>
/// A force drain of a cycle that runs in a named tenant. The drain itself runs outside any tenant, and the instance
/// store only sees the instance under its own tenant.
/// </summary>
public class DrainOrchestratorTenantTests : DrainOrchestratorTestsBase
{
    private const string InstanceId = "instance-tenant-a";
    private static readonly Tenant TenantA = new() { Id = "tenant-a", Name = "tenant-a" };
    private readonly List<(string? TenantId, string? Reason)> _logEntries = [];

    public DrainOrchestratorTenantTests()
    {
        var handle = new ExecutionCycleHandle(Guid.NewGuid(), InstanceId, ingressSourceName: null, startedAt: DateTimeOffset.UtcNow, linkedToken: CancellationToken.None) { Tenant = TenantA };
        ExecutionCycleRegistry.ActiveCount.Returns(1);
        ExecutionCycleRegistry.ListActiveCycles().Returns(new[] { handle });
        LogStore.When(x => x.AddAsync(Arg.Any<WorkflowExecutionLogRecord>(), Arg.Any<CancellationToken>()))
            .Do(call => _logEntries.Add((TenantAccessor.Tenant?.Id, (call.Arg<WorkflowExecutionLogRecord>().Payload as WorkflowInterruptedPayload)?.Reason)));
    }

    [Fact(DisplayName = "A named tenant's running instance is marked Interrupted and logged under that tenant")]
    public async Task RunningInstanceIsMarkedUnderItsTenant()
    {
        StubInstanceVisibleToTenantA(RunningInstance());
        string? markedUnder = null;
        InstanceStore.TryMarkInterruptedAsync(InstanceId, Arg.Any<CancellationToken>(), Arg.Any<bool>()).Returns(_ =>
        {
            markedUnder = TenantAccessor.Tenant?.Id;
            return new ValueTask<bool>(true);
        });

        var outcome = await BuildSut().DrainAsync(DrainTrigger.OperatorForce);

        Assert.Equal(1, outcome.ExecutionCyclesForceCancelledCount);
        Assert.Equal(TenantA.Id, markedUnder);
        Assert.Equal([(TenantA.Id, WorkflowInterruptedPayload.ReasonOperatorForce)], _logEntries);
    }

    [Fact(DisplayName = "A named tenant's user-cancelled instance is left alone, because the pre-cancel snapshot also reads under that tenant")]
    public async Task UserCancelledInstanceIsLeftAlone()
    {
        StubInstanceVisibleToTenantA(new()
        {
            Id = InstanceId,
            TenantId = TenantA.Id,
            DefinitionId = "def-1",
            DefinitionVersionId = "ver-1",
            Version = 1,
            Status = WorkflowStatus.Finished,
            SubStatus = WorkflowSubStatus.Cancelled,
        });

        await BuildSut().DrainAsync(DrainTrigger.OperatorForce);

        await InstanceStore.DidNotReceive().TryMarkInterruptedAsync(Arg.Any<string>(), Arg.Any<CancellationToken>(), Arg.Any<bool>());
        Assert.Empty(_logEntries);
    }

    [Fact(DisplayName = "The obsolete constructor still reads and writes outside the cycle's tenant")]
    public async Task ObsoleteConstructorIgnoresTheCycleTenant()
    {
        StubInstanceVisibleToTenantA(RunningInstance());

        var outcome = await BuildSut(withTenantScopeFactory: false).DrainAsync(DrainTrigger.OperatorForce);

        Assert.Equal(1, outcome.ExecutionCyclesForceCancelledCount);
        await InstanceStore.DidNotReceive().TryMarkInterruptedAsync(Arg.Any<string>(), Arg.Any<CancellationToken>(), Arg.Any<bool>());
        Assert.Equal([((string?)null, WorkflowInterruptedPayload.ReasonPersistenceFailure)], _logEntries);
    }

    private void StubInstanceVisibleToTenantA(WorkflowInstance instance) =>
        InstanceStore.FindAsync(Arg.Is<WorkflowInstanceFilter>(f => f.Id == InstanceId), Arg.Any<CancellationToken>())
            .Returns(_ => new ValueTask<WorkflowInstance?>(TenantAccessor.Tenant?.Id == TenantA.Id ? instance : null));

    private static WorkflowInstance RunningInstance() => new()
    {
        Id = InstanceId,
        TenantId = TenantA.Id,
        DefinitionId = "def-1",
        DefinitionVersionId = "ver-1",
        Version = 1,
        Status = WorkflowStatus.Running,
        SubStatus = WorkflowSubStatus.Executing,
        IsExecuting = true,
    };
}
