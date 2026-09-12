using System.Xml.Linq;

namespace Elsa.Bpmn.Interchange.IntegrationTests.Support;

/// <summary>
/// The XML namespaces every test that parses a BPMN document with <see cref="XDocument"/> needs, kept in one place
/// so <c>camunda-order-process.bpmn</c>'s fixtures agree on them everywhere they are asserted against.
/// </summary>
internal static class BpmnXNamespaces
{
    public static readonly XNamespace Camunda = "http://camunda.org/schema/1.0/bpmn";
    public static readonly XNamespace Elsa = "https://elsaworkflows.io/schemas/bpmn/v1";
    public static readonly XNamespace Dc = "http://www.omg.org/spec/DD/20100524/DC";
    public static readonly XNamespace Di = "http://www.omg.org/spec/DD/20100524/DI";
    public static readonly XNamespace Bpmn = "http://www.omg.org/spec/BPMN/20100524/MODEL";
}
