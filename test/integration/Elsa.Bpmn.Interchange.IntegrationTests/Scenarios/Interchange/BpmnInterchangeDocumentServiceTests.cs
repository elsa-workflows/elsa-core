using System.Xml.Linq;
using Bpmn.Interchange;
using Elsa.Bpmn.Interchange.Services;
using Elsa.Common.Models;
using Elsa.Extensions;
using Elsa.Workflows.Management;
using Elsa.Workflows.Models;
using Xunit.Abstractions;

namespace Elsa.Bpmn.Interchange.IntegrationTests.Scenarios.Interchange;

/// <summary>
/// The round trip the Analyze, Import and Export endpoints exist for: a Camunda-authored <c>.bpmn</c> file survives
/// import and export with its foreign extension elements, foreign attributes and BPMN DI waypoints intact.
/// </summary>
public class BpmnInterchangeDocumentServiceTests(ITestOutputHelper testOutputHelper) : BpmnInterchangeTestBase(testOutputHelper)
{
    private static readonly XNamespace Camunda = "http://camunda.org/schema/1.0/bpmn";
    private static readonly XNamespace Elsa = "https://elsaworkflows.io/schemas/bpmn/v1";
    private static readonly XNamespace Dc = "http://www.omg.org/spec/DD/20100524/DC";
    private static readonly XNamespace Di = "http://www.omg.org/spec/DD/20100524/DI";
    private static readonly XNamespace Bpmn = "http://www.omg.org/spec/BPMN/20100524/MODEL";

    [Fact(DisplayName = "Analyze and Import report the same findings for the same document")]
    public async Task Analyze_AndImport_ReportTheSameFindings()
    {
        var xml = ReadAsset("camunda-order-process.bpmn");

        var analysis = DocumentService.Analyze(xml);
        var imported = await DocumentService.ImportAsync(xml, definitionId: null, name: null, processId: null, CancellationToken.None);

        Assert.Equal(analysis.ProcessIds, imported.Analysis.ProcessIds);
        Assert.Equal(analysis.Issues.Select(issue => issue.Message), imported.Analysis.Issues.Select(issue => issue.Message));
    }

    [Fact(DisplayName = "Importing a Camunda file binds the unbound task and persists a workflow definition")]
    public async Task Import_BindsAndPersists()
    {
        var xml = ReadAsset("camunda-order-process.bpmn");

        var result = await DocumentService.ImportAsync(xml, definitionId: null, name: null, processId: null, CancellationToken.None);

        Assert.True(result.ImportResult.Succeeded, string.Join("; ", result.ImportResult.ValidationErrors.Select(error => error.Message)));
        Assert.NotEmpty(result.ImportResult.WorkflowDefinition.DefinitionId);
        Assert.Contains(result.Analysis.Issues, issue => issue.ElementId == "NotifyWarehouse" && issue.Severity == BpmnImportIssueSeverity.Info);
    }

    [Fact(DisplayName = "A non-interrupting error event subprocess is dropped at import, and the rest of the document still imports")]
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

        var dropped = Assert.Single(analysis.Issues, candidate => candidate.Severity == BpmnImportIssueSeverity.Dropped);

        Assert.Equal("OnError", dropped.ElementId);
        Assert.Contains("non-interrupting error event subprocess", dropped.Message);

        // The reader's own findings about the dropped body's elements survive the drop, at Info: the body is read
        // before the rule that drops the element around it is applied. Pinned so a future reader meets it as the
        // library's behaviour rather than as evidence the drop was partial -- what proves it was total is the import.
        Assert.Contains(analysis.Issues, candidate => candidate.ElementId == "HandleError" && candidate.Severity == BpmnImportIssueSeverity.Info);

        var imported = await DocumentService.ImportAsync(xml, definitionId: null, name: null, processId: null, CancellationToken.None);

