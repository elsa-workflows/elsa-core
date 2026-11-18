using Elsa.Common.Multitenancy;
using Elsa.Workflows;
using Elsa.Workflows.Management.Exceptions;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Management.Mappers;
using Elsa.Workflows.State;
using Microsoft.EntityFrameworkCore;

namespace Elsa.Workflows.Persistence.ConformanceTests;

[Collection(WorkflowStoreSqliteConformanceCollection.Name)]
public sealed class EFCoreWorkflowInstanceSnapshotTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task DetachedSnapshotsCommitAfterCreationAndFromLegacyRows(bool legacyTenantId)
    {
        await using var scenario = await WorkflowStoreScenario.CreateSqliteAsync(Tenant.DefaultTenantId);
        var stateMapper = new WorkflowStateMapper();
        var state = new WorkflowState
        {
            Id = "first-run",
            DefinitionId = "workflow",
            DefinitionVersionId = "workflow-v1",
            DefinitionVersion = 1,
            CreatedAt = DateTimeOffset.UnixEpoch,
            UpdatedAt = DateTimeOffset.UnixEpoch,
            IsExecuting = true,
            Status = WorkflowStatus.Running,
            SubStatus = WorkflowSubStatus.Pending
        };

        await scenario.Instances.AddAsync(stateMapper.Map(state)!);
        await Assert.ThrowsAsync<WorkflowInstanceAlreadyExistsException>(
            () => scenario.Instances.AddAsync(stateMapper.Map(state)!).AsTask());

        if (legacyTenantId)
        {
            await using var dbContext = await scenario.ManagementDbContextFactory!.CreateDbContextAsync();
            await dbContext.WorkflowInstances
                .Where(instance => instance.Id == state.Id)
                .ExecuteUpdateAsync(setters => setters.SetProperty(instance => instance.TenantId, (string?)null));
        }

        state.UpdatedAt = state.CreatedAt.AddSeconds(1);
        state.IsExecuting = false;
        state.SubStatus = WorkflowSubStatus.Suspended;
        await scenario.Instances.SaveAsync(stateMapper.Map(state)!);

        var suspended = await scenario.Instances.FindAsync(new WorkflowInstanceFilter { Id = state.Id });
        Assert.NotNull(suspended);
        Assert.Equal(WorkflowSubStatus.Suspended, suspended.SubStatus);
        Assert.Equal(state.UpdatedAt, suspended.UpdatedAt);

        state.UpdatedAt = state.UpdatedAt.AddSeconds(1);
        state.Status = WorkflowStatus.Finished;
        state.SubStatus = WorkflowSubStatus.Finished;
        await scenario.Instances.SaveAsync(stateMapper.Map(state)!);

        var finished = await scenario.Instances.FindAsync(new WorkflowInstanceFilter { Id = state.Id });
        Assert.NotNull(finished);
        Assert.Equal(WorkflowStatus.Finished, finished.Status);
        Assert.Equal(WorkflowSubStatus.Finished, finished.SubStatus);
        Assert.False(finished.IsExecuting);
        Assert.Equal(Tenant.DefaultTenantId, finished.TenantId);
        Assert.Equal(1, await scenario.Instances.CountAsync(new WorkflowInstanceFilter { Id = state.Id }));
    }
}
