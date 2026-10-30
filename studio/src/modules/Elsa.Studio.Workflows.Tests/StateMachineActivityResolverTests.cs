using System.Text.Json.Nodes;
using Elsa.Studio.Workflows.Domain.Services;
using Elsa.Studio.Workflows.Extensions;
using Elsa.Studio.Workflows.Resolvers;
using Xunit;

namespace Elsa.Studio.Workflows.Tests;

public class StateMachineActivityResolverTests
{
    private readonly DefaultActivityVisitor _visitor = new([
        new StateMachineActivityResolver(),
        new DefaultActivityResolver()
    ]);

    [Fact]
    public async Task VisitAndMapAsync_IndexesStateMachineSlotActivities()
    {
        var activity = CreateActivity();

        var nodes = await _visitor.VisitAndMapAsync(activity);

        Assert.Contains("Workflow1:StateMachine1", nodes.Keys);
        Assert.Contains("Workflow1:StateMachine1:Entry1", nodes.Keys);
        Assert.Contains("Workflow1:StateMachine1:Exit1", nodes.Keys);
        Assert.Contains("Workflow1:StateMachine1:Trigger1", nodes.Keys);
        Assert.Contains("Workflow1:StateMachine1:Action1", nodes.Keys);
        Assert.DoesNotContain("Workflow1:StateMachine1:Condition1", nodes.Keys);
    }

    private static JsonObject CreateActivity() => JsonNode.Parse("""
    {
      "id": "StateMachine1",
      "nodeId": "Workflow1:StateMachine1",
      "type": "Elsa.StateMachine",
      "states": [
        {
          "name": "Pending",
          "entry": {
            "id": "Entry1",
            "nodeId": "Workflow1:StateMachine1:Entry1",
            "name": "WriteLine1",
            "version": 1,
            "type": "Elsa.WriteLine"
          },
          "exit": {
            "id": "Exit1",
            "nodeId": "Workflow1:StateMachine1:Exit1",
            "name": "WriteLine2",
            "version": 1,
            "type": "Elsa.WriteLine"
          }
        }
      ],
      "transitions": [
        {
          "name": "Approve",
          "from": "Pending",
          "to": "Approved",
          "trigger": {
            "id": "Trigger1",
            "nodeId": "Workflow1:StateMachine1:Trigger1",
            "name": "Event1",
            "version": 1,
            "type": "Elsa.Event"
          },
          "condition": {
            "id": "Condition1",
            "nodeId": "Workflow1:StateMachine1:Condition1",
            "name": "JavaScript1",
            "version": 1,
            "type": "JavaScript"
          },
          "action": {
            "id": "Action1",
            "nodeId": "Workflow1:StateMachine1:Action1",
            "name": "WriteLine3",
            "version": 1,
            "type": "Elsa.WriteLine"
          }
        }
      ]
    }
    """)!.AsObject();
}
