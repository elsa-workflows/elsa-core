using System.Text.Json;
using System.Text.Json.Nodes;
using Elsa.Api.Client.Extensions;
using Elsa.Api.Client.Resources.Scripting.Models;
using Elsa.Api.Client.Shared.Models;
using Elsa.Studio.Workflows.Domain.Models.Bpmn;
using Elsa.Studio.Workflows.Tests.Support;
using Xunit;
using static Elsa.Studio.Workflows.Tests.Support.BpmnDocumentFixtures;

namespace Elsa.Studio.Workflows.Tests;

/// <summary>
/// The input round trip (D): a binding carries each input as the JSON Studio's own input editors wrote into the
/// activity — no second encoder — and reading the binding back the way elsa-core does reconstructs every input value.
/// Covered for both shapes the format has to carry: an <c>Input&lt;T&gt;</c>'s <c>{"typeName":…,"expression":…}</c>
/// wrapper (here a JavaScript expression) and a plain <c>[Input]</c>'s own JSON (here <c>FlowSwitch.Cases</c>, an array).
/// </summary>
public class BpmnActivityBindingDraftTests
{
    /// <summary>What <c>InputsTab.HandleValueChangedAsync</c> serializes an editor's value with.</summary>
    private static readonly JsonSerializerOptions EditorOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    private readonly Elsa.Api.Client.Resources.ActivityDescriptors.Models.ActivityDescriptor _flowSwitch = Descriptor("Elsa.FlowSwitch", "Switch",
        Input("Cases", "System.Collections.Generic.ICollection<Elsa.Workflows.Activities.Flowchart.Models.FlowSwitchCase>", isWrapped: false),
        Input("Mode", "Elsa.Workflows.Activities.Flowchart.Models.SwitchMode", isWrapped: true),
        Input("Label", "String", isWrapped: true));

    [Fact]
    public void ToBinding_CarriesAWrappedJavaScriptInputAndAPlainArrayInput_AsElsaCoreReadsThemBack()
    {
        var draft = BpmnActivityBindingDraft.Create(_flowSwitch, BoundActivityId);
        EditAsInputsTabDoes(draft, "label", new WrappedInput { TypeName = "String", Expression = new Expression("JavaScript", "`Route ${getVariable('orderId')}` && x < y") });
        EditAsInputsTabDoes(draft, "cases", new List<SwitchCase>
        {
            new() { Label = "Big order", Condition = new Expression("JavaScript", "getVariable('total') > 100") },
            new() { Label = "Small order", Condition = new Expression("Literal", "true") }
        });

        var activityAsCoreBuildsIt = ReadAsElsaCore(draft.ToBinding());

        Assert.Equal("Elsa.FlowSwitch", activityAsCoreBuildsIt["type"]!.GetValue<string>());
        Assert.True(JsonNode.DeepEquals(draft.Activity["label"], activityAsCoreBuildsIt["label"]), activityAsCoreBuildsIt.ToJsonString());
        Assert.True(JsonNode.DeepEquals(draft.Activity["cases"], activityAsCoreBuildsIt["cases"]), activityAsCoreBuildsIt.ToJsonString());
        Assert.Equal(JsonValueKind.Array, activityAsCoreBuildsIt["cases"]!.GetValueKind());
        Assert.Equal("JavaScript", activityAsCoreBuildsIt["label"]!["expression"]!["type"]!.GetValue<string>());

        // An input nobody configured is left out, exactly as elsa-core's own writer leaves a null input out.
        Assert.False(activityAsCoreBuildsIt.ContainsKey("mode"));
    }

    [Fact]
    public void FromBinding_ThenToBinding_ReproducesTheAuthoredBindingExactly()
    {
        var authored = BpmnActivityBindingFormat.Find(BpmnDocumentFixtures.TaskElement(Document()))!;

        var draft = BpmnActivityBindingDraft.FromBinding(BpmnActivityBindingFormat.Read(authored), WriteLine(), BoundActivityId);

        Assert.Equal("Notifying the warehouse", draft.Activity["text"]!["expression"]!["value"]!.GetValue<string>());
        Assert.True(JsonNode.DeepEquals(authored, draft.ToBinding()), draft.ToBinding().ToJsonString());
    }

