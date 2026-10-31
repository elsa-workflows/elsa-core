using System.Text.Json.Nodes;
using Elsa.Studio.Workflows.Designer.Models;
using Elsa.Studio.Workflows.Designer.Services;
using Xunit;

namespace Elsa.Studio.Workflows.Designer.Tests;

public class StateMachineEditorSessionTests
{
    [Fact]
    public void RenameState_PreservesVisualIdentityLayoutSemanticOrderAndUnknownJson()
    {
        var session = CreateSession(CreateActivity());
        var before = session.ProjectCanvas();
        var pending = before.States[0];
        var transition = Assert.Single(before.Transitions);

        session.SetStatePosition(pending.VisualId, 125, 75);
        session.RenameState(pending.VisualId, "AwaitingApproval");

        var after = session.ProjectCanvas();
        Assert.Equal(["AwaitingApproval", "Approved"], after.States.Select(x => x.Name));
        Assert.Equal(pending.VisualId, after.States[0].VisualId);
        Assert.Equal(125, after.States[0].Position.X);
        Assert.Equal(75, after.States[0].Position.Y);
        Assert.Equal(transition.VisualId, after.Transitions[0].VisualId);
        Assert.Equal(pending.VisualId, after.Transitions[0].SourceStateVisualId);
        Assert.Equal("AwaitingApproval", after.Transitions[0].From);

        var exported = session.Export();
        Assert.Equal("root-value", exported["unknownRoot"]!.GetValue<string>());
        Assert.Equal("state-value", exported["states"]![0]!["unknownState"]!.GetValue<string>());
        Assert.Equal("transition-value", exported["transitions"]![0]!["unknownTransition"]!.GetValue<string>());
    }

    [Fact]
    public void AddAndDeleteCommands_PreserveAppendOrderAndCascadeConnectedTransitions()
    {
        var session = CreateSession(CreateActivity());
        var initial = session.ProjectCanvas();
        var pendingId = initial.States[0].VisualId;
        var approvedId = initial.States[1].VisualId;

        var archivedId = session.AddState("Archived");
        var archiveTransitionId = session.AddTransition(approvedId, archivedId, "Archive", "Archive order");

        var afterAdd = session.ProjectCanvas();
        Assert.Equal(["Pending", "Approved", "Archived"], afterAdd.States.Select(x => x.Name));
        Assert.Equal(["Approve", "Archive"], afterAdd.Transitions.Select(x => x.Name));
        Assert.Equal(archiveTransitionId, afterAdd.Transitions[1].VisualId);

        session.DeleteState(approvedId);

        var afterDelete = session.ProjectCanvas();
        Assert.Equal(["Pending", "Archived"], afterDelete.States.Select(x => x.Name));
        Assert.Empty(afterDelete.Transitions);
        Assert.Equal("Pending", afterDelete.InitialState);
        Assert.Equal("Pending", afterDelete.CurrentState);
        Assert.Contains(afterDelete.States, x => x.VisualId == pendingId);
    }

    [Fact]
    public void Projection_AssignsDistinctVisualIdsToSemanticallyInvalidDuplicateItems()
    {
        var activity = CreateActivity();
        activity["states"]!.AsArray().Add(activity["states"]![0]!.DeepClone());
        activity["transitions"]!.AsArray().Add(activity["transitions"]![0]!.DeepClone());
        var session = CreateSession(activity);

        var canvas = session.ProjectCanvas();

        Assert.Equal(3, canvas.States.Select(x => x.VisualId).Distinct().Count());
        Assert.Equal(2, canvas.Transitions.Select(x => x.VisualId).Distinct().Count());
        Assert.False(session.CanExport);
        Assert.Throws<InvalidOperationException>(() => session.Export());
        Assert.Contains(session.ValidationIssues, x => x.Code == "DuplicateStateName");
        Assert.Contains(session.ValidationIssues, x => x.Code == "DuplicateTransitionIdentity");
        Assert.Contains(canvas.States, x => x.HasValidationErrors && x.ValidationIssueCount > 0);
        Assert.Contains(canvas.Transitions, x => x.HasValidationErrors && x.ValidationIssueCount > 0);

        var x6 = StateMachineX6Mapper.Map(canvas);
        Assert.Contains(x6.Nodes, x => x.Attrs["body"]?["stroke"]?.GetValue<string>() == "var(--mud-palette-error)");
        Assert.Contains(x6.Nodes, x => x.Data["accessibleName"]?.GetValue<string>().Contains("validation issue", StringComparison.Ordinal) == true);
        Assert.Contains(x6.Edges, x => x.Attrs["line"]?["stroke"]?.GetValue<string>() == "var(--mud-palette-error)");
    }

