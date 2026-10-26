using Elsa.Common.Services;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Management.Stores;
using Elsa.Workflows.Models;
using Elsa.Workflows.State;

namespace Elsa.Workflows.Management.UnitTests.Stores;

public class MemoryWorkflowInstanceStoreTests
{
    [Fact(DisplayName = "TryMarkInterruptedAsync applies Interrupted only when the instance is still Running")]
    public async Task TryMarkInterrupted_MarksRunningInstance()
    {
        var store = CreateStore(new WorkflowInstance
        {
            Id = "running-1",
            DefinitionId = "def-1",
            DefinitionVersionId = "ver-1",
            Version = 1,
            Status = WorkflowStatus.Running,
            SubStatus = WorkflowSubStatus.Executing,
            IsExecuting = true,
        });

        var marked = await store.TryMarkInterruptedAsync("running-1");

        Assert.True(marked);
        var instance = await store.FindAsync(new() { Id = "running-1" });
        Assert.NotNull(instance);
        Assert.Equal(WorkflowStatus.Running, instance.Status);
        Assert.Equal(WorkflowSubStatus.Interrupted, instance.SubStatus);
        Assert.False(instance.IsExecuting);
    }

    [Fact(DisplayName = "TryMarkInterruptedAsync refuses Finished/Cancelled even when allowFinishedCancelled is true")]
    public async Task TryMarkInterrupted_DoesNotOverwriteCancelledInstance()
    {
        var store = CreateStore(new WorkflowInstance
        {
            Id = "cancelled-1",
            DefinitionId = "def-1",
            DefinitionVersionId = "ver-1",
            Version = 1,
            Status = WorkflowStatus.Finished,
            SubStatus = WorkflowSubStatus.Cancelled,
            IsExecuting = false,
        });

        var markedByDefault = await store.TryMarkInterruptedAsync("cancelled-1");
        var markedWhenAllowed = await store.TryMarkInterruptedAsync("cancelled-1", allowFinishedCancelled: true);

        Assert.False(markedByDefault);
        Assert.False(markedWhenAllowed);
        var instance = await store.FindAsync(new() { Id = "cancelled-1" });
        Assert.NotNull(instance);
        Assert.Equal(WorkflowStatus.Finished, instance.Status);
        Assert.Equal(WorkflowSubStatus.Cancelled, instance.SubStatus);
    }

    [Fact(DisplayName = "TryMarkInterruptedAsync still refuses Finished/Finished when allowFinishedCancelled is true")]
    public async Task TryMarkInterrupted_DoesNotOverwriteFinishedEvenWhenCancelledAllowed()
    {
        var store = CreateStore(new WorkflowInstance
        {
            Id = "finished-1",
            DefinitionId = "def-1",
            DefinitionVersionId = "ver-1",
            Version = 1,
            Status = WorkflowStatus.Finished,
            SubStatus = WorkflowSubStatus.Finished,
            IsExecuting = false,
        });

        var marked = await store.TryMarkInterruptedAsync("finished-1", allowFinishedCancelled: true);

        Assert.False(marked);
        var instance = await store.FindAsync(new() { Id = "finished-1" });
        Assert.NotNull(instance);
        Assert.Equal(WorkflowStatus.Finished, instance.Status);
        Assert.Equal(WorkflowSubStatus.Finished, instance.SubStatus);
    }

    [Fact(DisplayName = "TryMarkInterruptedAsync does not overwrite a Finished instance")]
    public async Task TryMarkInterrupted_DoesNotOverwriteFinishedInstance()
    {
        var store = CreateStore(new WorkflowInstance
        {
            Id = "finished-1",
            DefinitionId = "def-1",
            DefinitionVersionId = "ver-1",
            Version = 1,
            Status = WorkflowStatus.Finished,
            SubStatus = WorkflowSubStatus.Finished,
            IsExecuting = false,
        });

        var marked = await store.TryMarkInterruptedAsync("finished-1");

        Assert.False(marked);
        var instance = await store.FindAsync(new() { Id = "finished-1" });
        Assert.NotNull(instance);
        Assert.Equal(WorkflowStatus.Finished, instance.Status);
        Assert.Equal(WorkflowSubStatus.Finished, instance.SubStatus);
    }

