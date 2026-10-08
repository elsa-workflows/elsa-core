using System.Text.Json.Nodes;
using Elsa.Studio.Workflows.Designer.Services;
using Xunit;

namespace Elsa.Studio.Workflows.Designer.Tests;

public class StateMachineMapperTests
{
    private readonly StateMachineMapper _mapper = new(new StateMachineValidator());

    [Fact]
    public void Map_LoadsStatesTransitionsAndTerminalMarkers()
    {
        var graph = _mapper.Map(CreateActivity());

        Assert.Equal("NewOrder", graph.InitialState);
        Assert.Equal("NewOrder", graph.CurrentState);
        Assert.Equal(["NewOrder", "Paid", "Closed"], graph.States.Select(x => x.Name));

        var transition = Assert.Single(graph.Transitions);
        Assert.Equal("MarkPaid", transition.Name);
        Assert.Equal("Mark paid", transition.DisplayName);
        Assert.Equal("NewOrder", transition.From);
        Assert.Equal("Paid", transition.To);
        Assert.NotNull(transition.Trigger);
        Assert.NotNull(transition.Condition);
        Assert.NotNull(transition.Action);

        Assert.False(graph.States.Single(x => x.Name == "NewOrder").IsTerminal);
        Assert.True(graph.States.Single(x => x.Name == "Paid").IsTerminal);
        Assert.True(graph.States.Single(x => x.Name == "Closed").IsTerminal);
    }

    [Fact]
    public void Map_RoundTripsStateMachineJsonAndPreservesUnknownProperties()
    {
        var graph = _mapper.Map(CreateActivity());
        graph.States.Single(x => x.Name == "Paid").Name = "PaymentReceived";
        graph.Transitions.Single().To = "PaymentReceived";

        var activity = _mapper.Map(graph);

        Assert.Equal("keep-me", activity["customActivityProperty"]!.GetValue<string>());
        Assert.Equal("custom-state", activity["states"]![0]!["customStateProperty"]!.GetValue<string>());
        Assert.Equal("custom-transition", activity["transitions"]![0]!["customTransitionProperty"]!.GetValue<string>());
        Assert.Equal("PaymentReceived", activity["states"]![1]!["name"]!.GetValue<string>());
        Assert.Equal("PaymentReceived", activity["transitions"]![0]!["to"]!.GetValue<string>());
        Assert.Equal("Elsa.Event", activity["transitions"]![0]!["trigger"]!["type"]!.GetValue<string>());
        Assert.True(activity["transitions"]![0]!["condition"]!.GetValue<bool>());
        Assert.Equal("Elsa.WriteLine", activity["transitions"]![0]!["action"]!["type"]!.GetValue<string>());
    }

    [Fact]
    public void Map_PreservesMissingConditionAsMissing()
    {
        var source = CreateActivity();
        source["transitions"]![0]!.AsObject().Remove("condition");

        var graph = _mapper.Map(source);
        var activity = _mapper.Map(graph);

        Assert.Null(activity["transitions"]![0]!["condition"]);
    }

    [Theory]
    [InlineData("{\"type\":\"Contoso.Condition\",\"payload\":{\"opaque\":true}}")]
    [InlineData("{\"$invalidJson\":\"Elsa.Studio.Workflows.StateMachineDesigner.InvalidJsonSlot\",\"source\":\"{ broken\"}")]
    public void Map_PreservesUnknownOrMalformedConditionOnNoOpRoundTrip(string conditionJson)
    {
        var source = CreateActivity();
        source["transitions"]![0]!["condition"] = JsonNode.Parse(conditionJson);

        var graph = _mapper.Map(source);
        var activity = _mapper.Map(graph);

        Assert.True(JsonNode.DeepEquals(source["transitions"]![0]!["condition"], activity["transitions"]![0]!["condition"]));
    }

    [Fact]
    public void Map_RemovesBlankOptionalStateProperties()
    {
        var graph = _mapper.Map(CreateActivity());
        graph.InitialState = "";
        graph.CurrentState = " ";

        var activity = _mapper.Map(graph);

        Assert.Null(activity["initialState"]);
        Assert.Null(activity["currentState"]);
    }

    [Fact]
    public void Map_CoercesNonStringScalarNamesToStrings()
    {
        var source = CreateActivity();
        source["initialState"] = 123;
        source["states"]![0]!["name"] = 456;

        var graph = _mapper.Map(source);

        Assert.Equal("123", graph.InitialState);
        Assert.Equal("456", graph.States.First().Name);
    }

    [Fact]
    public void Map_ReportsNonObjectStateAndTransitionItemsAsValidationErrors()
    {
        var source = CreateActivity();
        source["states"]!.AsArray().Add("unexpected-state");
        source["transitions"]!.AsArray().Add(true);

        var graph = _mapper.Map(source);

        Assert.Contains(graph.ValidationIssues, x => x.Code == "InvalidStateItem" && x.Target == "states[3]");
        Assert.Contains(graph.ValidationIssues, x => x.Code == "InvalidTransitionItem" && x.Target == "transitions[1]");
    }

    [Fact]
    public void Map_PreservesStructurallyInvalidArrayEntriesWhenMappingBack()
    {
        var source = CreateActivity();
        source["states"]!.AsArray().Add("unexpected-state");
        source["transitions"]!.AsArray().Add(true);

        var graph = _mapper.Map(source);
        var activity = _mapper.Map(graph);

        Assert.True(JsonNode.DeepEquals(source["states"], activity["states"]));
        Assert.True(JsonNode.DeepEquals(source["transitions"], activity["transitions"]));
    }

