using System.Text.Json;
using Elsa.Api.Client.Resources.WorkflowInstances.Models;
using Elsa.Studio.Workflows.Domain.Models.Bpmn;
using Xunit;

namespace Elsa.Studio.Workflows.Tests;

/// <summary>
/// Covers <see cref="BpmnElementStatsProjector"/>: folding the BPMN diagnostics elsa-core projects onto a workflow
/// instance's journal (see <see cref="BpmnDiagnosticEventNames"/> and <see cref="BpmnDiagnosticLogPayload"/>) into
/// the element-keyed overlay the instance viewer's canvas reads.
/// </summary>
/// <remarks>
/// The parallel-gateway scenario below is this item's own verification scenario (D1): a join waiting on a still
/// blocked sibling, and a completed branch whose flows were taken, both visible with nothing bound to the gateway
/// itself.
/// </remarks>
public class BpmnElementStatsProjectorTests
{
    [Fact(DisplayName = "A parallel split where one branch blocks: the join is waiting, and the completed branch's flows are taken")]
    public void Project_ParallelSplitOneBranchBlocked_ShowsTheJoinWaitingAndTheCompletedBranchesFlowsTaken()
    {
        var entries = new[]
        {
            DiagnosticEntry(BpmnDiagnosticEventNames.TokenEmitted, flowId: "flow-split-completed"),
            DiagnosticEntry(BpmnDiagnosticEventNames.TokenEmitted, flowId: "flow-completed-join"),
            DiagnosticEntry(BpmnDiagnosticEventNames.Waiting, elementId: "join"),
        };

        var stats = BpmnElementStatsProjector.Project(entries);

        Assert.True(stats["join"].Blocked);
        Assert.True(IsTaken(stats["flow-split-completed"]));
        Assert.True(IsTaken(stats["flow-completed-join"]));
    }

    [Fact(DisplayName = "A join firing clears its blocked flag and counts as completed")]
    public void Project_Joined_ClearsBlockedAndCountsAsCompleted()
    {
        var entries = new[]
        {
            DiagnosticEntry(BpmnDiagnosticEventNames.Waiting, elementId: "join"),
            DiagnosticEntry(BpmnDiagnosticEventNames.Joined, elementId: "join"),
        };

        var stats = BpmnElementStatsProjector.Project(entries).Single(x => x.Key == "join").Value;

        Assert.False(stats.Blocked);
        Assert.Equal(1, stats.Completed);
    }

    [Fact(DisplayName = "A single diagnostic naming both an element and a flow updates both entries independently")]
    public void Project_DiagnosticNamingBothAnElementAndAFlow_UpdatesBothEntries()
    {
        var entries = new[] { DiagnosticEntry(BpmnDiagnosticEventNames.TokenEmitted, elementId: "catch-event", flowId: "flow-into-catch") };

        var stats = BpmnElementStatsProjector.Project(entries);

        Assert.Equal(1, stats["catch-event"].Started);
        Assert.Equal(1, stats["catch-event"].Active);
        Assert.True(IsTaken(stats["flow-into-catch"]));
    }

    [Theory(DisplayName = "Faulted and BehaviorFailure both mark the element faulted")]
    [InlineData(nameof(BpmnDiagnosticEventNames.Faulted))]
    [InlineData(nameof(BpmnDiagnosticEventNames.BehaviorFailure))]
    [InlineData(nameof(BpmnDiagnosticEventNames.CallActivityFailureRouted))]
    public void Project_FaultKinds_MarkTheElementFaulted(string kind)
    {
        var entries = new[] { DiagnosticEntry(kind, elementId: "task") };

        var stats = BpmnElementStatsProjector.Project(entries);

        Assert.True(stats["task"].Faulted);
    }

    [Theory(DisplayName = "EscalationUnhandled and EscalationLate are documented as never a fault, and must not be folded into one")]
    [InlineData(nameof(BpmnDiagnosticEventNames.EscalationUnhandled))]
    [InlineData(nameof(BpmnDiagnosticEventNames.EscalationLate))]
    public void Project_NeverFaultKinds_DoNotMarkTheElementFaulted(string kind)
    {
        var entries = new[] { DiagnosticEntry(kind, elementId: "boundary") };

        var stats = BpmnElementStatsProjector.Project(entries);

        Assert.False(stats["boundary"].Faulted ?? false);
    }

    [Fact(DisplayName = "Canceled marks the element cancelled and decrements its active count")]
    public void Project_Canceled_MarksCancelledAndDecrementsActive()
    {
        var entries = new[]
        {
            DiagnosticEntry(BpmnDiagnosticEventNames.Scheduled, elementId: "task"),
            DiagnosticEntry(BpmnDiagnosticEventNames.Canceled, elementId: "task"),
        };

        var stats = BpmnElementStatsProjector.Project(entries)["task"];

        Assert.True(stats.Canceled);
        Assert.Equal(0, stats.Active);
    }

    [Fact(DisplayName = "Entries from a nested scope's own activity land in the same, single element-keyed map")]
    public void Project_EntriesFromDifferentScopeActivities_FoldIntoTheSameMap()
    {
        // Diagnostics for the outer BpmnProcess scope and a nested one are both written on their own scope's
        // activity id (never a child's), but element and flow ids are unique across the whole document, so the
        // resulting map is global per instance rather than segregated by which scope wrote the entry.
        var entries = new[]
        {
            DiagnosticEntry(BpmnDiagnosticEventNames.TokenEmitted, elementId: "outer-task", activityId: "outer-scope"),
            DiagnosticEntry(BpmnDiagnosticEventNames.Waiting, elementId: "inner-join", activityId: "inner-scope"),
        };

        var stats = BpmnElementStatsProjector.Project(entries);

        Assert.Equal(2, stats.Count);
        Assert.Equal(1, stats["outer-task"].Started);
        Assert.True(stats["inner-join"].Blocked);
    }

