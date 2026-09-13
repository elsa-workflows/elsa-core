using System.Xml.Linq;
using Bpmn.Interchange;
using Elsa.Bpmn.Interchange.IntegrationTests.Support;
using Elsa.Bpmn.Interchange.Services;
using Elsa.Common.Models;
using Elsa.Extensions;
using Elsa.Workflows.Management;
using Elsa.Workflows.Models;
using TUnit.Assertions.Enums;

namespace Elsa.Bpmn.Interchange.IntegrationTests.Scenarios.Interchange;

/// <summary>
/// The round trip the Analyze, Import and Export endpoints exist for: a Camunda-authored <c>.bpmn</c> file survives
/// import and export with its foreign extension elements, foreign attributes and BPMN DI waypoints intact.
/// </summary>
public class BpmnInterchangeDocumentServiceTests : BpmnInterchangeTestBase
{
    private static readonly XNamespace Camunda = BpmnXNamespaces.Camunda;
    private static readonly XNamespace Elsa = BpmnXNamespaces.Elsa;
    private static readonly XNamespace Dc = BpmnXNamespaces.Dc;
    private static readonly XNamespace Di = BpmnXNamespaces.Di;
    private static readonly XNamespace Bpmn = BpmnXNamespaces.Bpmn;

    [Test]
    [DisplayName("Analyze and Import report the same findings for the same document")]
    public async Task Analyze_AndImport_ReportTheSameFindings()
    {
        var xml = ReadAsset("camunda-order-process.bpmn");

        var analysis = DocumentService.Analyze(xml);
        var imported = await DocumentService.ImportAsync(xml, definitionId: null, name: null, processId: null, CancellationToken.None);

        await Assert.That(imported.Analysis.ProcessIds).IsEquivalentTo(analysis.ProcessIds, CollectionOrdering.Matching);
        await Assert.That(imported.Analysis.Issues.Select(issue => issue.Message))
            .IsEquivalentTo(analysis.Issues.Select(issue => issue.Message), CollectionOrdering.Matching);
    }

    [Test]
    [DisplayName("Importing a Camunda file binds the unbound task and persists a workflow definition")]
    public async Task Import_BindsAndPersists()
    {
        var xml = ReadAsset("camunda-order-process.bpmn");

        var result = await DocumentService.ImportAsync(xml, definitionId: null, name: null, processId: null, CancellationToken.None);

        await Assert.That(result.ImportResult.Succeeded).IsTrue()
            .Because(string.Join("; ", result.ImportResult.ValidationErrors.Select(error => error.Message)));
        await Assert.That(result.ImportResult.WorkflowDefinition.DefinitionId).IsNotEmpty();
        await Assert.That(result.Analysis.Issues).Contains(issue => issue.ElementId == "NotifyWarehouse" && issue.Severity == BpmnImportIssueSeverity.Info);
    }

    [Test]
    [DisplayName("A non-interrupting error event subprocess is dropped at import, and the rest of the document still imports")]
    public async Task Import_DropsANonInterruptingErrorEventSubprocess()
    {
        // A refusal, not a bug, and not a failed import: error events are always interrupting per BPMN, so the reader
        // reports the whole <subProcess> as Dropped and reads the rest of the document as written.
        //
        // The import succeeding is what proves the drop was total. The dropped body declares an undeclared
        // <serviceTask>, and an unbound task the binder can see is refused outright -- so a drop that reported the
        // element but left its bindings behind would surface here as a BpmnBindingException naming 'HandleError',
        // not as a quietly half-imported process.
        var xml = ReadAsset("non-interrupting-error-event-subprocess.bpmn");

        var analysis = DocumentService.Analyze(xml);

        var dropped = (await Assert.That(analysis.Issues).HasSingleItem(candidate => candidate.Severity == BpmnImportIssueSeverity.Dropped))!;

        await Assert.That(dropped.ElementId).IsEqualTo("OnError");
        await Assert.That(dropped.Message).Contains("non-interrupting error event subprocess", StringComparison.CurrentCulture);

        // The reader's own findings about the dropped body's elements survive the drop, at Info: the body is read
        // before the rule that drops the element around it is applied. Pinned so a future reader meets it as the
        // library's behaviour rather than as evidence the drop was partial -- what proves it was total is the import.
        await Assert.That(analysis.Issues).Contains(candidate => candidate.ElementId == "HandleError" && candidate.Severity == BpmnImportIssueSeverity.Info);

        var imported = await DocumentService.ImportAsync(xml, definitionId: null, name: null, processId: null, CancellationToken.None);

        await Assert.That(imported.ImportResult.Succeeded).IsTrue()
            .Because(string.Join("; ", imported.ImportResult.ValidationErrors.Select(error => error.Message)));
    }

