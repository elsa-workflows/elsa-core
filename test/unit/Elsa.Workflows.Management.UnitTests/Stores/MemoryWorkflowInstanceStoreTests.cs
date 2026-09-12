using Elsa.Common.Services;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Management.Stores;

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

    [Fact(DisplayName = "TryMarkInterruptedAsync refuses Finished/Cancelled unless allowFinishedCancelled is set")]
    public async Task TryMarkInterrupted_DoesNotOverwriteCancelledInstanceByDefault()
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

        var marked = await store.TryMarkInterruptedAsync("cancelled-1");

        Assert.False(marked);
        var instance = await store.FindAsync(new() { Id = "cancelled-1" });
        Assert.NotNull(instance);
        Assert.Equal(WorkflowStatus.Finished, instance.Status);
        Assert.Equal(WorkflowSubStatus.Cancelled, instance.SubStatus);
    }

    [Fact(DisplayName = "TryMarkInterruptedAsync promotes Finished/Cancelled only when allowFinishedCancelled is true")]
    public async Task TryMarkInterrupted_PromotesCancelledWhenAllowed()
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

        var marked = await store.TryMarkInterruptedAsync("cancelled-1", allowFinishedCancelled: true);

        Assert.True(marked);
        var instance = await store.FindAsync(new() { Id = "cancelled-1" });
        Assert.NotNull(instance);
        Assert.Equal(WorkflowStatus.Running, instance.Status);
        Assert.Equal(WorkflowSubStatus.Interrupted, instance.SubStatus);
        Assert.False(instance.IsExecuting);
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

    private static MemoryWorkflowInstanceStore CreateStore(WorkflowInstance instance)
    {
        var store = new MemoryWorkflowInstanceStore(new MemoryStore<WorkflowInstance>());
        store.SaveAsync(instance).AsTask().GetAwaiter().GetResult();
        return store;
    }
}
