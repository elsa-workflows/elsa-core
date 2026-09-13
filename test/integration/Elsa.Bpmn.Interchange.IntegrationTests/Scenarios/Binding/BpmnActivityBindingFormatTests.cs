using Bpmn.Model;
using Elsa.Bpmn.Interchange.Binding;
using Elsa.Bpmn.Interchange.Exceptions;
using Elsa.Expressions.Models;
using Elsa.Workflows.Activities;

namespace Elsa.Bpmn.Interchange.IntegrationTests.Scenarios.Binding;

/// <summary>
/// The <c>elsa:activityBinding</c> extension element: what it holds, and that what goes in comes back out.
/// </summary>
/// <remarks>
/// This element is a compatibility surface — Elsa Studio writes it and every exported <c>.bpmn</c> is bound to it —
/// so the round-trip is asserted on the element's own shape, not only on the activity that comes back. A change to a
/// name or to how an input is encoded shows up here as a failing assertion rather than as files that stop importing.
/// </remarks>
public class BpmnActivityBindingFormatTests : BpmnBindingTestBase
{
    [Test]
    [DisplayName("A written binding names the activity type and carries one element per configured input")]
    public async Task Write_ProducesTheDocumentedShape()
    {
        var element = Format.Write(new WriteLine("hello"));

        await Assert.That(element.Name).IsEqualTo(new BpmnQName(BpmnActivityBindingFormat.NamespaceUri, BpmnActivityBindingFormat.BindingElementName));
        await Assert.That(AttributeOf(element, BpmnActivityBindingFormat.ActivityTypeAttributeName)).IsEqualTo("Elsa.WriteLine");

        var input = (await Assert.That(element.Children).HasSingleItem())!;

        await Assert.That(input.Name).IsEqualTo(new BpmnQName(BpmnActivityBindingFormat.NamespaceUri, BpmnActivityBindingFormat.InputElementName));
        await Assert.That(AttributeOf(input, BpmnActivityBindingFormat.InputNameAttributeName)).IsEqualTo("text");
        await Assert.That(input.Value).Contains("\"typeName\"", StringComparison.CurrentCulture);
        await Assert.That(input.Value).Contains("hello", StringComparison.CurrentCulture);
    }

    [Test]
    [DisplayName("A binding written then read back carries its literal inputs intact")]
    public async Task WriteThenRead_PreservesALiteralInput()
    {
        var element = Format.Write(new WriteLine("hello"));

        var result = Format.Read(element);
        await Assert.That(result).IsOfType(typeof(WriteLine));
        var activity = (WriteLine)result;

        await Assert.That(activity.Type).IsEqualTo("Elsa.WriteLine");
        await Assert.That(ValueOf<string>(activity.Text)).IsEqualTo("hello");
    }

    [Test]
    [DisplayName("A binding written then read back carries an expression, not just a literal")]
    public async Task WriteThenRead_PreservesAnExpression()
    {
        // The reason the disclosure note on BpmnActivityBindingFormat is not theoretical: an exported .bpmn carries
        // expression source verbatim. It also proves the encoding is Elsa's own input JSON rather than a value dump —
        // a value-only format would silently degrade this input to a literal, or to nothing.
        var element = Format.Write(new WriteLine(new Expression("JavaScript", "getSecretMessage()")));

        var input = (await Assert.That(element.Children).HasSingleItem())!;
        await Assert.That(input.Value).Contains("getSecretMessage()", StringComparison.CurrentCulture);

        var result = Format.Read(element);
        await Assert.That(result).IsOfType(typeof(WriteLine));
        var activity = (WriteLine)result;

        await Assert.That(activity.Text.Expression!.Type).IsEqualTo("JavaScript");
        await Assert.That(ValueOf<string>(activity.Text)).IsEqualTo("getSecretMessage()");
    }

