using Elsa.Common.Models;
using Elsa.Common.Services;
using Elsa.Extensions;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Management.Models;
using Elsa.Workflows.Management.Stores;
using Elsa.Workflows.Models;

namespace Elsa.Workflows.Management.UnitTests.Stores;

/// <summary>
/// <see cref="MemoryWorkflowDefinitionStore.TryUpdateLatestAsync"/> is the compare-and-swap the BPMN document
/// PUT uses: load, match, apply, save in one critical section. Lost match is Conflict, not an overwrite.
/// </summary>
public class MemoryWorkflowDefinitionStoreCompareAndSwapTests
{
    [Fact(DisplayName = "A matching latest row is updated and the callback sees that just-loaded row")]
    public async Task TryUpdateLatestAsync_WhenTheRowMatches_SavesTheUpdateBuiltFromTheLoadedRow()
    {
        var store = new MemoryWorkflowDefinitionStore(new MemoryStore<WorkflowDefinition>());
        var current = Definition("def-1", "id-1", name: "Original", stringData: "graph-v1");
        await store.SaveAsync(current);

        var result = await store.TryUpdateLatestAsync(
            LatestOf("def-1"),
            loaded => loaded.StringData == "graph-v1",
            loaded =>
            {
                var next = loaded.ShallowClone();
                next.StringData = "graph-v2";
                next.Name = loaded.Name + "-kept";
                return next;
            });

        Assert.Equal(WorkflowDefinitionUpdateOutcome.Updated, result.Outcome);
        Assert.Equal("graph-v2", result.Definition!.StringData);
        Assert.Equal("Original-kept", result.Definition.Name);

        var stored = await store.FindAsync(LatestOf("def-1"));
        Assert.Equal("graph-v2", stored!.StringData);
        Assert.Equal("Original-kept", stored.Name);
    }

    [Fact(DisplayName = "A match that fails is Conflict and the stored row is unchanged")]
    public async Task TryUpdateLatestAsync_WhenTheRowDoesNotMatch_ReturnsConflictAndWritesNothing()
    {
        var store = new MemoryWorkflowDefinitionStore(new MemoryStore<WorkflowDefinition>());
        await store.SaveAsync(Definition("def-1", "id-1", name: "Original", stringData: "graph-v2"));

        var result = await store.TryUpdateLatestAsync(
            LatestOf("def-1"),
            loaded => loaded.StringData == "graph-v1",
            loaded =>
            {
                var next = loaded.ShallowClone();
                next.StringData = "should-not-be-saved";
                return next;
            });

        Assert.Equal(WorkflowDefinitionUpdateOutcome.Conflict, result.Outcome);
        Assert.Null(result.Definition);

        var stored = await store.FindAsync(LatestOf("def-1"));
        Assert.Equal("graph-v2", stored!.StringData);
        Assert.Equal("Original", stored.Name);
    }

    [Fact(DisplayName = "A missing definition is NotFound")]
    public async Task TryUpdateLatestAsync_WhenNothingMatchesTheFilter_ReturnsNotFound()
    {
        var store = new MemoryWorkflowDefinitionStore(new MemoryStore<WorkflowDefinition>());

        var result = await store.TryUpdateLatestAsync(
            LatestOf("missing"),
            _ => true,
            current => current);

        Assert.Equal(WorkflowDefinitionUpdateOutcome.NotFound, result.Outcome);
    }

    [Fact(DisplayName = "Two scoped wrappers share the backing-store lock: the loser is Conflict and the winner's graph stays")]
    public async Task TryUpdateLatestAsync_WhenTwoWrappersShareTheBackingStore_LoserIsConflictAndWinnerGraphStays()
    {
        var backing = new MemoryStore<WorkflowDefinition>();
        var writerA = new MemoryWorkflowDefinitionStore(backing);
        var writerB = new MemoryWorkflowDefinitionStore(backing);
        await writerA.SaveAsync(Definition("def-1", "id-1", name: "Original", stringData: "graph-v1"));

        var firstHoldsLock = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseFirst = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var first = writerA.TryUpdateLatestAsync(
            LatestOf("def-1"),
            loaded => loaded.StringData == "graph-v1",
            loaded =>
            {
                firstHoldsLock.TrySetResult();
                releaseFirst.Task.GetAwaiter().GetResult();
                var next = loaded.ShallowClone();
                next.StringData = "winner-graph";
                return next;
            });

        await firstHoldsLock.Task;

        var second = Task.Run(() => writerB.TryUpdateLatestAsync(
            LatestOf("def-1"),
            loaded => loaded.StringData == "graph-v1",
            loaded =>
            {
                var next = loaded.ShallowClone();
                next.StringData = "stale-overwrite";
                return next;
            }));

        // Writer B must still be waiting on the shared lock. A per-wrapper lock would have
        // already written the stale graph and completed as Updated.
        await Task.Delay(100);
        Assert.False(second.IsCompleted);

        releaseFirst.TrySetResult();

        var firstResult = await first;
        var secondResult = await second;

        Assert.Equal(WorkflowDefinitionUpdateOutcome.Updated, firstResult.Outcome);
        Assert.Equal(WorkflowDefinitionUpdateOutcome.Conflict, secondResult.Outcome);

        var stored = await writerA.FindAsync(LatestOf("def-1"));
        Assert.Equal("winner-graph", stored!.StringData);
    }

    [Fact(DisplayName = "A loaded row that is no longer IsLatest is Conflict and writes nothing")]
    public async Task TryUpdateLatestAsync_WhenTheLoadedRowIsNoLongerLatest_ReturnsConflictAndWritesNothing()
    {
        var backing = new MemoryStore<WorkflowDefinition>();
        var store = new MemoryWorkflowDefinitionStore(backing);
        var published = Definition("def-1", "id-1", name: "Published", stringData: "graph-v1");
        published.IsPublished = true;
        await store.SaveAsync(published);

        var winner = await store.TryUpdateLatestAsync(
            LatestOf("def-1"),
            _ => true,
            loaded =>
            {
                var draft = loaded.ShallowClone();
                draft.Id = "id-2";
                draft.Version = loaded.Version + 1;
                draft.IsPublished = false;
                draft.StringData = "winner-draft";
                return draft;
            });

        Assert.Equal(WorkflowDefinitionUpdateOutcome.Updated, winner.Outcome);

        var loser = await store.TryUpdateLatestAsync(
            new WorkflowDefinitionFilter { Id = published.Id },
            _ => true,
            loaded =>
            {
                var draft = loaded.ShallowClone();
                draft.Id = "id-3";
                draft.Version = loaded.Version + 1;
                draft.IsPublished = false;
                draft.StringData = "should-not-be-saved";
                return draft;
            });

        Assert.Equal(WorkflowDefinitionUpdateOutcome.Conflict, loser.Outcome);
        Assert.Null(loser.Definition);

        var stored = await store.FindAsync(LatestOf("def-1"));
        Assert.Equal("id-2", stored!.Id);
        Assert.Equal("winner-draft", stored.StringData);
    }

    private static WorkflowDefinitionFilter LatestOf(string definitionId) =>
        WorkflowDefinitionHandle.ByDefinitionId(definitionId, VersionOptions.Latest).ToFilter();

    private static WorkflowDefinition Definition(string definitionId, string id, string name, string stringData) =>
        new()
        {
            Id = id,
            DefinitionId = definitionId,
            Name = name,
            StringData = stringData,
            Version = 1,
            IsLatest = true,
            MaterializerName = "Json"
        };
}
