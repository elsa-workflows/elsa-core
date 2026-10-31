using Bpmn.Interchange;
using Elsa.Bpmn.Interchange.Exceptions;
using Elsa.Bpmn.Interchange.Services;

namespace Elsa.Bpmn.Interchange.UnitTests;

/// <summary>
/// The conditions import still refuses now that Elsa evaluates condition expressions:
/// <see cref="BpmnInterchangeDocumentService.EnsureConditionsEvaluable"/> is the internal seam the Import endpoint and the
/// document <c>PUT</c> both call into. See <c>Elsa.Bpmn.Interchange.IntegrationTests.Endpoints.BpmnInterchangeEndpointTests</c>
/// for the refusals through a real HTTP request.
/// </summary>
public class BpmnInterchangeDocumentServiceExpressionConditionTests
{
    private const string BpmnNamespace = "http://www.omg.org/spec/BPMN/20100524/MODEL";
    private const string VendorNamespace = "https://bpmn.valenceworks.io/schema/bpmn";

    [Theory(DisplayName = "A condition expression in a language Elsa evaluates is accepted")]
    [InlineData(null)]
    [InlineData("javascript")]
    [InlineData("JavaScript")]
    [InlineData("csharp")]
    [InlineData("liquid")]
    [InlineData("https://elsaworkflows.io/expressions/javascript")]
    public void AcceptsASupportedLanguage(string? language)
    {
        var languageAttribute = language is null ? "" : $" language=\"{language}\"";
        var xml = Document($"""
            <sequenceFlow id="f1" sourceRef="Start" targetRef="End">
              <conditionExpression{languageAttribute}>x &gt; 1</conditionExpression>
            </sequenceFlow>
            """);

        // No exception is the assertion.
        Check(xml);
    }

    [Fact(DisplayName = "A condition expression in a language Elsa does not evaluate is refused, naming the flows sorted")]
    public void RefusesAnUnsupportedLanguage()
    {
        var xml = Document("""
            <sequenceFlow id="z" sourceRef="Start" targetRef="End">
              <conditionExpression language="http://www.w3.org/1999/XPath">x &gt; 1</conditionExpression>
            </sequenceFlow>
            <sequenceFlow id="a" sourceRef="Start" targetRef="End">
              <conditionExpression language="juel">${x &gt; 1}</conditionExpression>
            </sequenceFlow>
            """);

        var exception = Assert.Throws<BpmnConditionRefusedException>(() => Check(xml));

        Assert.Equal(BpmnErrorCodes.ImportConditionLanguageUnsupported, exception.Code);
        Assert.Equal(["a", "z"], exception.FlowIds);
        Assert.Contains("'juel'", exception.Message);
    }

    [Fact(DisplayName = "The document's expression language applies to a condition that names none")]
    public void AppliesTheDocumentLanguage()
    {
        var xml = $"""
            <definitions xmlns="{BpmnNamespace}" id="d" targetNamespace="t" expressionLanguage="http://www.w3.org/1999/XPath">
              <process id="p">
                <startEvent id="Start" />
                <endEvent id="End" />
                <sequenceFlow id="f1" sourceRef="Start" targetRef="End"><conditionExpression>x &gt; 1</conditionExpression></sequenceFlow>
                <sequenceFlow id="f2" sourceRef="Start" targetRef="End"><conditionExpression language="javascript">x &gt; 1</conditionExpression></sequenceFlow>
              </process>
            </definitions>
            """;

        var exception = Assert.Throws<BpmnConditionRefusedException>(() => Check(xml));

        Assert.Equal(["f1"], exception.FlowIds);
    }

    [Fact(DisplayName = "A flow with both an outcome and an author-written condition expression is refused")]
    public void RefusesAnOutcomeWithAnAuthorExpression()
    {
        var xml = Document($"""
            <sequenceFlow id="f1" sourceRef="Start" targetRef="End" xmlns:x="{VendorNamespace}" x:conditionOutcome="Approved">
              <conditionExpression>amount &gt; 1000</conditionExpression>
            </sequenceFlow>
            """);

        var exception = Assert.Throws<BpmnConditionRefusedException>(() => Check(xml));

        Assert.Equal(BpmnErrorCodes.ImportConditionConflict, exception.Code);
        Assert.Equal(["f1"], exception.FlowIds);
    }

    [Fact(DisplayName = "A flow conditional on an outcome is accepted, with or without the expression the library's writer emits beside it")]
    public void AcceptsConditionOutcomeFlows()
    {
        var xml = Document($"""
            <sequenceFlow id="f1" sourceRef="Start" targetRef="End" xmlns:x="{VendorNamespace}" x:conditionOutcome="Approved">
              <conditionExpression>outcome == 'Approved'</conditionExpression>
            </sequenceFlow>
            <sequenceFlow id="f2" sourceRef="Start" targetRef="End" xmlns:x="{VendorNamespace}" x:conditionOutcome="Rejected" />
            """);

        // No exception is the assertion.
        Check(xml);
    }

    private static void Check(string xml)
    {
        var result = new BpmnXmlReader().Read(xml, new BpmnImportOptions());
        BpmnInterchangeDocumentService.EnsureConditionsEvaluable(result);
    }

    /// <summary>A one-process document in the BPMN default namespace (no prefix), with <paramref name="flows"/> between a start and an end event.</summary>
    private static string Document(string flows) => $"""
        <definitions xmlns="{BpmnNamespace}" id="d" targetNamespace="t">
          <process id="p">
            <startEvent id="Start" />
            <endEvent id="End" />
            {flows}
          </process>
        </definitions>
        """;
}
