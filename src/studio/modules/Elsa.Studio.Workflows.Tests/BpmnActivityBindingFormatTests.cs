using System.Text.Json.Nodes;
using Elsa.Studio.Workflows.Domain.Models.Bpmn;
using Elsa.Studio.Workflows.Tests.Support;
using Xunit;

namespace Elsa.Studio.Workflows.Tests;

/// <summary>
/// Covers Studio's side of the <c>elsa:activityBinding</c> compatibility surface on the library-format document the
/// document endpoints exchange (D4): a binding edit touches the one extension entry that binds the task and nothing else
/// in the document — not the task's <c>camunda:*</c> extensions, its foreign attributes, its documentation, the other
/// elements, or the DI — and what Studio writes is the exact shape <c>Bpmn.Interchange</c>'s own reader produced for the
/// same binding.
/// </summary>
public class BpmnActivityBindingFormatTests
{
    private readonly JsonObject _original = BpmnDocumentFixtures.Document();
    private readonly JsonObject _document = BpmnDocumentFixtures.Document();

    private JsonObject Task => BpmnDocumentFixtures.TaskElement(_document);
    private JsonArray TaskExtensionElements => (JsonArray)Task["extensions"]!["extensionElements"]!;

    [Fact]
    public void Create_WritesExactlyTheShapeTheLibraryReaderProducesForTheSameBinding()
    {
        var authored = BpmnActivityBindingFormat.Find(Task)!;
        var text = JsonNode.Parse("""{"typeName":"String","expression":{"type":"Literal","value":"Notifying the warehouse"}}""");

        var written = BpmnActivityBindingFormat.Create("Elsa.WriteLine", [KeyValuePair.Create("text", text)]);

        Assert.True(JsonNode.DeepEquals(authored, written), written.ToJsonString());
    }

    [Fact]
    public void Attach_ReplacesTheTasksBindingInPlace_AndChangesNothingElseInTheDocument()
    {
        var bindingIndex = TaskExtensionElements.IndexOf(BpmnActivityBindingFormat.Find(Task));
        var replacement = BpmnActivityBindingFormat.Create("Elsa.HttpRequest", [KeyValuePair.Create<string, JsonNode?>("url", JsonNode.Parse("""{"typeName":"Uri","expression":{"type":"JavaScript","value":"getUrl()"}}"""))]);

        BpmnActivityBindingFormat.Attach(Task, replacement);

        Assert.Same(replacement, TaskExtensionElements[bindingIndex]);
        Assert.False(JsonNode.DeepEquals(_original, _document));

        // Putting the original entry back must restore the document exactly: the edit touched that one entry only.
        TaskExtensionElements[bindingIndex] = BpmnActivityBindingFormat.Find(BpmnDocumentFixtures.TaskElement(_original))!.DeepClone();
        Assert.True(JsonNode.DeepEquals(_original, _document));
    }

    [Fact]
    public void Attach_AppendsABindingToAnElementThatHasNone_AndChangesNothingElse()
    {
        var startEvent = (JsonObject)_document["processes"]![0]!["elements"]![0]!;
        var binding = BpmnActivityBindingFormat.Create("Elsa.WriteLine", []);

        BpmnActivityBindingFormat.Attach(startEvent, binding);

        var extensionElements = (JsonArray)startEvent["extensions"]!["extensionElements"]!;
        Assert.Same(binding, Assert.Single(extensionElements));

        extensionElements.Clear();
        Assert.True(JsonNode.DeepEquals(_original, _document));
    }

    [Fact]
    public void Attach_LeavesExactlyOneBinding_WhenTheElementCarriedTwo()
    {
        // A reader takes the first of two; leaving the older one behind would silently keep applying it.
        TaskExtensionElements.Add(BpmnActivityBindingFormat.Create("Elsa.Stale", []));
        var replacement = BpmnActivityBindingFormat.Create("Elsa.HttpRequest", []);

        BpmnActivityBindingFormat.Attach(Task, replacement);

        Assert.Same(replacement, BpmnActivityBindingFormat.Find(Task));
        Assert.Equal(2, TaskExtensionElements.Count);
        Assert.Equal("inputOutput", TaskExtensionElements[0]!["name"]!["localName"]!.GetValue<string>());
    }