    [Fact]
    public void InvalidTransition_WithBothEndpointsMissing_CanBeRepairedOneEndpointAtATime()
    {
        var activity = CreateActivity();
        activity["transitions"]![0]!["from"] = "Missing source";
        activity["transitions"]![0]!["to"] = "Missing target";
        var session = CreateSession(activity);
        var invalid = Assert.Single(session.ProjectCanvas().Transitions);

        Assert.Null(invalid.SourceStateVisualId);
        Assert.Null(invalid.TargetStateVisualId);
        Assert.True(invalid.HasValidationErrors);
        Assert.False(session.CanExport);

        var x6 = StateMachineX6Mapper.Map(session.ProjectCanvas());
        var unresolvedEndpoints = x6.Nodes.Where(x => x.Data["kind"]?.GetValue<string>() == "missing-state").ToList();
        Assert.Equal(2, unresolvedEndpoints.Count);
        Assert.All(unresolvedEndpoints, x => Assert.Equal(invalid.VisualId, x.Data["transitionVisualId"]?.GetValue<string>()));

        var states = session.ProjectCanvas().States;
        session.SetTransitionSource(invalid.VisualId, states[0].VisualId);

        var partlyRepaired = Assert.Single(session.ProjectCanvas().Transitions);
        Assert.Equal(states[0].VisualId, partlyRepaired.SourceStateVisualId);
        Assert.Null(partlyRepaired.TargetStateVisualId);
        Assert.False(session.CanExport);

        session.SetTransitionTarget(invalid.VisualId, states[1].VisualId);

        var repaired = Assert.Single(session.ProjectCanvas().Transitions);
        Assert.Equal(states[0].VisualId, repaired.SourceStateVisualId);
        Assert.Equal(states[1].VisualId, repaired.TargetStateVisualId);
        Assert.True(session.CanExport);
        var exported = session.Export();
        Assert.Equal("Pending", exported["transitions"]![0]!["from"]!.GetValue<string>());
        Assert.Equal("Approved", exported["transitions"]![0]!["to"]!.GetValue<string>());
    }

    [Fact]
    public void SlotCommands_CloneInputAndPreserveUnchangedSlotJson()
    {
        var session = CreateSession(CreateActivity());
        var canvas = session.ProjectCanvas();
        var pendingId = canvas.States[0].VisualId;
        var transitionId = canvas.Transitions[0].VisualId;
        var replacementEntry = JsonNode.Parse("""{ "id": "Entry2", "nodeId": "Machine1:Entry2", "name": "WriteLine3", "type": "Elsa.WriteLine", "version": 1, "custom": true }""")!;
        var replacementAction = JsonNode.Parse("""{ "id": "Action1", "nodeId": "Machine1:Action1", "name": "WriteLine4", "type": "Elsa.WriteLine", "version": 1 }""")!;

        session.SetStateSlot(pendingId, StateMachineStateSlot.Entry, replacementEntry);
        session.SetTransitionSlot(transitionId, StateMachineTransitionSlot.Action, replacementAction);
        replacementEntry["custom"] = false;

        var exported = session.Export();
        Assert.True(exported["states"]![0]!["entry"]!["custom"]!.GetValue<bool>());
        Assert.Equal("keep-exit", exported["states"]![0]!["exit"]!["unknown"]!.GetValue<string>());
        Assert.Equal("Action1", exported["transitions"]![0]!["action"]!["id"]!.GetValue<string>());
    }

