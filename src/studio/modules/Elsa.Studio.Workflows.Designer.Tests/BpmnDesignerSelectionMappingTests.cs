using System.Text.Json.Nodes;
using Elsa.Studio.Workflows.Designer.Components;
using Elsa.Studio.Workflows.Extensions;
using Xunit;

namespace Elsa.Studio.Workflows.Designer.Tests;

/// <summary>
/// Covers <see cref="BpmnDesigner"/>'s mapping from a BPMN element selection back to the Elsa
/// activity JSON the properties panel should show, and from an activity id to the element bound to
/// it. Both walk the root <c>Elsa.BpmnProcess</c> activity's tree, recursing into nested BPMN scopes,
/// so the fixtures below nest a scope inside the root the same way an embedded subprocess would.
/// </summary>
public class BpmnDesignerSelectionMappingTests
{
    [Fact]
    public void FindActivityById_ReturnsTheBoundChildActivity_WhenTheIdNamesOneAtTheRoot()
    {
        var boundActivity = CreateActivity("write-line-1", "Elsa.WriteLine");
        var root = CreateScope("root", ("ref-1", "write-line-1"), boundActivity);

        var found = root.FindActivity("write-line-1");

        Assert.Same(boundActivity, found);
    }

    [Fact]
    public void FindActivityById_ReturnsTheBoundChildActivity_WhenItIsNestedInAChildScope()
    {
        var boundActivity = CreateActivity("write-line-1", "Elsa.WriteLine");
        var nestedScope = CreateScope("nested-scope", ("ref-1", "write-line-1"), boundActivity);
        var root = CreateScope("root", ("ref-nested", "nested-scope"), nestedScope);

        var found = root.FindActivity("write-line-1");

        Assert.Same(boundActivity, found);
    }

    [Fact]
    public void FindActivityById_ReturnsTheRootScopeActivity_WhenTheIdNamesTheRootItself()
    {
        var root = CreateScope("root");

        var found = root.FindActivity("root");

        Assert.Same(root, found);
    }

    [Fact]
    public void FindActivityById_ReturnsTheNestedScopeActivity_WhenTheIdNamesTheNestedScopeItself()
    {
        var nestedScope = CreateScope("nested-scope");
        var root = CreateScope("root", ("ref-nested", "nested-scope"), nestedScope);

        var found = root.FindActivity("nested-scope");

        Assert.Same(nestedScope, found);
    }

    [Fact]
    public void FindActivityById_ReturnsNull_WhenNothingInTheTreeCarriesTheId()
    {
        var root = CreateScope("root");

        var found = root.FindActivity("does-not-exist");

        Assert.Null(found);
    }

    [Fact]
    public void ResolveElementId_ReturnsTheBoundElementId_ThroughWorkBindingsAndTheProcessElements()
    {
        var boundActivity = CreateActivity("order-process:node-NotifyWarehouse", "Elsa.WriteLine");
        var root = CreateScope(
            "order-process",
            ("node-NotifyWarehouse", "order-process:node-NotifyWarehouse"),
            boundActivity,
            ("NotifyWarehouse", "serviceTask", "node-NotifyWarehouse"),
            ("StartEvent_1", "startEvent", null),
            ("EndEvent_1", "endEvent", null));

        var elementId = BpmnDesigner.ResolveElementId(root, "order-process:node-NotifyWarehouse");

        Assert.Equal("NotifyWarehouse", elementId);
    }

    [Fact]
    public void ResolveElementId_ReturnsNull_ForTheRootScopeActivityId()
    {
        var root = CreateScope("order-process", workBindings: [("node-NotifyWarehouse", "order-process:node-NotifyWarehouse")]);

        var elementId = BpmnDesigner.ResolveElementId(root, "order-process");

        Assert.Null(elementId);
    }

    [Fact]
    public void ResolveElementId_ReturnsNull_ForAnActivityIdNoBindingNames()
    {
        var root = CreateScope("order-process", workBindings: [("node-NotifyWarehouse", "order-process:node-NotifyWarehouse")]);

        var elementId = BpmnDesigner.ResolveElementId(root, "does-not-exist");

        Assert.Null(elementId);
    }

    private static JsonObject CreateActivity(string id, string type) => new()
    {
        ["id"] = id,
        ["type"] = type
    };

    /// <summary>
    /// Builds an <c>Elsa.BpmnProcess</c> scope activity carrying <paramref name="workBindings"/>,
    /// <paramref name="processElements"/> (each a BPMN element declaring an id, type and, optionally,
    /// a binding ref) and the given bound child activities.
    /// </summary>
    private static JsonObject CreateScope(
        string id,
        (string Ref, string ActivityId)[]? workBindings = null,
        JsonObject[]? boundActivities = null,
        (string ElementId, string ElementType, string? BindingRef)[]? processElements = null)
    {
        var workBindingsObject = new JsonObject();

        foreach (var (bindingRef, activityId) in workBindings ?? [])
            workBindingsObject[bindingRef] = activityId;

        var elements = new JsonArray();

        foreach (var (elementId, elementType, bindingRef) in processElements ?? [])
        {
            elements.Add(new JsonObject
            {
                ["elementId"] = elementId,
                ["elementType"] = elementType,
                ["bindingRef"] = bindingRef
            });
        }

        var activities = new JsonArray();

        foreach (var activity in boundActivities ?? [])
            activities.Add(activity);

        return new JsonObject
        {
            ["id"] = id,
            ["type"] = "Elsa.BpmnProcess",
            ["workBindings"] = workBindingsObject,
            ["process"] = new JsonObject { ["elements"] = elements },
            ["activities"] = activities
        };
    }

    // Overload used by the tests that only care about a single work binding and its bound activity.
    private static JsonObject CreateScope(string id, (string Ref, string ActivityId) workBinding, JsonObject boundActivity) =>
        CreateScope(id, [workBinding], [boundActivity]);

    private static JsonObject CreateScope(
        string id,
        (string Ref, string ActivityId) workBinding,
        JsonObject boundActivity,
        params (string ElementId, string ElementType, string? BindingRef)[] processElements) =>
        CreateScope(id, [workBinding], [boundActivity], processElements);
}
