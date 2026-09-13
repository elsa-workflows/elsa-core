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
    [Test]
    [DisplayName("A matching latest row is updated and the callback sees that just-loaded row")]
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

        await Assert.That(result.Outcome).IsEqualTo(WorkflowDefinitionUpdateOutcome.Updated);
        await Assert.That(result.Definition!.StringData).IsEqualTo("graph-v2");
        await Assert.That(result.Definition.Name).IsEqualTo("Original-kept");

        var stored = await store.FindAsync(LatestOf("def-1"));
        await Assert.That(stored!.StringData).IsEqualTo("graph-v2");
        await Assert.That(stored.Name).IsEqualTo("Original-kept");
    }

    [Test]
    [DisplayName("A match that fails is Conflict and the stored row is unchanged")]
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

        await Assert.That(result.Outcome).IsEqualTo(WorkflowDefinitionUpdateOutcome.Conflict);
        await Assert.That(result.Definition).IsNull();

        var stored = await store.FindAsync(LatestOf("def-1"));
        await Assert.That(stored!.StringData).IsEqualTo("graph-v2");
        await Assert.That(stored.Name).IsEqualTo("Original");
    }

    [Test]
    [DisplayName("A missing definition is NotFound")]
    public async Task TryUpdateLatestAsync_WhenNothingMatchesTheFilter_ReturnsNotFound()
    {
        var store = new MemoryWorkflowDefinitionStore(new MemoryStore<WorkflowDefinition>());

        var result = await store.TryUpdateLatestAsync(
            LatestOf("missing"),
            _ => true,
            current => current);

        await Assert.That(result.Outcome).IsEqualTo(WorkflowDefinitionUpdateOutcome.NotFound);
    }

    [Test]
    [DisplayName("Two scoped wrappers share the backing-store lock: the loser is Conflict and the winner's graph stays")]
    public async Task TryUpdateLatestAsync_WhenTwoWrappersShareTheBackingStore_LoserIsConflictAndWinnerGraphStays()
    {
        var backing = new MemoryStore<WorkflowDefinition>();
        var writerA = new MemoryWorkflowDefinitionStore(backing);
        var writerB = new MemoryWorkflowDefinitionStore(backing);
        await writerA.SaveAsync(Definition("def-1", "id-1", name: "Original", stringData: "graph-v1"));

        var timeout = TimeSpan.FromSeconds(5);
        var firstHoldsLock = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseFirst = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var secondAttempting = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var secondCompleted = new TaskCompletionSource<WorkflowDefinitionUpdateResult>(TaskCreationOptions.RunContinuationsAsynchronously);

        var first = Task.Run(() => writerA.TryUpdateLatestAsync(
            LatestOf("def-1"),
            loaded => loaded.StringData == "graph-v1",
            loaded =>
            {
                firstHoldsLock.TrySetResult();
                releaseFirst.Task.GetAwaiter().GetResult();
                var next = loaded.ShallowClone();
                next.StringData = "winner-graph";
                return next;
            }));

        var secondThread = new Thread(() =>
        {
            try
            {
                secondAttempting.TrySetResult();
                var result = writerB.TryUpdateLatestAsync(
                        LatestOf("def-1"),
                        loaded => loaded.StringData == "graph-v1",
                        loaded =>
                        {
                            var next = loaded.ShallowClone();
                            next.StringData = "stale-overwrite";
                            return next;
                        })
                    .GetAwaiter()
                    .GetResult();
                secondCompleted.TrySetResult(result);
            }
            catch (Exception exception)
            {
                secondCompleted.TrySetException(exception);
            }
        })
        {
            IsBackground = true,
            Name = "workflow-definition-cas-writer-b"
        };

        var observedWaiting = false;
        try
        {
            await firstHoldsLock.Task.WaitAsync(timeout);
            secondThread.Start();
            await secondAttempting.Task.WaitAsync(timeout);
            observedWaiting = SpinWait.SpinUntil(
                () => (secondThread.ThreadState & ThreadState.WaitSleepJoin) != 0,
                timeout);
        }
        finally
        {
            // Never strand either writer if an observation or assertion fails.
            releaseFirst.TrySetResult();
        }

        var firstResult = await first.WaitAsync(timeout);
        var secondResult = await secondCompleted.Task.WaitAsync(timeout);
        var secondExited = secondThread.Join(timeout);

        await Assert.That(observedWaiting).IsTrue()
            .Because("Writer B did not block while writer A held the shared backing-store monitor.");
        await Assert.That(secondExited).IsTrue()
            .Because("Writer B did not exit after the shared monitor was released.");

        await Assert.That(firstResult.Outcome).IsEqualTo(WorkflowDefinitionUpdateOutcome.Updated);
        await Assert.That(secondResult.Outcome).IsEqualTo(WorkflowDefinitionUpdateOutcome.Conflict);

        var stored = await writerA.FindAsync(LatestOf("def-1"));
        await Assert.That(stored!.StringData).IsEqualTo("winner-graph");
    }

    [Test]
    [DisplayName("A loaded row that is no longer IsLatest is Conflict and writes nothing")]
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

        await Assert.That(winner.Outcome).IsEqualTo(WorkflowDefinitionUpdateOutcome.Updated);

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

        await Assert.That(loser.Outcome).IsEqualTo(WorkflowDefinitionUpdateOutcome.Conflict);
        await Assert.That(loser.Definition).IsNull();

        var stored = await store.FindAsync(LatestOf("def-1"));
        await Assert.That(stored!.Id).IsEqualTo("id-2");
        await Assert.That(stored.StringData).IsEqualTo("winner-draft");
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