    [Test]
    [DisplayName("Exporting an imported definition retains foreign extension elements, foreign attributes and waypoints byte-identically")]
    public async Task Export_RetainsForeignContentAndWaypoints()
    {
        var sourceXml = ReadAsset("camunda-order-process.bpmn");
        var imported = await DocumentService.ImportAsync(sourceXml, definitionId: null, name: null, processId: null, CancellationToken.None);
        await Assert.That(imported.ImportResult.Succeeded).IsTrue();

        var definitionId = imported.ImportResult.WorkflowDefinition.DefinitionId;
        var filter = WorkflowDefinitionHandle.ByDefinitionId(definitionId, VersionOptions.Latest).ToFilter();
        var stored = await DefinitionStore.FindAsync(filter);

        await Assert.That(stored).IsNotNull();
        await Assert.That(stored!.CustomProperties.TryGetValue<string>(BpmnInterchangeDocumentService.SourceXmlCustomPropertyKey, out var storedXml)).IsTrue();
        await Assert.That(storedXml).IsEqualTo(sourceXml);

        var exportedBytes = DocumentService.Export(storedXml!);
        var exportedXml = System.Text.Encoding.UTF8.GetString(exportedBytes);

        var sourceDocument = XDocument.Parse(sourceXml);
        var exportedDocument = XDocument.Parse(exportedXml);

        // Whole-file byte identity is explicitly not what the library guarantees: attribute order, whitespace and
        // namespace prefixes are documented cuts. What survives is asserted piece by piece instead.
        await Assert.That(exportedXml).IsNotEqualTo(sourceXml);

        await AssertForeignAttributesRetained(sourceDocument, exportedDocument);
        await AssertForeignExtensionElementsRetained(sourceDocument, exportedDocument);
        await AssertWaypointsRetained(sourceDocument, exportedDocument);
    }

    private static async Task AssertForeignAttributesRetained(XDocument source, XDocument exported)
    {
        var sourceProcess = source.Descendants(Bpmn + "process").Single();
        var exportedProcess = exported.Descendants(Bpmn + "process").Single();
        await Assert.That(exportedProcess.Attribute(Camunda + "versionTag")?.Value).IsEqualTo(sourceProcess.Attribute(Camunda + "versionTag")?.Value);

        var sourceTask = source.Descendants(Bpmn + "serviceTask").Single();
        var exportedTask = exported.Descendants(Bpmn + "serviceTask").Single();
        await Assert.That(sourceTask.Attribute(Camunda + "asyncBefore")?.Value).IsEqualTo("true");
        await Assert.That(exportedTask.Attribute(Camunda + "asyncBefore")?.Value).IsEqualTo(sourceTask.Attribute(Camunda + "asyncBefore")?.Value);
    }

    private static async Task AssertForeignExtensionElementsRetained(XDocument source, XDocument exported)
    {
        var sourceProperty = source.Descendants(Camunda + "property").Single();
        var exportedProperty = exported.Descendants(Camunda + "property").Single();
        await Assert.That(sourceProperty.Attribute("name")?.Value).IsEqualTo("owner");
        await Assert.That(exportedProperty.Attribute("name")?.Value).IsEqualTo(sourceProperty.Attribute("name")?.Value);
        await Assert.That(exportedProperty.Attribute("value")?.Value).IsEqualTo(sourceProperty.Attribute("value")?.Value);

        var sourceInputParameter = source.Descendants(Camunda + "inputParameter").Single();
        var exportedInputParameter = exported.Descendants(Camunda + "inputParameter").Single();
        await Assert.That(exportedInputParameter.Value).IsEqualTo(sourceInputParameter.Value);

        // The elsa: activity binding is foreign to Bpmn.Interchange itself (its own vendor namespace defaults to
        // vw:), so it round-trips as opaque retained content exactly like the camunda: extensions above.
        var sourceBinding = source.Descendants(Elsa + "activityBinding").Single();
        var exportedBinding = exported.Descendants(Elsa + "activityBinding").Single();
        await Assert.That(sourceBinding.Attribute("activityType")?.Value).IsEqualTo("Elsa.WriteLine");
        await Assert.That(exportedBinding.Attribute("activityType")?.Value).IsEqualTo(sourceBinding.Attribute("activityType")?.Value);
        await Assert.That(exportedBinding.Descendants(Elsa + "input").Single().Value).IsEqualTo(sourceBinding.Descendants(Elsa + "input").Single().Value);

        var sourceDocumentation = source.Descendants(Bpmn + "documentation").Single();
        var exportedDocumentation = exported.Descendants(Bpmn + "documentation").Single();
        await Assert.That(exportedDocumentation.Value).IsEqualTo(sourceDocumentation.Value);
    }

    private static async Task AssertWaypointsRetained(XDocument source, XDocument exported)
    {
        var sourceWaypoints = source.Descendants(Di + "waypoint").Select(WaypointOf).ToList();
        var exportedWaypoints = exported.Descendants(Di + "waypoint").Select(WaypointOf).ToList();

        await Assert.That(sourceWaypoints.Count).IsEqualTo(4);
        await Assert.That(exportedWaypoints).IsEquivalentTo(sourceWaypoints, CollectionOrdering.Matching);

        var sourceBounds = source.Descendants(Dc + "Bounds").Select(BoundsOf).ToList();
        var exportedBounds = exported.Descendants(Dc + "Bounds").Select(BoundsOf).ToList();
        await Assert.That(exportedBounds).IsEquivalentTo(sourceBounds, CollectionOrdering.Matching);
    }

    private static (string X, string Y) WaypointOf(XElement element) => (element.Attribute("x")!.Value, element.Attribute("y")!.Value);

    private static (string X, string Y, string Width, string Height) BoundsOf(XElement element) =>
        (element.Attribute("x")!.Value, element.Attribute("y")!.Value, element.Attribute("width")!.Value, element.Attribute("height")!.Value);
}
