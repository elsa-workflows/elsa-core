using System.Text.Json.Nodes;
using System.Reflection;
using Elsa.Api.Client.Extensions;
using Elsa.Api.Client.Resources.ActivityDescriptors.Models;
using Elsa.Studio.Localization;
using Elsa.Studio.Workflows.DiagramDesigners;
using Elsa.Studio.Workflows.Designer.Models;
using Elsa.Studio.Workflows.Designer.Services;
using Elsa.Studio.Workflows.DiagramDesigners.StateMachines;
using Elsa.Studio.Workflows.Domain.Contracts;
using Elsa.Studio.Workflows.Domain.Models;
using Elsa.Studio.Workflows.Shared.Components;
using Elsa.Studio.Workflows.UI.Contexts;
using Elsa.Studio.Workflows.UI.Contracts;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;
using Xunit;

namespace Elsa.Studio.Workflows.Tests;

public class StateMachineDesignerWrapperTests
{
    [Theory]
    [InlineData(false, false, "Repair the activity definition. Include a type, id, and nodeId.")]
    [InlineData(false, true, "Edit the complete activity definition. The activity id and nodeId cannot be changed here.")]
    [InlineData(true, false, "Review the complete activity definition.")]
    public void ActivityJsonHelperText_DescribesTheApplicableValidationRules(bool isReadOnly, bool hasOriginalActivity, string expected)
    {
        var wrapper = new StateMachineDesignerWrapper();
        SetComponentParameter(wrapper, nameof(StateMachineDesignerWrapper.IsReadOnly), isReadOnly);
        SetPrivateField(wrapper, "Localizer", new TestLocalizer());

        var originalActivity = hasOriginalActivity ? new JsonObject() : null;
        var helperText = (string)InvokePrivate(wrapper, "GetActivityJsonHelperText", [originalActivity])!;

        Assert.Equal(expected, helperText);
    }

    [Fact]
    public void GetUniqueStateName_WhenRenamingToExistingStateName_ReturnsAvailableVariant()
    {
        var selectedState = new StateMachineStateNode { Name = "Pending" };
        var graph = new StateMachineGraph
        {
            States =
            {
                new StateMachineStateNode { Name = "NewOrder" },
                selectedState
            }
        };

        var result = StateMachineDesignerNames.GetUniqueStateName(graph, "NewOrder", selectedState);

        Assert.Equal("NewOrder2", result);
    }

    [Fact]
    public void GetUniqueTransitionName_IgnoresUnnamedTransitions()
    {
        var graph = new StateMachineGraph
        {
            Transitions =
            {
                new StateMachineTransitionEdge(),
                new StateMachineTransitionEdge { Name = "Approve" }
            }
        };

        var result = StateMachineDesignerNames.GetUniqueTransitionName(graph, "Approve");

        Assert.Equal("Approve2", result);
    }

    [Fact]
    public void CanvasProjection_WhenTransitionIdentitiesAreDuplicated_AssignsDistinctVisualIds()
    {
        var activity = new JsonObject
        {
            ["states"] = new JsonArray
            {
                new JsonObject { ["name"] = "Pending" },
                new JsonObject { ["name"] = "Approved" }
            },
            ["transitions"] = new JsonArray
            {
                new JsonObject { ["name"] = "Review", ["from"] = "Pending", ["to"] = "Approved" },
                new JsonObject { ["name"] = "Review", ["from"] = "Pending", ["to"] = "Approved" }
            }
        };
        var validator = new StateMachineValidator();
        var session = new StateMachineEditorSession(new StateMachineMapper(validator), validator, activity);
        var transitions = session.ProjectCanvas().Transitions;

        Assert.Equal(2, transitions.Count);
        Assert.NotEqual(transitions[0].VisualId, transitions[1].VisualId);
    }