    [Fact(DisplayName = "TryMarkInterruptedAsync does not replace a concurrent Finished commit with a stale Running snapshot")]
    public async Task TryMarkInterrupted_DoesNotClobberConcurrentFinishedCommit()
    {
        var memory = new MemoryStore<WorkflowInstance>();
        var store = new MemoryWorkflowInstanceStore(memory);
        await store.SaveAsync(new WorkflowInstance
        {
            Id = "raced-1",
            DefinitionId = "def-1",
            DefinitionVersionId = "ver-1",
            Version = 1,
            Status = WorkflowStatus.Running,
            SubStatus = WorkflowSubStatus.Executing,
            IsExecuting = true,
        });

        var staleRunning = await store.FindAsync(new() { Id = "raced-1" });
        Assert.NotNull(staleRunning);

        await store.SaveAsync(new WorkflowInstance
        {
            Id = "raced-1",
            DefinitionId = "def-1",
            DefinitionVersionId = "ver-1",
            Version = 1,
            Status = WorkflowStatus.Finished,
            SubStatus = WorkflowSubStatus.Finished,
            IsExecuting = false,
        });

        staleRunning.SubStatus = WorkflowSubStatus.Interrupted;
        staleRunning.IsExecuting = false;

        var marked = await store.TryMarkInterruptedAsync("raced-1");

        Assert.False(marked);
        var instance = await store.FindAsync(new() { Id = "raced-1" });
        Assert.NotNull(instance);
        Assert.Equal(WorkflowStatus.Finished, instance.Status);
        Assert.Equal(WorkflowSubStatus.Finished, instance.SubStatus);
        Assert.NotSame(staleRunning, instance);
    }

    [Fact(DisplayName = "TryMarkInterruptedAsync does not leave Finished+Interrupted when SaveAsync completes concurrently")]
    public async Task TryMarkInterrupted_DoesNotLeaveFinishedInterruptedWhenSaveCompletes()
    {
        for (var i = 0; i < 200; i++)
        {
            var store = new MemoryWorkflowInstanceStore(new MemoryStore<WorkflowInstance>());
            await store.SaveAsync(new WorkflowInstance
            {
                Id = "save-race-1",
                DefinitionId = "def-1",
                DefinitionVersionId = "ver-1",
                Version = 1,
                Status = WorkflowStatus.Running,
                SubStatus = WorkflowSubStatus.Executing,
                IsExecuting = true,
            });

            var mark = Task.Run(() => store.TryMarkInterruptedAsync("save-race-1").AsTask());
            var complete = Task.Run(() => store.SaveAsync(new WorkflowInstance
            {
                Id = "save-race-1",
                DefinitionId = "def-1",
                DefinitionVersionId = "ver-1",
                Version = 1,
                Status = WorkflowStatus.Finished,
                SubStatus = WorkflowSubStatus.Finished,
                IsExecuting = false,
            }).AsTask());

            await Task.WhenAll(mark, complete);

            var stored = await store.FindAsync(new WorkflowInstanceFilter { Id = "save-race-1" });
            Assert.NotNull(stored);
            Assert.False(
                stored.Status == WorkflowStatus.Finished && stored.SubStatus == WorkflowSubStatus.Interrupted,
                $"Finished+Interrupted after SaveAsync completion race (iteration {i}).");
        }
    }

    [Fact(DisplayName = "UpdateUpdatedTimestampAsync updates the stored instance")]
    public async Task UpdateUpdatedTimestamp_UpdatesStoredInstance()
    {
        var store = CreateStore(new WorkflowInstance
        {
            Id = "instance-1",
            DefinitionId = "def-1",
            DefinitionVersionId = "ver-1",
            Version = 1,
            Status = WorkflowStatus.Running
        });
        var updatedAt = new DateTimeOffset(2026, 1, 2, 3, 4, 5, TimeSpan.Zero);

        await store.UpdateUpdatedTimestampAsync("instance-1", updatedAt);

        var instance = await store.FindAsync(new() { Id = "instance-1" });
        Assert.NotNull(instance);
        Assert.Equal(updatedAt, instance.UpdatedAt);
    }