    [Fact]
    public void Attach_CreatesTheExtensionsAnElementWithoutAnyNeeds()
    {
        var element = new JsonObject { ["elementId"] = "Added", ["elementType"] = "serviceTask" };

        BpmnActivityBindingFormat.Attach(element, BpmnActivityBindingFormat.Create("Elsa.WriteLine", []));

        Assert.NotNull(BpmnActivityBindingFormat.Find(element));
        Assert.IsType<JsonArray>(element["extensions"]!["foreignAttributes"]);
    }

    [Fact]
    public void Create_SkipsInputsWithNoValue_AndOrdersTheRestByName()
    {
        var binding = BpmnActivityBindingFormat.Create("Elsa.HttpRequest",
        [
            KeyValuePair.Create<string, JsonNode?>("url", JsonValue.Create("u")),
            KeyValuePair.Create<string, JsonNode?>("method", null),
            KeyValuePair.Create<string, JsonNode?>("body", JsonNode.Parse("null")),
            KeyValuePair.Create<string, JsonNode?>("contentType", JsonValue.Create("c"))
        ]);

        Assert.Equal(["contentType", "url"], BpmnActivityBindingFormat.Read(binding).Inputs.Select(input => input.Name));
    }

    [Fact]
    public void Read_ReadsTheAuthoredBinding()
    {
        var binding = BpmnActivityBindingFormat.Read(BpmnActivityBindingFormat.Find(Task)!);

        Assert.Equal("Elsa.WriteLine", binding.ActivityType);
        var input = Assert.Single(binding.Inputs);
        Assert.Equal("text", input.Name);
        Assert.Equal("Notifying the warehouse", input.Value!["expression"]!["value"]!.GetValue<string>());
    }

    [Theory]
    [InlineData("missing-activity-type")]
    [InlineData("duplicate-input")]
    [InlineData("unnamed-input")]
    [InlineData("invalid-json")]
    public void Read_RefusesWhatElsaCoreRefuses(string defect)
    {
        var binding = BpmnActivityBindingFormat.Create("Elsa.WriteLine", [KeyValuePair.Create<string, JsonNode?>("text", JsonValue.Create("x"))]);
        var inputs = (JsonArray)binding["children"]!;

        switch (defect)
        {
            case "missing-activity-type":
                ((JsonArray)binding["attributes"]!).Clear();
                break;
            case "duplicate-input":
                inputs.Add(inputs[0]!.DeepClone());
                break;
            case "unnamed-input":
                ((JsonArray)inputs[0]!["attributes"]!).Clear();
                break;
            case "invalid-json":
                inputs[0]!["value"] = "{ not json";
                break;
        }

        Assert.Throws<BpmnActivityBindingFormatException>(() => BpmnActivityBindingFormat.Read(binding));
    }

    [Fact]
    public void Find_IgnoresAnExtensionWithTheBindingsLocalNameInAnotherNamespace()
    {
        var foreign = BpmnActivityBindingFormat.Create("Elsa.WriteLine", []);
        foreign["name"]!["ns"] = "http://camunda.org/schema/1.0/bpmn";
        var element = new JsonObject { ["extensions"] = new JsonObject { ["extensionElements"] = new JsonArray(foreign) } };

        Assert.Null(BpmnActivityBindingFormat.Find(element));
    }

    [Fact]
    public void FindElement_FindsATaskOfAnyProcess_AndNothingForAnUnknownId()
    {
        Assert.Same(Task, BpmnDefinitionsDocument.FindElement(_document, BpmnDocumentFixtures.TaskId));
        Assert.Null(BpmnDefinitionsDocument.FindElement(_document, "Nope"));
    }
}