    [Fact]
    public void Map_ReportsNonArrayStateMachineCollectionsAsBlockingErrors()
    {
        var source = CreateActivity();
        source["states"] = "not-an-array";
        source["transitions"] = new JsonObject();

        var graph = _mapper.Map(source);

        Assert.Contains(graph.ValidationIssues, x => x.Code == "InvalidStateCollection" && x.Target == "states");
        Assert.Contains(graph.ValidationIssues, x => x.Code == "InvalidTransitionCollection" && x.Target == "transitions");
    }

    [Fact]
    public void Map_PreservesStructurallyInvalidCollectionsWhenMappingBack()
    {
        var source = CreateActivity();
        source["states"] = "not-an-array";
        source["transitions"] = new JsonObject { ["unexpected"] = true };

        var graph = _mapper.Map(source);
        var activity = _mapper.Map(graph);

        Assert.True(JsonNode.DeepEquals(source["states"], activity["states"]));
        Assert.True(JsonNode.DeepEquals(source["transitions"], activity["transitions"]));
    }

    [Fact]
    public void Map_MarksStateTerminalWhenOnlyOutboundTransitionsTargetUnknownStates()
    {
        var source = CreateActivity();
        source["transitions"] = JsonNode.Parse("""
        [
          {
            "name": "ArchiveExternally",
            "from": "Paid",
            "to": "Archived"
          }
        ]
        """);

        var graph = _mapper.Map(source);

        Assert.True(graph.States.Single(x => x.Name == "Paid").IsTerminal);
    }

    [Fact]
    public void Map_RoundTripsAddedStatesAndTransition()
    {
        var graph = _mapper.Map(CreateActivity());
        graph.States.Add(new()
        {
            Name = "Archived",
            Source = new JsonObject { ["name"] = "Archived" }
        });
        graph.Transitions.Add(new()
        {
            Name = "Archive",
            DisplayName = "Archive",
            From = "Paid",
            To = "Archived",
            Source = new JsonObject
            {
                ["name"] = "Archive",
                ["displayName"] = "Archive",
                ["from"] = "Paid",
                ["to"] = "Archived"
            }
        });
        graph.InitialState = "NewOrder";

        var activity = _mapper.Map(graph);

        Assert.Contains(activity["states"]!.AsArray(), x => x!["name"]!.GetValue<string>() == "Archived");
        Assert.Contains(activity["transitions"]!.AsArray(), x =>
            x!["name"]!.GetValue<string>() == "Archive" &&
            x["from"]!.GetValue<string>() == "Paid" &&
            x["to"]!.GetValue<string>() == "Archived");
        Assert.Equal("NewOrder", activity["initialState"]!.GetValue<string>());
    }

    [Fact]
    public void Map_RoundTripsTransitionTriggerConditionAndActionSlots()
    {
        var graph = _mapper.Map(CreateActivity());
        var transition = graph.Transitions.Single();
        transition.Trigger = JsonNode.Parse("""{ "type": "Elsa.Timer", "period": "PT1M" }""");
        transition.Condition = JsonNode.Parse("""{ "type": "JavaScript", "expression": "order.Total > 0" }""");
        transition.Action = JsonNode.Parse("""{ "type": "Elsa.SendEmail", "subject": "Paid" }""");

        var activity = _mapper.Map(graph);

        Assert.Equal("Elsa.Timer", activity["transitions"]![0]!["trigger"]!["type"]!.GetValue<string>());
        Assert.Equal("JavaScript", activity["transitions"]![0]!["condition"]!["type"]!.GetValue<string>());
        Assert.Equal("Elsa.SendEmail", activity["transitions"]![0]!["action"]!["type"]!.GetValue<string>());
    }

    [Fact]
    public void Map_RoundTripsStateEntryAndExitSlots()
    {
        var graph = _mapper.Map(CreateActivity());
        var state = graph.States.Single(x => x.Name == "Closed");
        state.Entry = JsonNode.Parse("""{ "type": "Elsa.WriteLine", "text": "Entering closed" }""");
        state.Exit = JsonNode.Parse("""{ "type": "Elsa.WriteLine", "text": "Leaving closed" }""");

        var activity = _mapper.Map(graph);
        var closed = activity["states"]!.AsArray().Single(x => x!["name"]!.GetValue<string>() == "Closed")!;

        Assert.Equal("Entering closed", closed["entry"]!["text"]!.GetValue<string>());
        Assert.Equal("Leaving closed", closed["exit"]!["text"]!.GetValue<string>());
    }

    private static JsonObject CreateActivity() => JsonNode.Parse("""
    {
      "type": "Elsa.StateMachine",
      "initialState": "NewOrder",
      "currentState": "NewOrder",
      "customActivityProperty": "keep-me",
      "states": [
        {
          "name": "NewOrder",
          "customStateProperty": "custom-state",
          "entry": { "type": "Elsa.WriteLine", "text": "New order" },
          "exit": { "type": "Elsa.WriteLine", "text": "Leaving new order" }
        },
        {
          "name": "Paid",
          "entry": { "type": "Elsa.WriteLine", "text": "Paid" }
        },
        {
          "name": "Closed"
        }
      ],
      "transitions": [
        {
          "name": "MarkPaid",
          "displayName": "Mark paid",
          "from": "NewOrder",
          "to": "Paid",
          "customTransitionProperty": "custom-transition",
          "trigger": { "type": "Elsa.Event" },
          "condition": true,
          "action": { "type": "Elsa.WriteLine", "text": "Payment accepted" }
        }
      ]
    }
    """)!.AsObject();
}