    [Test]
    [DisplayName("A binding written then read back carries an attribute-declared input intact, not only an Input<T>-typed one")]
    public async Task WriteThenRead_PreservesAnAttributeDeclaredInput()
    {
        // Switch.Cases is ICollection<SwitchCase>: [Input] on a plain-typed property, not one that derives from Input.
        // Write used to enumerate only properties whose CLR type derives from Input, which silently dropped this kind
        // of configuration from the export.
        var activity = new Switch
        {
            Cases = { new SwitchCase { Label = "case one", Condition = new Expression("Literal", true) } }
        };

        var element = Format.Write(activity);

        await Assert.That(element.Children).Contains(child => AttributeOf(child, BpmnActivityBindingFormat.InputNameAttributeName) == "cases");

        var result = Format.Read(element);
        await Assert.That(result).IsOfType(typeof(Switch));
        var read = (Switch)result;
        var readCase = (await Assert.That(read.Cases).HasSingleItem())!;

        await Assert.That(readCase.Label).IsEqualTo("case one");
        await Assert.That(readCase.Condition.Value is true).IsTrue();
    }

    [Test]
    [DisplayName("A binding naming an input the activity type does not declare is refused, not silently dropped")]
    public async Task Read_RefusesAnUnknownInputName()
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

        var exception = Assert.ThrowsExactly<BpmnBindingException>(() => Format.Read(element));

        await Assert.That(exception.Message).Contains("txt", StringComparison.CurrentCulture);
    }

    [Test]
    [DisplayName("A binding naming an activity type nothing registered is refused, not turned into a placeholder")]
    public async Task Read_RefusesAnUnregisteredActivityType()
    {
        // Elsa answers an unknown activity type with a NotFoundActivity that only throws once it executes. Accepting
        // it here would import cleanly, publish cleanly, and fail in the middle of a running process.
        var element = new BpmnExtensionElement(
            new(BpmnActivityBindingFormat.NamespaceUri, BpmnActivityBindingFormat.BindingElementName),
            [new(new(null, BpmnActivityBindingFormat.ActivityTypeAttributeName), "Contoso.NoSuchActivity")]);

        var exception = Assert.ThrowsExactly<BpmnBindingException>(() => Format.Read(element));

        await Assert.That(exception.Message).Contains("Contoso.NoSuchActivity", StringComparison.CurrentCulture);
    }

    [Test]
    [DisplayName("A binding declaring no activity type is refused")]
    public void Read_RefusesAnElementWithNoActivityType()
    {
        var element = new BpmnExtensionElement(new(BpmnActivityBindingFormat.NamespaceUri, BpmnActivityBindingFormat.BindingElementName));

        Assert.ThrowsExactly<BpmnBindingException>(() => Format.Read(element));
    }

    [Test]
    [DisplayName("A binding declaring the same input name twice is refused, not resolved last-wins")]
    public async Task Read_RefusesADuplicateInputName()
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

        var exception = Assert.ThrowsExactly<BpmnBindingException>(() => Format.Read(element));

        await Assert.That(exception.Message).Contains("text", StringComparison.CurrentCulture);
    }

    [Test]
    [DisplayName("Attaching a binding replaces the previous one and leaves other retained content alone")]
    public async Task Attach_ReplacesTheBindingAndKeepsForeignContent()
    {
        var foreign = new BpmnExtensionElement(new("http://camunda.org/schema/1.0/bpmn", "properties"));
        var extensions = new BpmnExtensions(ExtensionElements: [foreign, Format.Write(new WriteLine("first"))]);

        var updated = BpmnActivityBindingFormat.Attach(extensions, Format.Write(new WriteLine("second")));

        await Assert.That(updated.ExtensionElements).Contains(foreign);
        var result = Format.Read(BpmnActivityBindingFormat.Find(updated)!);
        await Assert.That(result).IsOfType(typeof(WriteLine));
        var activity = (WriteLine)result;
        await Assert.That(ValueOf<string>(activity.Text)).IsEqualTo("second");

        // Two bindings on one element would leave a reader taking the first of them applying the older one.
        await Assert.That(updated.ExtensionElements).HasSingleItem(element => element.Name.LocalName == BpmnActivityBindingFormat.BindingElementName);
    }

    [Test]
    [DisplayName("Retained content declaring no binding reports none")]
    public async Task Find_ReturnsNullWhenNothingIsDeclared()
    {
        await Assert.That(BpmnActivityBindingFormat.Find(null)).IsNull();
        await Assert.That(BpmnActivityBindingFormat.Find(BpmnExtensions.Empty)).IsNull();
    }

    private static string? AttributeOf(BpmnExtensionElement element, string name) =>
        element.Attributes.FirstOrDefault(attribute => attribute.Name.LocalName == name)?.Value;

    private static BpmnForeignAttribute Attribute(string name, string value) => new(new(null, name), value);
}
