using Bpmn.Interchange;
using Elsa.Bpmn.Interchange.Exceptions;
using Elsa.Bpmn.Interchange.Services;

namespace Elsa.Bpmn.Interchange.UnitTests;

/// <summary>
/// Expression-condition refusal at import (elsa-core#8903): <see cref="BpmnInterchangeDocumentService.EnsureNoExpressionConditions"/>
/// is the internal seam the Import endpoint and the document <c>PUT</c> both call into, exercised here with the edge
/// cases its namespace-aware detection exists for. See
/// <c>Elsa.Bpmn.Interchange.IntegrationTests.Endpoints.BpmnInterchangeEndpointTests</c> for the refusal through a real
/// HTTP request, nested subprocesses included.
/// </summary>
public class BpmnInterchangeDocumentServiceExpressionConditionTests
{
    private const string BpmnNamespace = "http://www.omg.org/spec/BPMN/20100524/MODEL";

    [Fact(DisplayName = "A flow whose expression condition is in the BPMN namespace under any prefix is refused")]
    public void RefusesAnExpressionConditionUnderAnyPrefix()
    {
        var xml = Document("""
            <sequenceFlow id="f1" sourceRef="Start" targetRef="End">
              <conditionExpression>${x &gt; 1}</conditionExpression>
            </sequenceFlow>
            """);

        var exception = Assert.Throws<BpmnConditionUnsupportedException>(() => Check(xml));

        Assert.Equal(["f1"], exception.FlowIds);
        Assert.Contains("f1", exception.Message);
    }

    [Fact(DisplayName = "Expression conditions in every top-level process are refused, sorted by flow id")]
    public void RefusesExpressionConditionsAcrossEveryProcessSorted()
    {
        var xml = $"""
            <bpmn:definitions xmlns:bpmn="{BpmnNamespace}" id="d" targetNamespace="t">
              <bpmn:process id="p2">
                <bpmn:startEvent id="S2" /><bpmn:endEvent id="E2" />
                <bpmn:sequenceFlow id="z" sourceRef="S2" targetRef="E2"><bpmn:conditionExpression>a</bpmn:conditionExpression></bpmn:sequenceFlow>
              </bpmn:process>
              <bpmn:process id="p1">
                <bpmn:startEvent id="S1" /><bpmn:endEvent id="E1" />
                <bpmn:sequenceFlow id="a" sourceRef="S1" targetRef="E1"><bpmn:conditionExpression>b</bpmn:conditionExpression></bpmn:sequenceFlow>
              </bpmn:process>
            </bpmn:definitions>
            """;

        var exception = Assert.Throws<BpmnConditionUnsupportedException>(() => Check(xml));

        Assert.Equal(["a", "z"], exception.FlowIds);
    }

    [Fact(DisplayName = "A flow conditional on an outcome is accepted, with or without the expression the library's writer emits beside it")]
    public void AcceptsConditionOutcomeFlows()
    {
        var xml = Document("""
            <sequenceFlow id="f1" sourceRef="Start" targetRef="End" xmlns:x="https://bpmn.valenceworks.io/schema/bpmn" x:conditionOutcome="Approved">
              <conditionExpression>outcome == 'Approved'</conditionExpression>
            </sequenceFlow>
            <sequenceFlow id="f2" sourceRef="Start" targetRef="End" xmlns:x="https://bpmn.valenceworks.io/schema/bpmn" x:conditionOutcome="Rejected" />
            """);

        // No exception is the assertion.
        Check(xml);
    }

    [Fact(DisplayName = "A conditionExpression element outside the BPMN namespace is foreign content, not a condition, and is accepted")]
    public void AcceptsAForeignNamespaceConditionExpression()
    {
        var xml = Document("""
            <sequenceFlow id="f1" sourceRef="Start" targetRef="End">
              <conditionExpression xmlns="urn:example:not-bpmn">${x &gt; 1}</conditionExpression>
            </sequenceFlow>
            """);

        // No exception is the assertion.
        Check(xml);
    }

    private static void Check(string xml)
    {
        var result = new BpmnXmlReader().Read(xml, new BpmnImportOptions());
        BpmnInterchangeDocumentService.EnsureNoExpressionConditions(xml, result);
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