    [Fact(DisplayName = "TryTransitionSubStatusAsync writes only the sub-status and UpdatedAt")]
    public async Task TryTransition_UpdatesSubStatusAndTimestampOnly()
    {
        var cancellingAt = new DateTimeOffset(2026, 10, 26, 12, 0, 0, TimeSpan.Zero);
        var state = SuspendedState();
        var store = CreateStore(SuspendedInstance(state));

        var marked = await store.TryTransitionSubStatusAsync("wf-1", WorkflowSubStatus.Suspended, null, WorkflowSubStatus.Cancelling, cancellingAt);

        Assert.True(marked);
        var instance = await store.FindAsync(new() { Id = "wf-1" });
        Assert.NotNull(instance);
        Assert.Equal(WorkflowStatus.Running, instance.Status);
        Assert.Equal(WorkflowSubStatus.Cancelling, instance.SubStatus);
        Assert.Equal(cancellingAt, instance.UpdatedAt);
        Assert.False(instance.IsExecuting);
        Assert.Equal(WorkflowSubStatus.Suspended, state.SubStatus);
        Assert.Equal("bm-1", Assert.Single(state.Bookmarks).Id);
    }

    [Fact(DisplayName = "TryTransitionSubStatusAsync refuses an executing instance and leaves it unchanged")]
    public async Task TryTransition_RefusesExecutingInstance()
    {
        var originalUpdatedAt = new DateTimeOffset(2026, 10, 1, 0, 0, 0, TimeSpan.Zero);
        var state = SuspendedState();
        state.SubStatus = WorkflowSubStatus.Executing;
        var store = CreateStore(new WorkflowInstance
        {
            Id = "wf-1",
            DefinitionId = "def-1",
            DefinitionVersionId = "ver-1",
            Version = 1,
            Status = WorkflowStatus.Running,
            SubStatus = WorkflowSubStatus.Executing,
            IsExecuting = true,
            UpdatedAt = originalUpdatedAt,
            WorkflowState = state
        });

        var marked = await store.TryTransitionSubStatusAsync("wf-1", WorkflowSubStatus.Executing, null, WorkflowSubStatus.Cancelling, originalUpdatedAt.AddMinutes(1));

        Assert.False(marked);
        var instance = await store.FindAsync(new() { Id = "wf-1" });
        Assert.NotNull(instance);
        Assert.Equal(WorkflowSubStatus.Executing, instance.SubStatus);
        Assert.True(instance.IsExecuting);
        Assert.Equal(originalUpdatedAt, instance.UpdatedAt);
        Assert.Equal(WorkflowSubStatus.Executing, state.SubStatus);
        Assert.Equal("bm-1", Assert.Single(state.Bookmarks).Id);
    }

    [Fact(DisplayName = "TryTransitionSubStatusAsync makes no change when the expected sub-status does not match")]
    public async Task TryTransition_FailedPredicateMakesNoChange()
    {
        var originalUpdatedAt = new DateTimeOffset(2026, 10, 1, 0, 0, 0, TimeSpan.Zero);
        var store = CreateStore(SuspendedInstance(SuspendedState(), originalUpdatedAt));

        var marked = await store.TryTransitionSubStatusAsync("wf-1", WorkflowSubStatus.Pending, null, WorkflowSubStatus.Cancelling, originalUpdatedAt.AddMinutes(1));

        Assert.False(marked);
        var instance = await store.FindAsync(new() { Id = "wf-1" });
        Assert.NotNull(instance);
        Assert.Equal(WorkflowSubStatus.Suspended, instance.SubStatus);
        Assert.Equal(originalUpdatedAt, instance.UpdatedAt);
    }

    [Fact(DisplayName = "TryTransitionSubStatusAsync puts the previous sub-status back only for the marked generation")]
    public async Task TryTransition_RestoresPreviousSubStatusAndTimestamp()
    {
        var cancellingAt = new DateTimeOffset(2026, 10, 26, 12, 0, 0, TimeSpan.Zero);
        var restoredAt = cancellingAt.AddMinutes(2);
        var state = SuspendedState();
        var store = CreateStore(SuspendedInstance(state));
        Assert.True(await store.TryTransitionSubStatusAsync("wf-1", WorkflowSubStatus.Suspended, null, WorkflowSubStatus.Cancelling, cancellingAt));

        var restored = await store.TryTransitionSubStatusAsync("wf-1", WorkflowSubStatus.Cancelling, cancellingAt, WorkflowSubStatus.Suspended, restoredAt);

        Assert.True(restored);
        var instance = await store.FindAsync(new() { Id = "wf-1" });
        Assert.NotNull(instance);
        Assert.Equal(WorkflowStatus.Running, instance.Status);
        Assert.Equal(WorkflowSubStatus.Suspended, instance.SubStatus);
        Assert.Equal(restoredAt, instance.UpdatedAt);
        Assert.Equal(WorkflowSubStatus.Suspended, state.SubStatus);
        Assert.Equal("bm-1", Assert.Single(state.Bookmarks).Id);
    }

