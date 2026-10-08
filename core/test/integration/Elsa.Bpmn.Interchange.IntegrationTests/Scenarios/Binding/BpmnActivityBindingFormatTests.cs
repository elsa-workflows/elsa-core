using Bpmn.Model;
using Elsa.Bpmn.Interchange.Binding;
using Elsa.Bpmn.Interchange.Exceptions;
using Elsa.Expressions.Models;
using Elsa.Workflows.Activities;
using Xunit.Abstractions;

namespace Elsa.Bpmn.Interchange.IntegrationTests.Scenarios.Binding;

/// <summary>
/// The <c>elsa:activityBinding</c> extension element: what it holds, and that what goes in comes back out.
/// </summary>
/// <remarks>
/// This element is a compatibility surface — Elsa Studio writes it and every exported <c>.bpmn</c> is bound to it —
/// so the round-trip is asserted on the element's own shape, not only on the activity that comes back. A change to a
/// name or to how an input is encoded shows up here as a failing assertion rather than as files that stop importing.
/// </remarks>
public class BpmnActivityBindingFormatTests(ITestOutputHelper testOutputHelper) : BpmnBindingTestBase(testOutputHelper)
{
    [Fact(DisplayName = "A written binding names the activity type and carries one element per configured input")]
    public void Write_ProducesTheDocumentedShape()
    {
        var element = Format.Write(new WriteLine("hello"));

        Assert.Equal(new BpmnQName(BpmnActivityBindingFormat.NamespaceUri, BpmnActivityBindingFormat.BindingElementName), element.Name);
        Assert.Equal("Elsa.WriteLine", AttributeOf(element, BpmnActivityBindingFormat.ActivityTypeAttributeName));

        var input = Assert.Single(element.Children);

        Assert.Equal(new BpmnQName(BpmnActivityBindingFormat.NamespaceUri, BpmnActivityBindingFormat.InputElementName), input.Name);
        Assert.Equal("text", AttributeOf(input, BpmnActivityBindingFormat.InputNameAttributeName));
        Assert.Contains("\"typeName\"", input.Value);
        Assert.Contains("hello", input.Value);
    }

    [Fact(DisplayName = "A binding written then read back carries its literal inputs intact")]
    public void WriteThenRead_PreservesALiteralInput()
    {
        var element = Format.Write(new WriteLine("hello"));

        var activity = Assert.IsType<WriteLine>(Format.Read(element));

        Assert.Equal("Elsa.WriteLine", activity.Type);
        Assert.Equal("hello", ValueOf<string>(activity.Text));
    }

    [Fact(DisplayName = "A binding written then read back carries an expression, not just a literal")]
    public void WriteThenRead_PreservesAnExpression()
    {
        // The reason the disclosure note on BpmnActivityBindingFormat is not theoretical: an exported .bpmn carries
        // expression source verbatim. It also proves the encoding is Elsa's own input JSON rather than a value dump —
        // a value-only format would silently degrade this input to a literal, or to nothing.
        var element = Format.Write(new WriteLine(new Expression("JavaScript", "getSecretMessage()")));

        Assert.Contains("getSecretMessage()", Assert.Single(element.Children).Value);

        var activity = Assert.IsType<WriteLine>(Format.Read(element));

        Assert.Equal("JavaScript", activity.Text.Expression!.Type);
        Assert.Equal("getSecretMessage()", ValueOf<string>(activity.Text));
    }

    [Fact(DisplayName = "A binding written then read back carries an attribute-declared input intact, not only an Input<T>-typed one")]
    public void WriteThenRead_PreservesAnAttributeDeclaredInput()
    {
        // Switch.Cases is ICollection<SwitchCase>: [Input] on a plain-typed property, not one that derives from Input.
        // Write used to enumerate only properties whose CLR type derives from Input, which silently dropped this kind
        // of configuration from the export.
        var activity = new Switch
        {
            Cases = { new SwitchCase { Label = "case one", Condition = new Expression("Literal", true) } }
        };

        var element = Format.Write(activity);

        Assert.Contains(element.Children, child => AttributeOf(child, BpmnActivityBindingFormat.InputNameAttributeName) == "cases");

        var read = Assert.IsType<Switch>(Format.Read(element));
        var readCase = Assert.Single(read.Cases);

        Assert.Equal("case one", readCase.Label);
        Assert.Equal(true, readCase.Condition.Value);
    }