    [Fact]
    public void AnAcronymInput_IsNamedAsElsaCoreNamesIt_InTheBinding_AndAsTheEditorsKeyIt_InTheActivity()
    {
        // JsonNamingPolicy.CamelCase makes "URL" "url"; Humanizer's Camelize, which the input editors key on, makes it "uRL".
        var descriptor = Descriptor("Acme.Fetch", "Fetch", Input("URL", "Uri", isWrapped: true));
        var url = JsonNode.Parse("""{"typeName":"Uri","expression":{"type":"Literal","value":"https://example.com"}}""");
        var binding = new BpmnActivityBinding("Acme.Fetch", [new("url", url)]);

        var draft = BpmnActivityBindingDraft.FromBinding(binding, descriptor, "task");

        Assert.True(JsonNode.DeepEquals(url, draft.Activity["uRL"]));
        Assert.True(JsonNode.DeepEquals(url, ReadAsElsaCore(draft.ToBinding())["url"]));
    }

    [Fact]
    public void AnInputStudiosCatalogueDoesNotDescribe_IsWrittenBackAsItWasRead()
    {
        // Studio's catalogue can be older than the server's; saving must not quietly drop what it cannot display.
        var unknown = JsonNode.Parse("""{"typeName":"Int32","expression":{"type":"Literal","value":"3"}}""");
        var binding = new BpmnActivityBinding("Elsa.WriteLine", [new("retries", unknown), new("text", JsonValue.Create("hello"))]);

        var draft = BpmnActivityBindingDraft.FromBinding(binding, WriteLine(), "task");

        Assert.False(draft.Activity.ContainsKey("retries"));
        var written = ReadAsElsaCore(draft.ToBinding());
        Assert.True(JsonNode.DeepEquals(unknown, written["retries"]));
        Assert.Equal("hello", written["text"]!.GetValue<string>());
    }

    [Fact]
    public void Create_StartsFromTheDescriptorsConstructionProperties_AndBindsOnlyDeclaredInputs()
    {
        var descriptor = WriteLine() with
        {
            ConstructionProperties = new Dictionary<string, object> { ["Text"] = new { typeName = "String", expression = new { type = "Literal", value = "default" } }, ["Unrelated"] = 1 }
        };

        var draft = BpmnActivityBindingDraft.Create(descriptor, "task");

        var written = ReadAsElsaCore(draft.ToBinding());
        Assert.Equal("default", written["text"]!["expression"]!["value"]!.GetValue<string>());
        Assert.False(written.ContainsKey("unrelated"));
        Assert.Equal(["type", "text"], written.Select(property => property.Key));
    }

    private static void EditAsInputsTabDoes(BpmnActivityBindingDraft draft, string inputName, object value) =>
        draft.Activity.SetProperty(value.SerializeToNode(EditorOptions), inputName);

    /// <summary>
    /// The activity JSON elsa-core's <c>BpmnActivityBindingFormat.Read</c> hands its activity serializer for a binding
    /// element — <c>{"type": activityType, &lt;name&gt;: &lt;parsed input text&gt;, …}</c> — rebuilt here independently of
    /// Studio's own reader, from the raw document shape, so the assertion does not rest on the code under test.
    /// </summary>
    private static JsonObject ReadAsElsaCore(JsonObject binding)
    {
        const string elsa = "https://elsaworkflows.io/schemas/bpmn/v1";

        Assert.Equal(elsa, binding["name"]!["ns"]!.GetValue<string>());
        Assert.Equal("activityBinding", binding["name"]!["localName"]!.GetValue<string>());

        var activity = new JsonObject { ["type"] = UnqualifiedAttribute(binding, "activityType") };

        foreach (var input in binding["children"]!.AsArray().Select(child => child!.AsObject()))
        {
            Assert.Equal(elsa, input["name"]!["ns"]!.GetValue<string>());
            Assert.Equal("input", input["name"]!["localName"]!.GetValue<string>());

            var name = UnqualifiedAttribute(input, "name");
            Assert.False(activity.ContainsKey(name), $"The input '{name}' is declared twice.");
            activity[name] = JsonNode.Parse(input["value"]!.GetValue<string>());
        }

        return activity;
    }

    private static string UnqualifiedAttribute(JsonObject element, string localName) =>
        element["attributes"]!.AsArray()
            .Select(attribute => attribute!.AsObject())
            .Single(attribute => attribute["name"]!["ns"] is null && attribute["name"]!["localName"]!.GetValue<string>() == localName)["value"]!
            .GetValue<string>();
}