    [Fact(DisplayName = "TryTransitionSubStatusAsync does not overwrite a concurrent Cancelled commit")]
    public async Task TryTransition_DoesNotOverwriteCancelled()
    {
        var cancellingAt = new DateTimeOffset(2026, 10, 26, 12, 0, 0, TimeSpan.Zero);
        var store = CreateStore(SuspendedInstance(SuspendedState()));
        Assert.True(await store.TryTransitionSubStatusAsync("wf-1", WorkflowSubStatus.Suspended, null, WorkflowSubStatus.Cancelling, cancellingAt));
        await store.SaveAsync(new WorkflowInstance
        {
            Id = "wf-1",
            DefinitionId = "def-1",
            DefinitionVersionId = "ver-1",
            Version = 1,
            Status = WorkflowStatus.Finished,
            SubStatus = WorkflowSubStatus.Cancelled,
            IsExecuting = false,
            UpdatedAt = cancellingAt.AddMinutes(1),
            WorkflowState = SuspendedState()
        });

        var restored = await store.TryTransitionSubStatusAsync("wf-1", WorkflowSubStatus.Cancelling, cancellingAt, WorkflowSubStatus.Suspended, cancellingAt.AddMinutes(2));

        Assert.False(restored);
        var instance = await store.FindAsync(new() { Id = "wf-1" });
        Assert.NotNull(instance);
        Assert.Equal(WorkflowStatus.Finished, instance.Status);
        Assert.Equal(WorkflowSubStatus.Cancelled, instance.SubStatus);
    }

    [Fact(DisplayName = "TryTransitionSubStatusAsync does not apply a stale Cancelling snapshot over a concurrent Cancelled save")]
    public async Task TryTransition_DoesNotClobberConcurrentCancelledSave()
    {
        var cancellingAt = new DateTimeOffset(2026, 10, 26, 12, 0, 0, TimeSpan.Zero);
        for (var i = 0; i < 50; i++)
        {
            var store = CreateStore(SuspendedInstance(SuspendedState(), id: "race-1"));
            Assert.True(await store.TryTransitionSubStatusAsync("race-1", WorkflowSubStatus.Suspended, null, WorkflowSubStatus.Cancelling, cancellingAt));

            var restore = Task.Run(() => store.TryTransitionSubStatusAsync("race-1", WorkflowSubStatus.Cancelling, cancellingAt, WorkflowSubStatus.Suspended, cancellingAt.AddMinutes(2)).AsTask());
            var complete = Task.Run(() => store.SaveAsync(new WorkflowInstance
            {
                Id = "race-1",
                DefinitionId = "def-1",
                DefinitionVersionId = "ver-1",
                Version = 1,
                Status = WorkflowStatus.Finished,
                SubStatus = WorkflowSubStatus.Cancelled,
                IsExecuting = false,
                UpdatedAt = cancellingAt.AddMinutes(1)
            }).AsTask());

            await Task.WhenAll(restore, complete);

            var stored = await store.FindAsync(new WorkflowInstanceFilter { Id = "race-1" });
            Assert.NotNull(stored);
            Assert.False(
                stored.Status == WorkflowStatus.Finished && stored.SubStatus != WorkflowSubStatus.Cancelled,
                $"Finished row lost Cancelled during restore (iteration {i}).");
        }
    }

    [Fact(DisplayName = "TryTransitionSubStatusAsync refuses a suspended row that is executing")]
    public async Task TryTransition_RefusesExecutingSuspendedRow()
    {
        var originalUpdatedAt = new DateTimeOffset(2026, 10, 1, 0, 0, 0, TimeSpan.Zero);
        var store = CreateStore(new WorkflowInstance
        {
            Id = "wf-1",
            DefinitionId = "def-1",
            DefinitionVersionId = "ver-1",
            Version = 1,
            Status = WorkflowStatus.Running,
            SubStatus = WorkflowSubStatus.Suspended,
            IsExecuting = true,
            UpdatedAt = originalUpdatedAt
        });

        var marked = await store.TryTransitionSubStatusAsync("wf-1", WorkflowSubStatus.Suspended, null, WorkflowSubStatus.Cancelling, originalUpdatedAt.AddMinutes(1));

        Assert.False(marked);
        var instance = await store.FindAsync(new() { Id = "wf-1" });
        Assert.NotNull(instance);
        Assert.Equal(WorkflowSubStatus.Suspended, instance.SubStatus);
        Assert.True(instance.IsExecuting);
        Assert.Equal(originalUpdatedAt, instance.UpdatedAt);
    }

