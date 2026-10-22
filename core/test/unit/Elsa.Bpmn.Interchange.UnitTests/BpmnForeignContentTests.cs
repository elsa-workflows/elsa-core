using Elsa.Bpmn.Interchange.Endpoints.Bpmn.Xml;

namespace Elsa.Bpmn.Interchange.UnitTests;

/// <summary>
/// What the XML <c>PUT</c> reports as dropped: <see cref="BpmnForeignContent.DroppedBetween"/> compares the foreign
/// content of the stored and the new document, ignoring what does not change it.
/// </summary>
public class BpmnForeignContentTests
{
    [Fact(DisplayName = "A renamed prefix, reordered attributes and changed whitespace drop nothing")]
    public void DroppedBetween_IgnoresSerializationDifferences()
    {
        var stored = Document("acme", """<bpmn:task id="T" acme:a="1" acme:b="2"><bpmn:extensionElements><acme:hint level="2" mode="x">text</acme:hint></bpmn:extensionElements></bpmn:task>""");
        var saved = Document("v", """
            <bpmn:task acme:b="2" id="T" acme:a="1">
              <bpmn:extensionElements>
                <v:hint mode="x" level="2">text</v:hint>
              </bpmn:extensionElements>
            </bpmn:task>
            """.Replace("acme:", "v:"));

        Assert.Empty(BpmnForeignContent.DroppedBetween(stored, saved));
    }

    [Fact(DisplayName = "A lost or changed attribute, and a lost or changed element, are each reported under their owner")]
    public void DroppedBetween_ReportsLostAndChangedContent()
    {
        var stored = Document("acme", """<bpmn:task id="T" acme:a="1" acme:b="2"><bpmn:extensionElements><acme:hint level="2"/><acme:note>n</acme:note></bpmn:extensionElements></bpmn:task>""");
        var saved = Document("acme", """<bpmn:task id="T" acme:b="3"><bpmn:extensionElements><acme:note>n</acme:note><acme:hint level="5"/></bpmn:extensionElements></bpmn:task>""");

        var dropped = BpmnForeignContent.DroppedBetween(stored, saved).Select(item => $"{item.ElementId} {item.Kind} {item.Name}");

        Assert.Equal(["T attribute a", "T attribute b", "T element hint"], dropped);
    }

    [Fact(DisplayName = "Content that moves to another element, or loses one of two identical copies, is reported")]
    public void DroppedBetween_ComparesByOwnerAndAsAMultiset()
    {
        var stored = Document("acme", """<bpmn:task id="T"><acme:note/><acme:note/></bpmn:task><bpmn:task id="U"/>""");
        var saved = Document("acme", """<bpmn:task id="T"/><bpmn:task id="U"><acme:note/></bpmn:task>""");

        var dropped = BpmnForeignContent.DroppedBetween(stored, saved).Select(item => $"{item.ElementId} {item.Kind} {item.Name}");

        Assert.Equal(["T element note", "T element note"], dropped);
    }

    [Fact(DisplayName = "BPMN's own content, Elsa's bindings and xsi are not foreign")]
    public void DroppedBetween_IgnoresContentThatIsNotForeign()
    {
        var stored = Document("acme", """
            <bpmn:task id="T" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" xmlns:elsa="https://elsaworkflows.io/schemas/bpmn/v1">
              <bpmn:documentation>Docs</bpmn:documentation>
              <bpmn:extensionElements><elsa:activityBinding activityType="Elsa.WriteLine"/></bpmn:extensionElements>
              <bpmn:conditionExpression xsi:type="bpmn:tFormalExpression">x</bpmn:conditionExpression>
            </bpmn:task>
            """);
        var saved = Document("acme", """<bpmn:task id="T"/>""");

        Assert.Empty(BpmnForeignContent.DroppedBetween(stored, saved));
    }

