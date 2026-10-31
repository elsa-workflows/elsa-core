using System.Text.Json.Nodes;
using Elsa.Studio.Bpmn.Designers;
using Elsa.Studio.Workflows.DiagramDesigners.Bpmn;
using Elsa.Studio.Workflows.Domain.Models.Bpmn;
using Xunit;
using static Elsa.Studio.Workflows.Tests.Support.BpmnDocumentFixtures;

namespace Elsa.Studio.Workflows.Tests;

/// <summary>
/// Covers <see cref="BpmnDesignerBindings"/>: a binding read from a designer and written back from a draft keeps every
/// input value exactly, and is refused by the same rules elsa-core's reader applies.
/// </summary>
public class BpmnDesignerBindingsTests
{
    private static readonly BpmnDesignerBinding Binding = new("Elsa.WriteLine", [new("text", """{"typeName":"String","expression":{"type":"Literal","value":"a < b"}}""")]);

    [Fact(DisplayName = "A binding read into a draft and written back is the same binding, with readable JSON")]
    public void RoundTripsThroughADraft()
    {
        var draft = BpmnActivityBindingDraft.FromBinding(BpmnDesignerBindings.Read(Binding), WriteLine(), TaskId);

        var written = BpmnDesignerBindings.From(draft);

        Assert.True(BpmnDesignerBindings.AreEquivalent(Binding, written));
        Assert.Contains("a < b", Assert.Single(written.Inputs).Value);
    }

    [Fact(DisplayName = "An input named twice is refused")]
    public void RefusesARepeatedInput() =>
        Assert.Throws<BpmnActivityBindingFormatException>(() => BpmnDesignerBindings.Read(new("Elsa.WriteLine", [new("text", "1"), new("text", "2")])));

    [Fact(DisplayName = "An input that is not JSON is refused")]
    public void RefusesAnInputThatIsNotJson() =>
        Assert.Throws<BpmnActivityBindingFormatException>(() => BpmnDesignerBindings.Read(new("Elsa.WriteLine", [new("text", "{not json")])));

    [Fact(DisplayName = "Bindings are the same whatever their input order or JSON spacing, and differ on any value or type")]
    public void ComparesByMeaning()
    {
        BpmnDesignerBinding Make(string type, params (string Name, string Value)[] inputs) => new(type, inputs.Select(input => new BpmnDesignerBindingInput(input.Name, input.Value)).ToList());

        Assert.True(BpmnDesignerBindings.AreEquivalent(Make("A", ("x", "{\"a\":1}"), ("y", "2")), Make("A", ("y", "2"), ("x", "{ \"a\": 1 }"))));
        Assert.False(BpmnDesignerBindings.AreEquivalent(Make("A", ("x", "1")), Make("A", ("x", "2"))));
        Assert.False(BpmnDesignerBindings.AreEquivalent(Make("A", ("x", "1")), Make("B", ("x", "1"))));
        Assert.False(BpmnDesignerBindings.AreEquivalent(Make("A", ("x", "1")), null));
        Assert.True(BpmnDesignerBindings.AreEquivalent(null, null));
    }

    [Fact(DisplayName = "An input whose value is JSON null counts as absent, as a written binding leaves it out")]
    public void TreatsANullInputAsAbsent()
    {
        var withNull = new BpmnDesignerBinding("Elsa.WriteLine", [new("text", "\"hi\""), new("delay", "null")]);
        var draft = BpmnActivityBindingDraft.FromBinding(BpmnDesignerBindings.Read(withNull), WriteLine(), TaskId);

        Assert.True(BpmnDesignerBindings.AreEquivalent(withNull, BpmnDesignerBindings.From(draft)));
        Assert.False(BpmnDesignerBindings.AreEquivalent(withNull, new("Elsa.WriteLine", [new("text", "\"bye\"")])));
    }

    [Fact(DisplayName = "A binding that names an input twice matches nothing, even when one of the two is null")]
    public void ARepeatedName_MatchesNothing()
    {
        var repeated = new BpmnDesignerBinding("Elsa.WriteLine", [new("text", "\"hi\""), new("text", "null")]);

        Assert.False(BpmnDesignerBindings.AreEquivalent(repeated, new("Elsa.WriteLine", [new("text", "\"hi\"")])));
    }

    [Fact(DisplayName = "Each value is read as the JSON node it holds")]
    public void ReadsValuesAsJson()
    {
        var read = BpmnDesignerBindings.Read(Binding);

        Assert.Equal("a < b", Assert.Single(read.Inputs).Value!["expression"]!["value"]!.GetValue<string>());
        Assert.IsType<JsonObject>(read.Inputs[0].Value);
    }
}