    [Theory(DisplayName = "TryTransitionSubStatusAsync leaves Pending and Interrupted unmarked")]
    [InlineData(WorkflowSubStatus.Pending)]
    [InlineData(WorkflowSubStatus.Interrupted)]
    public async Task TryTransition_LeavesRecoverableSubStatusesUnmarked(WorkflowSubStatus subStatus)
    {
        var originalUpdatedAt = new DateTimeOffset(2026, 10, 1, 0, 0, 0, TimeSpan.Zero);
        var store = CreateStore(new WorkflowInstance
        {
            Id = "wf-1",
            DefinitionId = "def-1",
            DefinitionVersionId = "ver-1",
            Version = 1,
            Status = WorkflowStatus.Running,
            SubStatus = subStatus,
            IsExecuting = false,
            UpdatedAt = originalUpdatedAt
        });

        var marked = await store.TryTransitionSubStatusAsync("wf-1", subStatus, null, WorkflowSubStatus.Cancelling, originalUpdatedAt.AddMinutes(1));

        Assert.False(marked);
        var instance = await store.FindAsync(new() { Id = "wf-1" });
        Assert.NotNull(instance);
        Assert.Equal(subStatus, instance.SubStatus);
        Assert.Equal(originalUpdatedAt, instance.UpdatedAt);
    }

    [Fact(DisplayName = "TryTransitionSubStatusAsync does not restore when UpdatedAt is not the mark stamp")]
    public async Task TryTransition_DoesNotRestoreWhenStampDiffers()
    {
        var cancellingAt = new DateTimeOffset(2026, 10, 26, 12, 0, 0, TimeSpan.Zero);
        var movedAt = cancellingAt.AddMinutes(1);
        var store = CreateStore(SuspendedInstance(SuspendedState()));
        Assert.True(await store.TryTransitionSubStatusAsync("wf-1", WorkflowSubStatus.Suspended, null, WorkflowSubStatus.Cancelling, cancellingAt));
        await store.SaveAsync(new WorkflowInstance
        {
            Id = "wf-1",
            DefinitionId = "def-1",
            DefinitionVersionId = "ver-1",
            Version = 1,
            Status = WorkflowStatus.Running,
            SubStatus = WorkflowSubStatus.Cancelling,
            IsExecuting = false,
            UpdatedAt = movedAt,
            WorkflowState = SuspendedState()
        });

        var restored = await store.TryTransitionSubStatusAsync("wf-1", WorkflowSubStatus.Cancelling, cancellingAt, WorkflowSubStatus.Suspended, cancellingAt.AddMinutes(2));

        Assert.False(restored);
        var instance = await store.FindAsync(new() { Id = "wf-1" });
        Assert.NotNull(instance);
        Assert.Equal(WorkflowStatus.Running, instance.Status);
        Assert.Equal(WorkflowSubStatus.Cancelling, instance.SubStatus);
        Assert.Equal(movedAt, instance.UpdatedAt);
    }

    private static MemoryWorkflowInstanceStore CreateStore(WorkflowInstance instance)
    {
        var store = new MemoryWorkflowInstanceStore(new MemoryStore<WorkflowInstance>());
        store.SaveAsync(instance).AsTask().GetAwaiter().GetResult();
        return store;
    }

    private static WorkflowState SuspendedState() => new()
    {
        Id = "wf-1",
        Status = WorkflowStatus.Running,
        SubStatus = WorkflowSubStatus.Suspended,
        Bookmarks = [new Bookmark("bm-1", "Wait", "hash", null, "activity", "node", "instance", default)]
    };

    private static WorkflowInstance SuspendedInstance(WorkflowState state, DateTimeOffset? updatedAt = null, string id = "wf-1") => new()
    {
        Id = id,
        DefinitionId = "def-1",
        DefinitionVersionId = "ver-1",
        Version = 1,
        Status = WorkflowStatus.Running,
        SubStatus = WorkflowSubStatus.Suspended,
        IsExecuting = false,
        UpdatedAt = updatedAt ?? default,
        WorkflowState = state
    };
}
