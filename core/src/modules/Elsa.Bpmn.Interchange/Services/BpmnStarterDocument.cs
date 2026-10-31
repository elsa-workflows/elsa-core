using System.Xml.Linq;
using Bpmn.Interchange;

namespace Elsa.Bpmn.Interchange.Services;

/// <summary>
/// The document a new BPMN workflow starts from: one process holding a plain start event connected to an end event,
/// with the diagram layout a modeler needs to draw them.
/// </summary>
internal static class BpmnStarterDocument
{
    private static readonly XNamespace Bpmn = BpmnXmlNames.Model;
    private static readonly XNamespace BpmnDi = BpmnXmlNames.Di;
    private static readonly XNamespace Dc = BpmnXmlNames.Dc;
    private static readonly XNamespace Di = BpmnXmlNames.Dd;

    /// <summary>Writes the starter document for a process with <paramref name="processId"/> and <paramref name="name"/>.</summary>
    public static string Create(string processId, string name)
    {
        var document = new XDocument(
            new XDeclaration("1.0", "UTF-8", null),
            new XElement(Bpmn + "definitions",
                new XAttribute(XNamespace.Xmlns + "bpmn", Bpmn.NamespaceName),
                new XAttribute(XNamespace.Xmlns + "bpmndi", BpmnDi.NamespaceName),
                new XAttribute(XNamespace.Xmlns + "dc", Dc.NamespaceName),
                new XAttribute(XNamespace.Xmlns + "di", Di.NamespaceName),
                new XAttribute("id", $"Definitions_{processId}"),
                new XAttribute("targetNamespace", "https://elsaworkflows.io/bpmn"),
                new XElement(Bpmn + "process",
                    new XAttribute("id", processId),
                    new XAttribute("name", name),
                    new XAttribute("isExecutable", "true"),
                    new XElement(Bpmn + "startEvent", new XAttribute("id", "StartEvent_1"), new XElement(Bpmn + "outgoing", "Flow_1")),
                    new XElement(Bpmn + "endEvent", new XAttribute("id", "EndEvent_1"), new XElement(Bpmn + "incoming", "Flow_1")),
                    new XElement(Bpmn + "sequenceFlow", new XAttribute("id", "Flow_1"), new XAttribute("sourceRef", "StartEvent_1"), new XAttribute("targetRef", "EndEvent_1"))),
                new XElement(BpmnDi + "BPMNDiagram",
                    new XAttribute("id", "BPMNDiagram_1"),
                    new XElement(BpmnDi + "BPMNPlane",
                        new XAttribute("id", "BPMNPlane_1"),
                        new XAttribute("bpmnElement", processId),
                        Shape("StartEvent_1", x: 180),
                        Shape("EndEvent_1", x: 312),
                        new XElement(BpmnDi + "BPMNEdge",
                            new XAttribute("id", "Flow_1_di"),
                            new XAttribute("bpmnElement", "Flow_1"),
                            Waypoint(216),
                            Waypoint(312))))));

        return document.Declaration + Environment.NewLine + document;
    }

    private static XElement Shape(string elementId, int x) =>
        new(BpmnDi + "BPMNShape",
            new XAttribute("id", $"{elementId}_di"),
            new XAttribute("bpmnElement", elementId),
            new XElement(Dc + "Bounds", new XAttribute("x", x), new XAttribute("y", 100), new XAttribute("width", 36), new XAttribute("height", 36)));

    private static XElement Waypoint(int x) => new(Di + "waypoint", new XAttribute("x", x), new XAttribute("y", 118));
}