    [Fact]
    public void SetTransitionCondition_ChangesOnlyConditionAndPreservesOtherTransitionJson()
    {
        var activity = CreateActivity();
        activity["transitions"]![0]!["trigger"] = new JsonObject
        {
            ["id"] = "Trigger1", ["nodeId"] = "Machine1:Trigger1", ["name"] = "Event", ["type"] = "Elsa.Event", ["version"] = 1, ["opaque"] = true
        };
        activity["transitions"]![0]!["condition"] = new JsonObject { ["type"] = "Contoso.Condition", ["payload"] = "keep" };
        activity["transitions"]![0]!["action"] = new JsonObject
        {
            ["id"] = "Action1", ["nodeId"] = "Machine1:Action1", ["name"] = "WriteLine", ["type"] = "Elsa.WriteLine", ["version"] = 1, ["text"] = "before"
        };
        var session = CreateSession(activity);
        var transition = session.ProjectCanvas().Transitions.Single();
        var before = session.Export();
        var replacement = new JsonObject
        {
            ["typeName"] = "Boolean",
            ["expression"] = new JsonObject { ["type"] = "Literal", ["value"] = false }
        };

        session.SetTransitionSlot(transition.VisualId, StateMachineTransitionSlot.Condition, replacement);
        replacement["expression"]!["value"] = true;

        var after = session.Export();
        var beforeTransition = before["transitions"]![0]!.AsObject();
        var afterTransition = after["transitions"]![0]!.AsObject();
        Assert.True(JsonNode.DeepEquals(before["unknownRoot"], after["unknownRoot"]));
        Assert.True(JsonNode.DeepEquals(before["states"], after["states"]));
        Assert.True(JsonNode.DeepEquals(beforeTransition["trigger"], afterTransition["trigger"]));
        Assert.True(JsonNode.DeepEquals(beforeTransition["action"], afterTransition["action"]));
        Assert.Equal("transition-value", afterTransition["unknownTransition"]!.GetValue<string>());
        Assert.False(afterTransition["condition"]!["expression"]!["value"]!.GetValue<bool>());
    }

    [Fact]
    public void AddState_AfterDeletion_DoesNotReuseAnOccupiedDefaultPosition()
    {
        var session = CreateSession(CreateActivity());
        var firstStateId = session.ProjectCanvas().States[0].VisualId;

        session.DeleteState(firstStateId);
        session.AddState("Archived");

        var positions = session.ProjectCanvas().States.Select(x => x.Position).ToList();
        Assert.Equal(positions.Count, positions.Distinct().Count());
    }

    [Fact]
    public void X6Projection_UsesStableIdsNativeShapesPortsLabelsAndSessionGeometry()
    {
        var session = CreateSession(CreateActivity());
        var canvas = session.ProjectCanvas();
        session.SetStatePosition(canvas.States[0].VisualId, 120, 80);
        session.SetTransitionVertices(canvas.Transitions[0].VisualId, [new(300, 140)]);

        var graph = StateMachineX6Mapper.Map(session.ProjectCanvas());

        Assert.All(graph.Nodes, node => Assert.Equal("elsa-state-machine-state", node.Shape));
        Assert.Equal(120, graph.Nodes.First().Position.X);
        Assert.Equal(["in", "out"], graph.Nodes.First().Ports.Items.Select(x => x.Id));
        var edge = Assert.Single(graph.Edges);
        Assert.Equal("elsa-state-machine-transition", edge.Shape);
        Assert.Equal(canvas.Transitions[0].VisualId, edge.Id);
        Assert.Equal(canvas.States[0].VisualId, edge.Source.Cell);
        Assert.Equal(canvas.States[1].VisualId, edge.Target.Cell);
        Assert.Equal("Approve, transition from Pending to Approved", edge.Data["accessibleName"]?.GetValue<string>());
        Assert.Single(edge.Labels);
        Assert.Single(edge.Vertices);
    }

    private static StateMachineEditorSession CreateSession(JsonObject activity)
    {
        var validator = new StateMachineValidator();
        return new(new StateMachineMapper(validator), validator, activity);
    }

    private static JsonObject CreateActivity() => JsonNode.Parse("""
    {
      "id": "Machine1",
      "nodeId": "Workflow1:Machine1",
      "type": "Elsa.StateMachine",
      "initialState": "Pending",
      "currentState": "Pending",
      "unknownRoot": "root-value",
      "states": [
        {
          "name": "Pending",
          "unknownState": "state-value",
          "entry": { "id": "Entry1", "nodeId": "Machine1:Entry1", "name": "WriteLine1", "type": "Elsa.WriteLine", "version": 1 },
          "exit": { "id": "Exit1", "nodeId": "Machine1:Exit1", "name": "WriteLine2", "type": "Elsa.WriteLine", "version": 1, "unknown": "keep-exit" }
        },
        { "name": "Approved" }
      ],
      "transitions": [
        {
          "name": "Approve",
          "displayName": "Approve order",
          "from": "Pending",
          "to": "Approved",
          "unknownTransition": "transition-value"
        }
      ]
    }
    """)!.AsObject();
}