    [Fact(DisplayName = "Entries whose Source is not BPMN are ignored, even if their event name coincides with a diagnostic kind")]
    public void Project_NonBpmnSourcedEntries_AreIgnored()
    {
        var entry = DiagnosticEntry(BpmnDiagnosticEventNames.Faulted, elementId: "task") with { Source = "SomethingElse" };

        var stats = BpmnElementStatsProjector.Project([entry]);

        Assert.Empty(stats);
    }

    [Fact(DisplayName = "The scope's own terminal Completed diagnostic, which names neither an element nor a flow, contributes nothing")]
    public void Project_ScopeCompletionDiagnostic_ContributesNothing()
    {
        var entries = new[] { DiagnosticEntry(BpmnDiagnosticEventNames.Completed) };

        var stats = BpmnElementStatsProjector.Project(entries);

        Assert.Empty(stats);
    }

    [Fact(DisplayName = "A payload that arrives as a JsonElement -- exactly how it comes over the wire -- is read the same as a payload constructed directly")]
    public void Project_PayloadAsJsonElement_IsReadCorrectly()
    {
        var json = """{"diagnosticId":"diag:1","elementId":"join","flowId":null,"tokenId":null,"kind":"Waiting","details":{}}""";
        var payload = JsonSerializer.Deserialize<JsonElement>(json);
        var entry = new WorkflowExecutionLogRecord(
            Id: "log-1",
            ActivityInstanceId: "instance-1",
            ParentActivityInstanceId: null,
            ActivityId: "scope",
            ActivityType: "Elsa.BpmnProcess",
            ActivityTypeVersion: 1,
            ActivityName: null,
            NodeId: "scope-node",
            Timestamp: DateTimeOffset.UtcNow,
            Sequence: 1,
            EventName: BpmnDiagnosticEventNames.Waiting,
            Message: null,
            Source: BpmnDiagnosticEventNames.Source,
            ActivityState: null,
            Payload: payload);

        var stats = BpmnElementStatsProjector.Project([entry]);

        Assert.True(stats["join"].Blocked);
    }

    [Fact(DisplayName = "Fold mutates an already-populated map in place, combining new entries with what was already folded")]
    public void Fold_AlreadyPopulatedMap_CombinesNewEntriesWithExistingOnes()
    {
        var stats = new Dictionary<string, BpmnElementStats>();
        BpmnElementStatsProjector.Fold([DiagnosticEntry(BpmnDiagnosticEventNames.Waiting, elementId: "join")], stats);

        BpmnElementStatsProjector.Fold([DiagnosticEntry(BpmnDiagnosticEventNames.TokenEmitted, elementId: "task")], stats);

        Assert.Equal(2, stats.Count);
        Assert.True(stats["join"].Blocked);
        Assert.Equal(1, stats["task"].Started);
    }

    [Fact(DisplayName = "Folding the same element's records across two refreshes matches a single pass over all of them")]
    public void Fold_SameElementAcrossTwoRefreshes_MatchesASinglePassOverAllRecords()
    {
        // The join's Waiting record (refresh 1's whole take) lands first and marks it blocked; its later Joined
        // record (refresh 2's whole take) must still clear that blocked flag rather than the map getting stuck on
        // whatever refresh 1 last saw.
        var waiting = DiagnosticEntry(BpmnDiagnosticEventNames.Waiting, elementId: "join");
        var joined = DiagnosticEntry(BpmnDiagnosticEventNames.Joined, elementId: "join");

        var stats = new Dictionary<string, BpmnElementStats>();
        BpmnElementStatsProjector.Fold([waiting], stats);
        BpmnElementStatsProjector.Fold([joined], stats);

        var expected = BpmnElementStatsProjector.Project([waiting, joined]);

        Assert.Equal(expected.Keys.OrderBy(k => k), stats.Keys.OrderBy(k => k));
        var expectedJoin = expected["join"];
        var actualJoin = stats["join"];
        Assert.Equal(expectedJoin.Started, actualJoin.Started);
        Assert.Equal(expectedJoin.Completed, actualJoin.Completed);
        Assert.Equal(expectedJoin.Active, actualJoin.Active);
        Assert.Equal(expectedJoin.Blocked, actualJoin.Blocked);
        Assert.Equal(expectedJoin.Faulted, actualJoin.Faulted);
        Assert.Equal(expectedJoin.Canceled, actualJoin.Canceled);
        Assert.False(actualJoin.Blocked);
        Assert.Equal(1, actualJoin.Completed);
    }

    private static bool IsTaken(BpmnElementStats stats) => (stats.Started ?? 0) > 0 || (stats.Completed ?? 0) > 0;

    private static WorkflowExecutionLogRecord DiagnosticEntry(
        string kind,
        string? elementId = null,
        string? flowId = null,
        string activityId = "scope") => new(
        Id: Guid.NewGuid().ToString(),
        ActivityInstanceId: "instance-1",
        ParentActivityInstanceId: null,
        ActivityId: activityId,
        ActivityType: "Elsa.BpmnProcess",
        ActivityTypeVersion: 1,
        ActivityName: null,
        NodeId: $"{activityId}-node",
        Timestamp: DateTimeOffset.UtcNow,
        Sequence: 1,
        EventName: kind,
        Message: null,
        Source: BpmnDiagnosticEventNames.Source,
        ActivityState: null,
        Payload: new BpmnDiagnosticLogPayload(Guid.NewGuid().ToString(), elementId, flowId, null, kind, new Dictionary<string, string>()));
}
