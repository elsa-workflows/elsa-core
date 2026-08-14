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
}