    [Fact(DisplayName = "An attribute value that reads like a second attribute is not mistaken for one")]
    public void DroppedBetween_DoesNotConfuseAValueWithAnAttributeBoundary()
    {
        var stored = Document("acme", """<bpmn:task id="T"><acme:hint a="1&quot; |b=&quot;2"/></bpmn:task>""");
        var saved = Document("acme", """<bpmn:task id="T"><acme:hint a="1" b="2"/></bpmn:task>""");

        var dropped = BpmnForeignContent.DroppedBetween(stored, saved).Select(item => $"{item.ElementId} {item.Kind} {item.Name}");

        Assert.Equal(["T element hint"], dropped);
    }

    [Fact(DisplayName = "Text split across CDATA sections or around a comment is the same text written in one piece")]
    public void DroppedBetween_JoinsTextBetweenChildElements()
    {
        var stored = Document("acme", """<bpmn:task id="T"><acme:hint>a<![CDATA[b]]>c<!-- note -->d<acme:x/>e</acme:hint></bpmn:task>""");
        var same = Document("acme", """<bpmn:task id="T"><acme:hint>abcd<acme:x/>e</acme:hint></bpmn:task>""");
        var moved = Document("acme", """<bpmn:task id="T"><acme:hint>abc<acme:x/>de</acme:hint></bpmn:task>""");

        Assert.Empty(BpmnForeignContent.DroppedBetween(stored, same));
        Assert.Single(BpmnForeignContent.DroppedBetween(stored, moved));
    }

    [Fact(DisplayName = "Renaming the only process keeps the content it carries; with two processes, ids still decide")]
    public void DroppedBetween_ComparesTheOnlyProcessUnderOneOwnerWhateverItsId()
    {
        const string bpmn = "http://www.omg.org/spec/BPMN/20100524/MODEL";
        string Definitions(string processes) => $"""<bpmn:definitions xmlns:bpmn="{bpmn}" xmlns:acme="urn:acme" id="D" targetNamespace="x">{processes}</bpmn:definitions>""";

        var renamed = BpmnForeignContent.DroppedBetween(
            Definitions("""<bpmn:process id="P" acme:a="1"><bpmn:extensionElements><acme:hint/></bpmn:extensionElements></bpmn:process>"""),
            Definitions("""<bpmn:process id="Q" acme:a="1"><bpmn:extensionElements><acme:hint/></bpmn:extensionElements></bpmn:process>"""));
        var oneOfTwoRenamed = BpmnForeignContent.DroppedBetween(
            Definitions("""<bpmn:process id="P" acme:a="1"/><bpmn:process id="R"/>"""),
            Definitions("""<bpmn:process id="Q" acme:a="1"/><bpmn:process id="R"/>"""));

        Assert.Empty(renamed);
        Assert.Equal(["P attribute a"], oneOfTwoRenamed.Select(item => $"{item.ElementId} {item.Kind} {item.Name}"));
    }

    [Fact(DisplayName = "A deeply nested extension is compared without exhausting the call stack")]
    public void DroppedBetween_ComparesADeeplyNestedExtension()
    {
        const int depth = 20_000;
        string Nested(string leaf) => string.Concat(Enumerable.Repeat("<acme:n>", depth)) + leaf + string.Concat(Enumerable.Repeat("</acme:n>", depth));

        var stored = Document("acme", $"""<bpmn:task id="T">{Nested("a")}</bpmn:task>""");
        var saved = Document("acme", $"""<bpmn:task id="T">{Nested("b")}</bpmn:task>""");

        Assert.Equal(["T element n"], BpmnForeignContent.DroppedBetween(stored, saved).Select(item => $"{item.ElementId} {item.Kind} {item.Name}"));
    }

    [Theory(DisplayName = "Nothing is reported without a stored document, or when one cannot be parsed")]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("<bpmn:definitions")]
    public void DroppedBetween_ReportsNothingWithoutAComparableStoredDocument(string? stored)
    {
        Assert.Empty(BpmnForeignContent.DroppedBetween(stored, Document("acme", """<bpmn:task id="T"/>""")));
    }

    private static string Document(string prefix, string process) =>
        $"""<bpmn:definitions xmlns:bpmn="http://www.omg.org/spec/BPMN/20100524/MODEL" xmlns:{prefix}="urn:acme" id="D" targetNamespace="x"><bpmn:process id="P">{process}</bpmn:process></bpmn:definitions>""";
}
