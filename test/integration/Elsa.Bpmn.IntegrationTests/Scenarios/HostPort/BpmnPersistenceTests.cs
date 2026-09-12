using Bpmn.Model.State;
using Elsa.Workflows;
using Xunit.Abstractions;

namespace Elsa.Bpmn.IntegrationTests.Scenarios.HostPort;

/// <summary>
/// What survives a suspend and a resume: the pruned <see cref="BpmnExecutionState"/> and the host-side work ledger,
/// both persisted as JSON strings in <see cref="ActivityExecutionContext.Properties"/>.
/// </summary>
public class BpmnPersistenceTests(ITestOutputHelper testOutputHelper)
{
    private readonly BpmnTestHost _host = new(testOutputHelper);

    [Fact(DisplayName = "The persisted execution state stays bounded across many evaluations of one scope")]
    public async Task PersistedExecutionState_StaysBoundedAcrossManyEvaluations()
    {
        // A sequential multi-instance loop drives one evaluation per iteration, all on the same scope. Every
        // iteration but the last consumes a token that nothing afterward can ever reference again: an interpreter
        // state that is written without being pruned first re-serializes every one of them, so the token count
        // grows with the iteration count instead of staying flat.

        const int cardinality = 12;

        // Arrange & Act: run every iteration but the last, capturing the persisted token count while the scope is
        // still suspended and mid-loop after each one. The scope's own context stops being persisted the moment it
        // completes, so the last iteration -- which also completes "after" and the scope itself -- is run separately.
        await _host.RunAsync(BpmnTestProcesses.SequentialMultiInstanceTask(_host.Log, cardinality));

        var maxTokenCount = 0;

        for (var i = 0; i < cardinality - 1; i++)
        {
            await _host.FinishWorkAsync("each");

            var (state, _) = _host.PersistedScopeMemory();
            Assert.NotNull(state);
            maxTokenCount = Math.Max(maxTokenCount, state.Tokens.Count);
        }

        await _host.FinishWorkAsync("each");

        // Assert: the loop actually ran to completion...
        Assert.Equal(cardinality, _host.Log.Occurrences("executed:each"));
        Assert.Equal(1, _host.Log.Occurrences("executed:after"));

        // ...and the persisted state's token count stayed flat rather than growing with the iteration count. Without
        // pruning, a consumed token from every prior iteration would still be there by the time the loop is nearly
        // done, so the count would climb toward the iteration count instead of staying near-constant.
        Assert.True(
            maxTokenCount < cardinality,
            $"Expected the persisted token count to stay well below the iteration count ({cardinality}) because Prune() drops consumed tokens no active work references, but the highest observed count was {maxTokenCount}.");
    }

    [Fact(DisplayName = "A scope resumed from a JSON round trip matches each completion back to its binding through the rehydrated ledger")]
    public async Task ResumedScope_WithTwoUnitsOfLiveWork_MatchesEachCompletionToItsBindingThroughTheRehydratedLedger()
    {
        // Arrange: suspend with two live units of work outstanding, both bound under the same scope.
        await _host.RunAsync(BpmnTestProcesses.ParallelSplitAndJoinBlocking(_host.Log));

        var (_, ledgerBeforeResume) = _host.PersistedScopeMemory();
        Assert.Equal(2, ledgerBeforeResume.Records.Count);
        Assert.Contains(ledgerBeforeResume.Records, x => x.BindingRef == BpmnTestProcesses.BindingRef("left"));
        Assert.Contains(ledgerBeforeResume.Records, x => x.BindingRef == BpmnTestProcesses.BindingRef("right"));

        // Act: round-trip the workflow state through Elsa's own serializer -- what a real persistence store would
        // do -- and resume each branch by name, one at a time, round-tripping again between them.
        _host.RoundTripStateThroughJson();

        // Assert: the ledger the resumed scope would read from is intact before either branch is even resumed.
        var (_, ledgerAfterResume) = _host.PersistedScopeMemory();
        Assert.Equal(2, ledgerAfterResume.Records.Count);
        Assert.Contains(ledgerAfterResume.Records, x => x.BindingRef == BpmnTestProcesses.BindingRef("left"));
        Assert.Contains(ledgerAfterResume.Records, x => x.BindingRef == BpmnTestProcesses.BindingRef("right"));

        await _host.FinishWorkAsync("left");
        _host.RoundTripStateThroughJson();
        var result = await _host.FinishWorkAsync("right");

        // Assert: both branches ran, and the join fired exactly once. Neither is possible unless the resumed scope's
        // ledger still mapped each completing child context back to its own binding: a scope with a lost or shared
        // handle map either drops a completion outright (the join never fires and the workflow never finishes) or
        // cannot tell the two branches apart (the join fires more than once).
        Assert.Contains("resumed:left", _host.Log.Entries);
        Assert.Contains("resumed:right", _host.Log.Entries);
        Assert.Equal(1, _host.Log.Occurrences("executed:after"));
        Assert.Equal(WorkflowSubStatus.Finished, result.WorkflowState.SubStatus);
    }