    [Fact]
    public async Task OnGraphUpdated_WhenDesignerActivityCannotBeRead_DoesNotNotifyParent()
    {
        var graphUpdated = false;
        var wrapper = new DiagramDesignerWrapper();
        var wrapperType = typeof(DiagramDesignerWrapper);
        wrapperType.GetProperty(nameof(DiagramDesignerWrapper.GraphUpdated))!
            .SetValue(wrapper, EventCallback.Factory.Create(this, () => graphUpdated = true));
        wrapperType.GetField("_diagramDesigner", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
            .SetValue(wrapper, new ThrowingDiagramDesigner());
        var method = wrapperType.GetMethod("OnGraphUpdated", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;

        await (Task)method.Invoke(wrapper, [])!;

        Assert.False(graphUpdated);
    }

    [Fact]
    public void CreateSlotActivity_AssignsStableIdentityForASequence()
    {
        var root = CreateNestedSequenceStateMachine();
        var session = CreateSession(root);
        var wrapper = new StateMachineDesignerWrapper();
        SetComponentParameter(wrapper, nameof(StateMachineDesignerWrapper.StateMachine), root);
        SetPrivateField(wrapper, "_session", session);
        SetPrivateField(wrapper, "_selectedTransitionId", session.ProjectCanvas().Transitions.Single().VisualId);
        SetPrivateField(wrapper, "IdentityGenerator", new FixedIdentityGenerator("Sequence2"));
        SetPrivateField(wrapper, "ActivityNameGenerator", new FixedActivityNameGenerator("Sequence2"));

        var descriptor = new ActivityDescriptor
        {
            Name = "Sequence",
            TypeName = "Elsa.Sequence",
            Version = 1,
            IsBrowsable = true
        };
        var sequence = (JsonObject)InvokePrivate(wrapper, "CreateSlotActivity", descriptor)!;

        Assert.Equal("Sequence2", sequence.GetId());
        Assert.Equal("Workflow1:Machine1:Sequence2", sequence.GetNodeId());
        Assert.Equal("Elsa.Sequence", sequence.GetTypeName());
        Assert.Equal(1, sequence.GetVersion());
    }

    [Fact]
    public async Task OpenTransitionSequence_UsesDoubleClickNavigationEvenWhenReadOnly()
    {
        var root = CreateNestedSequenceStateMachine();
        var session = CreateSession(root);
        var transitionId = session.ProjectCanvas().Transitions.Single().VisualId;
        var wrapper = new StateMachineDesignerWrapper();
        SetComponentParameter(wrapper, nameof(StateMachineDesignerWrapper.StateMachine), root);
        SetComponentParameter(wrapper, nameof(StateMachineDesignerWrapper.IsReadOnly), true);
        SetPrivateField(wrapper, "_session", session);
        SetPrivateField(wrapper, "_selectedTransitionId", transitionId);
        JsonObject? opened = null;
        SetComponentParameter(wrapper, nameof(StateMachineDesignerWrapper.ActivityDoubleClick),
            EventCallback.Factory.Create<JsonObject>(new object(), (JsonObject activity) => opened = activity));

        await InvokePrivateAsync(wrapper, "OpenTransitionActivityAsync", "action");

        Assert.NotNull(opened);
        Assert.Equal("Elsa.Sequence", opened!.GetTypeName());
        Assert.Equal("Sequence1", opened.GetId());
    }

    [Fact]
    public async Task OpenStateSequence_UsesTheSameNestedDesignerNavigation()
    {
        var root = CreateNestedSequenceStateMachine();
        var sequence = root["transitions"]![0]!["action"]!.DeepClone();
        root["states"]![0]!["entry"] = sequence;
        var session = CreateSession(root);
        var stateId = session.ProjectCanvas().States.Single(x => x.Name == "Pending").VisualId;
        var wrapper = new StateMachineDesignerWrapper();
        SetComponentParameter(wrapper, nameof(StateMachineDesignerWrapper.StateMachine), root);
        SetPrivateField(wrapper, "_session", session);
        SetPrivateField(wrapper, "_selectedStateId", stateId);
        JsonObject? opened = null;
        SetComponentParameter(wrapper, nameof(StateMachineDesignerWrapper.ActivityDoubleClick),
            EventCallback.Factory.Create<JsonObject>(new object(), (JsonObject activity) => opened = activity));

        await InvokePrivateAsync(wrapper, "OpenStateActivityAsync", "entry");

        Assert.NotNull(opened);
        Assert.Equal("Elsa.Sequence", opened!.GetTypeName());
        Assert.Equal("Sequence1", opened.GetId());
    }

    [Fact]
    public async Task SelectActivityAsync_SelectsNestedSequenceChildInTheOwningAction()
    {
        var root = CreateNestedSequenceStateMachine();
        var session = CreateSession(root);
        var wrapper = new StateMachineDesignerWrapper();
        SetComponentParameter(wrapper, nameof(StateMachineDesignerWrapper.StateMachine), root);
        SetPrivateField(wrapper, "_session", session);
        JsonObject? selected = null;
        SetComponentParameter(wrapper, nameof(StateMachineDesignerWrapper.ActivitySelected),
            EventCallback.Factory.Create<JsonObject>(new object(), (JsonObject activity) => selected = activity));

        await wrapper.SelectActivityAsync("Grandchild1");

        Assert.NotNull(selected);
        Assert.Equal("Grandchild1", selected!.GetId());
        Assert.Equal("WriteLine2", selected.GetName());
    }

    [Fact]
    public async Task SelectTransitionActivity_SelectsArbitraryActivityWithoutOpeningANestedDesigner()
    {
        var root = CreateNestedSequenceStateMachine();
        root["transitions"]![0]!["action"] = new JsonObject
        {
            ["id"] = "WriteLine1",
            ["nodeId"] = "Workflow1:Machine1:WriteLine1",
            ["name"] = "WriteLine1",
            ["type"] = "Elsa.WriteLine",
            ["version"] = 1,
            ["text"] = "hello"
        };
        var session = CreateSession(root);
        var wrapper = new StateMachineDesignerWrapper();
        SetComponentParameter(wrapper, nameof(StateMachineDesignerWrapper.StateMachine), root);
        SetPrivateField(wrapper, "_session", session);
        SetPrivateField(wrapper, "_selectedTransitionId", session.ProjectCanvas().Transitions.Single().VisualId);
        JsonObject? selected = null;
        JsonObject? opened = null;
        SetComponentParameter(wrapper, nameof(StateMachineDesignerWrapper.ActivitySelected),
            EventCallback.Factory.Create<JsonObject>(new object(), (JsonObject activity) => selected = activity));
        SetComponentParameter(wrapper, nameof(StateMachineDesignerWrapper.ActivityDoubleClick),
            EventCallback.Factory.Create<JsonObject>(new object(), (JsonObject activity) => opened = activity));

        await InvokePrivateAsync(wrapper, "SelectTransitionActivityAsync", "action");

        Assert.Equal("WriteLine1", selected?.GetId());
        Assert.Equal("Elsa.WriteLine", selected?.GetTypeName());
        Assert.Null(opened);
    }

    [Fact]
    public void NestedSequenceChild_IsFoundAndUpdatedThroughItsOwningTransitionSlot()
    {
        var root = CreateNestedSequenceStateMachine();
        var session = CreateSession(root);
        var transitionId = session.ProjectCanvas().Transitions.Single().VisualId;
        var wrapper = new StateMachineDesignerWrapper();
        SetComponentParameter(wrapper, nameof(StateMachineDesignerWrapper.StateMachine), root);
        SetPrivateField(wrapper, "_session", session);
        SetPrivateField(wrapper, "_selectedTransitionId", transitionId);

        var arguments = new object?[] { "Grandchild1", null, null, null, null };
        var found = (bool)InvokePrivate(wrapper, "TryFindSlotActivity", arguments)!;

        Assert.True(found);
        Assert.Equal("action", arguments[4]);
        Assert.Equal("Grandchild1", ((JsonObject)arguments[1]!).GetId());
        Assert.NotNull(arguments[3]);

        var replacement = new JsonObject
        {
            ["id"] = "Grandchild1",
            ["nodeId"] = "Workflow1:Machine1:Sequence1:NestedSequence1:Grandchild1",
            ["name"] = "UpdatedGrandchild",
            ["type"] = "Elsa.WriteLine",
            ["version"] = 1,
            ["text"] = "updated"
        };
        var updated = (bool)InvokePrivate(wrapper, "TryUpdateSlotActivity", "Grandchild1", replacement)!;

        Assert.True(updated);
        var transition = session.Graph.Transitions.Single();
        var action = (JsonObject)transition.Action!;
        Assert.Equal("Sequence1", action.GetId());
        Assert.Equal("NestedSequence1", action["activities"]![1]!["id"]!.GetValue<string>());
        Assert.Equal("UpdatedGrandchild", action["activities"]![1]!["activities"]![0]!["name"]!.GetValue<string>());
        Assert.Equal("Elsa.Event", ((JsonObject)transition.Trigger!).GetTypeName());

        var reloaded = CreateSession(session.Export());
        var reloadedAction = (JsonObject)reloaded.Graph.Transitions.Single().Action!;
        Assert.Equal("Sequence1", reloadedAction.GetId());
        Assert.Equal("Workflow1:Machine1:Sequence1", reloadedAction.GetNodeId());
        Assert.Equal("NestedSequence1", reloadedAction["activities"]![1]!["id"]!.GetValue<string>());
        Assert.Equal("Workflow1:Machine1:Sequence1:NestedSequence1", reloadedAction["activities"]![1]!["nodeId"]!.GetValue<string>());
    }

    [Fact]
    public async Task ReadOnlyUpdateActivity_RejectsNestedSequenceMutation()
    {
        var root = CreateNestedSequenceStateMachine();
        var session = CreateSession(root);
        var wrapper = new StateMachineDesignerWrapper();
        SetComponentParameter(wrapper, nameof(StateMachineDesignerWrapper.StateMachine), root);
        SetComponentParameter(wrapper, nameof(StateMachineDesignerWrapper.IsReadOnly), true);
        SetPrivateField(wrapper, "_session", session);

        var replacement = new JsonObject
        {
            ["id"] = "Grandchild1",
            ["nodeId"] = "Workflow1:Machine1:Sequence1:NestedSequence1:Grandchild1",
            ["name"] = "ShouldNotApply",
            ["type"] = "Elsa.WriteLine",
            ["version"] = 1
        };

        await Assert.ThrowsAsync<InvalidOperationException>(() => wrapper.UpdateActivityAsync("Grandchild1", replacement));
        Assert.Equal("Grandchild1", session.Graph.Transitions.Single().Action!["activities"]![1]!["activities"]![0]!["id"]!.GetValue<string>());
    }

    private static StateMachineEditorSession CreateSession(JsonObject activity)
    {
        var validator = new StateMachineValidator();
        return new(new StateMachineMapper(validator), validator, activity);
    }

    private static JsonObject CreateNestedSequenceStateMachine() => JsonNode.Parse("""
    {
      "id": "Machine1",
      "nodeId": "Workflow1:Machine1",
      "type": "Elsa.StateMachine",
      "initialState": "Pending",
      "currentState": "Pending",
      "states": [
        { "name": "Pending" },
        { "name": "Approved" }
      ],
      "transitions": [
        {
          "name": "Approve",
          "from": "Pending",
          "to": "Approved",
          "trigger": { "id": "Trigger1", "nodeId": "Workflow1:Trigger1", "name": "Event1", "type": "Elsa.Event", "version": 1 },
          "action": {
            "id": "Sequence1",
            "nodeId": "Workflow1:Machine1:Sequence1",
            "name": "Sequence1",
            "type": "Elsa.Sequence",
            "version": 1,
            "activities": [
              { "id": "Child1", "nodeId": "Workflow1:Machine1:Sequence1:Child1", "name": "WriteLine1", "type": "Elsa.WriteLine", "version": 1 },
              {
                "id": "NestedSequence1",
                "nodeId": "Workflow1:Machine1:Sequence1:NestedSequence1",
                "name": "NestedSequence1",
                "type": "Elsa.Sequence",
                "version": 1,
                "activities": [
                  { "id": "Grandchild1", "nodeId": "Workflow1:Machine1:Sequence1:NestedSequence1:Grandchild1", "name": "WriteLine2", "type": "Elsa.WriteLine", "version": 1 }
                ]
              }
            ]
          }
        }
      ]
    }
    """)!.AsObject();

    private static object? InvokePrivate(object target, string methodName, params object?[] arguments) =>
        target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(target, arguments);

    private static async Task InvokePrivateAsync(object target, string methodName, params object?[] arguments) =>
        await (Task)InvokePrivate(target, methodName, arguments)!;

    private static void SetPrivateField(object target, string fieldName, object value)
    {
        var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)
                    ?? target.GetType().GetField($"<{fieldName}>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);
        if (field != null)
        {
            field.SetValue(target, value);
            return;
        }

        target.GetType().GetProperty(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(target, value);
    }

    private static void SetComponentParameter<T>(StateMachineDesignerWrapper wrapper, string propertyName, T value) =>
        typeof(StateMachineDesignerWrapper).GetProperty(propertyName)!.SetValue(wrapper, value);

    private sealed class FixedIdentityGenerator(string id) : IIdentityGenerator
    {
        public string GenerateId() => id;
    }

    private sealed class FixedActivityNameGenerator(string name) : IActivityNameGenerator
    {
        public bool GetNameExists(IEnumerable<JsonObject> activities, string name) => activities.Any(x => x.GetName() == name);
        public string GenerateNextName(IEnumerable<JsonObject> activities, ActivityDescriptor activityDescriptor) => name;
    }

    private sealed class TestLocalizer : ILocalizer
    {
        public LocalizedString this[string? key] => new(key ?? string.Empty, key ?? string.Empty);

        public LocalizedString this[string? key, params object[] arguments] =>
            new(key ?? string.Empty, string.Format(key ?? string.Empty, arguments));
    }

    private class ThrowingDiagramDesigner : IDiagramDesigner
    {
        public Task LoadRootActivityAsync(JsonObject activity, IDictionary<string, ActivityStats>? activityStatsMap) => Task.CompletedTask;

        public Task UpdateActivityAsync(string id, JsonObject activity) => Task.CompletedTask;

        public Task UpdateActivityStatsAsync(string id, ActivityStats stats) => Task.CompletedTask;

        public Task SelectActivityAsync(string id) => Task.CompletedTask;

        public Task<JsonObject> ReadRootActivityAsync() =>
            throw new DiagramDesignerValidationException("Cannot read the current designer activity.");

        public RenderFragment DisplayDesigner(DisplayContext context) => _ => { };
    }
}