        Assert.True(imported.ImportResult.Succeeded, string.Join("; ", imported.ImportResult.ValidationErrors.Select(error => error.Message)));
    }

    [Fact(DisplayName = "Exporting an imported definition retains foreign extension elements, foreign attributes and waypoints byte-identically")]
    public async Task Export_RetainsForeignContentAndWaypoints()
    {
        var sourceXml = ReadAsset("camunda-order-process.bpmn");
        var imported = await DocumentService.ImportAsync(sourceXml, definitionId: null, name: null, processId: null, CancellationToken.None);
        Assert.True(imported.ImportResult.Succeeded);

        var definitionId = imported.ImportResult.WorkflowDefinition.DefinitionId;
        var filter = WorkflowDefinitionHandle.ByDefinitionId(definitionId, VersionOptions.Latest).ToFilter();
        var stored = await DefinitionStore.FindAsync(filter);

        Assert.NotNull(stored);
        Assert.True(stored!.CustomProperties.TryGetValue<string>(BpmnInterchangeDocumentService.SourceXmlCustomPropertyKey, out var storedXml));
        Assert.Equal(sourceXml, storedXml);

        var exportedBytes = DocumentService.Export(storedXml!);
        var exportedXml = System.Text.Encoding.UTF8.GetString(exportedBytes);

        var sourceDocument = XDocument.Parse(sourceXml);
        var exportedDocument = XDocument.Parse(exportedXml);

        // Whole-file byte identity is explicitly not what the library guarantees: attribute order, whitespace and
        // namespace prefixes are documented cuts. What survives is asserted piece by piece instead.
        Assert.NotEqual(sourceXml, exportedXml);

        AssertForeignAttributesRetained(sourceDocument, exportedDocument);
        AssertForeignExtensionElementsRetained(sourceDocument, exportedDocument);
        AssertWaypointsRetained(sourceDocument, exportedDocument);
    }

    private static void AssertForeignAttributesRetained(XDocument source, XDocument exported)
    {
        var sourceProcess = source.Descendants(Bpmn + "process").Single();
        var exportedProcess = exported.Descendants(Bpmn + "process").Single();
        Assert.Equal(sourceProcess.Attribute(Camunda + "versionTag")?.Value, exportedProcess.Attribute(Camunda + "versionTag")?.Value);

        var sourceTask = source.Descendants(Bpmn + "serviceTask").Single();
        var exportedTask = exported.Descendants(Bpmn + "serviceTask").Single();
        Assert.Equal("true", sourceTask.Attribute(Camunda + "asyncBefore")?.Value);
        Assert.Equal(sourceTask.Attribute(Camunda + "asyncBefore")?.Value, exportedTask.Attribute(Camunda + "asyncBefore")?.Value);
    }

    private static void AssertForeignExtensionElementsRetained(XDocument source, XDocument exported)
    {
        var sourceProperty = source.Descendants(Camunda + "property").Single();
        var exportedProperty = exported.Descendants(Camunda + "property").Single();
        Assert.Equal("owner", sourceProperty.Attribute("name")?.Value);
        Assert.Equal(sourceProperty.Attribute("name")?.Value, exportedProperty.Attribute("name")?.Value);
        Assert.Equal(sourceProperty.Attribute("value")?.Value, exportedProperty.Attribute("value")?.Value);

        var sourceInputParameter = source.Descendants(Camunda + "inputParameter").Single();
        var exportedInputParameter = exported.Descendants(Camunda + "inputParameter").Single();
        Assert.Equal(sourceInputParameter.Value, exportedInputParameter.Value);

        // The elsa: activity binding is foreign to Bpmn.Interchange itself (its own vendor namespace defaults to
        // vw:), so it round-trips as opaque retained content exactly like the camunda: extensions above.
        var sourceBinding = source.Descendants(Elsa + "activityBinding").Single();
        var exportedBinding = exported.Descendants(Elsa + "activityBinding").Single();
        Assert.Equal("Elsa.WriteLine", sourceBinding.Attribute("activityType")?.Value);
        Assert.Equal(sourceBinding.Attribute("activityType")?.Value, exportedBinding.Attribute("activityType")?.Value);
        Assert.Equal(sourceBinding.Descendants(Elsa + "input").Single().Value, exportedBinding.Descendants(Elsa + "input").Single().Value);

        var sourceDocumentation = source.Descendants(Bpmn + "documentation").Single();
        var exportedDocumentation = exported.Descendants(Bpmn + "documentation").Single();
        Assert.Equal(sourceDocumentation.Value, exportedDocumentation.Value);
    }

    private static void AssertWaypointsRetained(XDocument source, XDocument exported)
    {
        var sourceWaypoints = source.Descendants(Di + "waypoint").Select(WaypointOf).ToList();
        var exportedWaypoints = exported.Descendants(Di + "waypoint").Select(WaypointOf).ToList();

        Assert.Equal(4, sourceWaypoints.Count);
        Assert.Equal(sourceWaypoints, exportedWaypoints);

        var sourceBounds = source.Descendants(Dc + "Bounds").Select(BoundsOf).ToList();
        var exportedBounds = exported.Descendants(Dc + "Bounds").Select(BoundsOf).ToList();
        Assert.Equal(sourceBounds, exportedBounds);
    }

    private static (string X, string Y) WaypointOf(XElement element) => (element.Attribute("x")!.Value, element.Attribute("y")!.Value);

    private static (string X, string Y, string Width, string Height) BoundsOf(XElement element) =>
        (element.Attribute("x")!.Value, element.Attribute("y")!.Value, element.Attribute("width")!.Value, element.Attribute("height")!.Value);
}