    [Fact(DisplayName = "A nested scope resumed from a JSON round trip matches each completion back to its binding through its own rehydrated ledger")]
    public async Task ResumedNestedScope_WithTwoUnitsOfLiveWork_MatchesEachCompletionToItsBindingThroughTheRehydratedLedger()
    {
        // Arrange: suspend with two live units of work outstanding inside the nested scope -- the embedded
        // subprocess -- while the root scope's own ledger holds only the subprocess itself.
        await _host.RunAsync(BpmnTestProcesses.NestedParallelSplitAndJoinBlocking(_host.Log));

        var (_, nestedLedgerBeforeResume) = _host.PersistedScopeMemory(nested: true);
        Assert.Equal(2, nestedLedgerBeforeResume.Records.Count);
        Assert.Contains(nestedLedgerBeforeResume.Records, x => x.BindingRef == BpmnTestProcesses.BindingRef("subLeft"));
        Assert.Contains(nestedLedgerBeforeResume.Records, x => x.BindingRef == BpmnTestProcesses.BindingRef("subRight"));

        // Act: round-trip the workflow state through Elsa's own serializer, then resume each branch by name, one at
        // a time, round-tripping again between them -- exactly as the root-scope test above does, but crossing into
        // the nested scope's own ledger instead of the root's.
        _host.RoundTripStateThroughJson();

        // Assert: the nested scope's own ledger is intact before either branch is even resumed.
        var (_, nestedLedgerAfterResume) = _host.PersistedScopeMemory(nested: true);
        Assert.Equal(2, nestedLedgerAfterResume.Records.Count);
        Assert.Contains(nestedLedgerAfterResume.Records, x => x.BindingRef == BpmnTestProcesses.BindingRef("subLeft"));
        Assert.Contains(nestedLedgerAfterResume.Records, x => x.BindingRef == BpmnTestProcesses.BindingRef("subRight"));

        await _host.FinishWorkAsync("subLeft");
        _host.RoundTripStateThroughJson();
        var result = await _host.FinishWorkAsync("subRight");

        // Assert: both branches ran, the nested join fired exactly once, and the process ran all the way out to the
        // root scope's own "after" step and completion. None of this is possible unless the resumed nested scope's
        // ledger still mapped each completing child context back to its own binding: a nested scope with a lost or
        // shared handle map either drops a completion outright (the nested join never fires and the process never
        // finishes) or cannot tell the two branches apart (the nested join fires more than once).
        Assert.Contains("resumed:subLeft", _host.Log.Entries);
        Assert.Contains("resumed:subRight", _host.Log.Entries);
        Assert.Equal(1, _host.Log.Occurrences("executed:subAfter"));
        Assert.Equal(1, _host.Log.Occurrences("executed:after"));
        Assert.Equal(WorkflowSubStatus.Finished, result.WorkflowState.SubStatus);
    }
}
