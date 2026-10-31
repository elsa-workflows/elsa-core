using Elsa.Workflows;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.State;

namespace Elsa.Workflows.Persistence.ConformanceTests;

/// <summary>
/// SQLite coverage for <see cref="Elsa.Persistence.EFCore.Modules.Management.EFCoreWorkflowInstanceStore.TryTransitionSubStatusAsync"/>.
/// The compare-and-swap is a filtered <c>ExecuteUpdate</c>, including the <c>UpdatedAt</c> equality that a timestamp converter can break.
/// </summary>
[Collection(WorkflowStoreSqliteConformanceCollection.Name)]
public sealed class EFCoreWorkflowInstanceSubStatusTransitionTests
{
    private static readonly DateTimeOffset OriginalUpdatedAt = new(2026, 10, 1, 0, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset CancellingAt = new(2026, 10, 26, 12, 0, 0, 123, TimeSpan.Zero);
    private static readonly DateTimeOffset RestoredAt = new(2026, 10, 26, 12, 5, 0, 456, TimeSpan.Zero);

    [Fact]
    public async Task TryTransitionSubStatusAsync_MarksSuspendedAndWritesOnlySubStatusAndUpdatedAt()
    {
        await using var scenario = await WorkflowStoreScenario.CreateSqliteAsync();
        await scenario.Instances.SaveAsync(Suspended("wf-1"));

        var marked = await scenario.Instances.TryTransitionSubStatusAsync(
            "wf-1",
            WorkflowSubStatus.Suspended,
            null,
            WorkflowSubStatus.Cancelling,
            CancellingAt);

        Assert.True(marked);
        var stored = await FindAsync(scenario, "wf-1");
        Assert.Equal(WorkflowStatus.Running, stored.Status);
        Assert.Equal(WorkflowSubStatus.Cancelling, stored.SubStatus);
        Assert.Equal(CancellingAt, stored.UpdatedAt);
        Assert.False(stored.IsExecuting);
        Assert.Equal("def-1", stored.DefinitionId);
    }

    [Fact]
    public async Task TryTransitionSubStatusAsync_SecondMarkLeavesTheFirstStamp()
    {
        await using var scenario = await WorkflowStoreScenario.CreateSqliteAsync();
        await scenario.Instances.SaveAsync(Suspended("wf-1"));
        Assert.True(await scenario.Instances.TryTransitionSubStatusAsync(
            "wf-1",
            WorkflowSubStatus.Suspended,
            null,
            WorkflowSubStatus.Cancelling,
            CancellingAt));

        var markedAgain = await scenario.Instances.TryTransitionSubStatusAsync(
            "wf-1",
            WorkflowSubStatus.Suspended,
            null,
            WorkflowSubStatus.Cancelling,
            RestoredAt);

        Assert.False(markedAgain);
        var stored = await FindAsync(scenario, "wf-1");
        Assert.Equal(WorkflowSubStatus.Cancelling, stored.SubStatus);
        Assert.Equal(CancellingAt, stored.UpdatedAt);
    }

    [Fact]
    public async Task TryTransitionSubStatusAsync_WrongStampDoesNotRestore()
    {
        await using var scenario = await WorkflowStoreScenario.CreateSqliteAsync();
        await scenario.Instances.SaveAsync(Suspended("wf-1"));
        Assert.True(await scenario.Instances.TryTransitionSubStatusAsync(
            "wf-1",
            WorkflowSubStatus.Suspended,
            null,
            WorkflowSubStatus.Cancelling,
            CancellingAt));

        var restored = await scenario.Instances.TryTransitionSubStatusAsync(
            "wf-1",
            WorkflowSubStatus.Cancelling,
            CancellingAt.AddMilliseconds(1),
            WorkflowSubStatus.Suspended,
            RestoredAt);

        Assert.False(restored);
        var stored = await FindAsync(scenario, "wf-1");
        Assert.Equal(WorkflowStatus.Running, stored.Status);
        Assert.Equal(WorkflowSubStatus.Cancelling, stored.SubStatus);
        Assert.Equal(CancellingAt, stored.UpdatedAt);
    }

    [Fact]
    public async Task TryTransitionSubStatusAsync_RightStampRestoresPreviousSubStatus()
    {
        await using var scenario = await WorkflowStoreScenario.CreateSqliteAsync();
        await scenario.Instances.SaveAsync(Suspended("wf-1"));
        Assert.True(await scenario.Instances.TryTransitionSubStatusAsync(
            "wf-1",
            WorkflowSubStatus.Suspended,
            null,
            WorkflowSubStatus.Cancelling,
            CancellingAt));

        var restored = await scenario.Instances.TryTransitionSubStatusAsync(
            "wf-1",
            WorkflowSubStatus.Cancelling,
            CancellingAt,
            WorkflowSubStatus.Suspended,
            RestoredAt);

        Assert.True(restored);
        var stored = await FindAsync(scenario, "wf-1");
        Assert.Equal(WorkflowStatus.Running, stored.Status);
        Assert.Equal(WorkflowSubStatus.Suspended, stored.SubStatus);
        Assert.Equal(RestoredAt, stored.UpdatedAt);
        Assert.False(stored.IsExecuting);
    }

    [Fact]
    public async Task TryTransitionSubStatusAsync_RefusesExecutingRow()
    {
        await using var scenario = await WorkflowStoreScenario.CreateSqliteAsync();
        await scenario.Instances.SaveAsync(Suspended("wf-1", isExecuting: true));

        var marked = await scenario.Instances.TryTransitionSubStatusAsync(
            "wf-1",
            WorkflowSubStatus.Suspended,
            null,
            WorkflowSubStatus.Cancelling,
            CancellingAt);

        Assert.False(marked);
        var stored = await FindAsync(scenario, "wf-1");
        Assert.Equal(WorkflowSubStatus.Suspended, stored.SubStatus);
        Assert.True(stored.IsExecuting);
        Assert.Equal(OriginalUpdatedAt, stored.UpdatedAt);
    }

    [Fact]
    public async Task TryTransitionSubStatusAsync_DoesNotMarkAnotherTenant()
    {
        await using var scenario = await WorkflowStoreScenario.CreateSqliteAsync("tenant-a");
        await scenario.Instances.SaveAsync(Suspended("wf-1"));

        using (scenario.UseTenant("tenant-b"))
        {
            var marked = await scenario.Instances.TryTransitionSubStatusAsync(
                "wf-1",
                WorkflowSubStatus.Suspended,
                null,
                WorkflowSubStatus.Cancelling,
                CancellingAt);

            Assert.False(marked);
        }

        var stored = await FindAsync(scenario, "wf-1");
        Assert.Equal(WorkflowSubStatus.Suspended, stored.SubStatus);
        Assert.Equal(OriginalUpdatedAt, stored.UpdatedAt);
    }

    private static async Task<WorkflowInstance> FindAsync(WorkflowStoreScenario scenario, string id)
    {
        var stored = await scenario.Instances.FindAsync(new WorkflowInstanceFilter { Id = id });
        Assert.NotNull(stored);
        return stored;
    }

    private static WorkflowInstance Suspended(string id, bool isExecuting = false) => new()
    {
        Id = id,
        DefinitionId = "def-1",
        DefinitionVersionId = "ver-1",
        Version = 1,
        Status = WorkflowStatus.Running,
        SubStatus = WorkflowSubStatus.Suspended,
        IsExecuting = isExecuting,
        UpdatedAt = OriginalUpdatedAt,
        CreatedAt = OriginalUpdatedAt,
        WorkflowState = new WorkflowState
        {
            Id = id,
            DefinitionId = "def-1",
            DefinitionVersionId = "ver-1",
            Status = WorkflowStatus.Running,
            SubStatus = WorkflowSubStatus.Suspended
        }
    };
}