    [Fact(DisplayName = "A binding naming an input the activity type does not declare is refused, not silently dropped")]
    public void Read_RefusesAnUnknownInputName()
    {
        // Elsa's own deserializer ignores a JSON member the target type does not declare, so a mistyped or stale
        // input name would otherwise import as an activity quietly missing that configuration, with no diagnostic
        // anywhere. WriteLine declares "text", not "txt".
        var inputName = new BpmnQName(BpmnActivityBindingFormat.NamespaceUri, BpmnActivityBindingFormat.InputElementName);
        var nameAttribute = Attribute(BpmnActivityBindingFormat.InputNameAttributeName, "txt");

        var element = new BpmnExtensionElement(
            new(BpmnActivityBindingFormat.NamespaceUri, BpmnActivityBindingFormat.BindingElementName),
            [Attribute(BpmnActivityBindingFormat.ActivityTypeAttributeName, "Elsa.WriteLine")],
            [new BpmnExtensionElement(inputName, [nameAttribute], null, "{\"typeName\":\"String\",\"expression\":{\"type\":\"Literal\",\"value\":\"hello\"}}")]);

        var exception = Assert.Throws<BpmnBindingException>(() => Format.Read(element));

        Assert.Contains("txt", exception.Message);
    }

    [Fact(DisplayName = "A binding naming an activity type nothing registered is refused, not turned into a placeholder")]
    public void Read_RefusesAnUnregisteredActivityType()
    {
        // Elsa answers an unknown activity type with a NotFoundActivity that only throws once it executes. Accepting
        // it here would import cleanly, publish cleanly, and fail in the middle of a running process.
        var element = new BpmnExtensionElement(
            new(BpmnActivityBindingFormat.NamespaceUri, BpmnActivityBindingFormat.BindingElementName),
            [new(new(null, BpmnActivityBindingFormat.ActivityTypeAttributeName), "Contoso.NoSuchActivity")]);

        var exception = Assert.Throws<BpmnBindingException>(() => Format.Read(element));

        Assert.Contains("Contoso.NoSuchActivity", exception.Message);
    }

    [Fact(DisplayName = "A binding declaring no activity type is refused")]
    public void Read_RefusesAnElementWithNoActivityType()
    {
        var element = new BpmnExtensionElement(new(BpmnActivityBindingFormat.NamespaceUri, BpmnActivityBindingFormat.BindingElementName));

        Assert.Throws<BpmnBindingException>(() => Format.Read(element));
    }

    [Fact(DisplayName = "A binding declaring the same input name twice is refused, not resolved last-wins")]
    public void Read_RefusesADuplicateInputName()
    {
        // Two <elsa:input name="text"> children for the same activity type. The second would otherwise silently win,
        // leaving the author's first configuration in the file but never applied — exactly the quiet wrong answer
        // this binder refuses everywhere else (an unbound task, a dead declaration, an unregistered activity type, a
        // malformed timer duration, a call activity with no calledElement).
        var inputName = new BpmnQName(BpmnActivityBindingFormat.NamespaceUri, BpmnActivityBindingFormat.InputElementName);
        var nameAttribute = Attribute(BpmnActivityBindingFormat.InputNameAttributeName, "text");

        var element = new BpmnExtensionElement(
            new(BpmnActivityBindingFormat.NamespaceUri, BpmnActivityBindingFormat.BindingElementName),
            [Attribute(BpmnActivityBindingFormat.ActivityTypeAttributeName, "Elsa.WriteLine")],
            [
                new BpmnExtensionElement(inputName, [nameAttribute], null, "{\"typeName\":\"String\",\"expression\":{\"type\":\"Literal\",\"value\":\"first\"}}"),
                new BpmnExtensionElement(inputName, [nameAttribute], null, "{\"typeName\":\"String\",\"expression\":{\"type\":\"Literal\",\"value\":\"second\"}}")
            ]);

        var exception = Assert.Throws<BpmnBindingException>(() => Format.Read(element));

        Assert.Contains("text", exception.Message);
    }

    [Fact(DisplayName = "Attaching a binding replaces the previous one and leaves other retained content alone")]
    public void Attach_ReplacesTheBindingAndKeepsForeignContent()
    {
        var foreign = new BpmnExtensionElement(new("http://camunda.org/schema/1.0/bpmn", "properties"));
        var extensions = new BpmnExtensions(ExtensionElements: [foreign, Format.Write(new WriteLine("first"))]);

        var updated = BpmnActivityBindingFormat.Attach(extensions, Format.Write(new WriteLine("second")));

        Assert.Contains(foreign, updated.ExtensionElements);
        Assert.Equal("second", ValueOf<string>(Assert.IsType<WriteLine>(Format.Read(BpmnActivityBindingFormat.Find(updated)!)).Text));

        // Two bindings on one element would leave a reader taking the first of them applying the older one.
        Assert.Single(updated.ExtensionElements, element => element.Name.LocalName == BpmnActivityBindingFormat.BindingElementName);
    }

    [Fact(DisplayName = "Retained content declaring no binding reports none")]
    public void Find_ReturnsNullWhenNothingIsDeclared()
    {
        Assert.Null(BpmnActivityBindingFormat.Find(null));
        Assert.Null(BpmnActivityBindingFormat.Find(BpmnExtensions.Empty));
    }

    private static string? AttributeOf(BpmnExtensionElement element, string name) =>
        element.Attributes.FirstOrDefault(attribute => attribute.Name.LocalName == name)?.Value;

    private static BpmnForeignAttribute Attribute(string name, string value) => new(new(null, name), value);
}
