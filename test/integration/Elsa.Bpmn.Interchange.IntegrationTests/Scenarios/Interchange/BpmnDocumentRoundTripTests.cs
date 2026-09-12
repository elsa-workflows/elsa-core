using System.Text;
using System.Xml.Linq;
using Bpmn.Model;
using Elsa.Bpmn.Interchange.Binding;
using Elsa.Bpmn.Interchange.Exceptions;
using Elsa.Bpmn.Interchange.IntegrationTests.Scenarios.Binding;
using Elsa.Bpmn.Interchange.IntegrationTests.Support;
using Elsa.Bpmn.Interchange.Services;
using Elsa.Common.Models;
using Elsa.Extensions;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Models;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;

namespace Elsa.Bpmn.Interchange.IntegrationTests.Scenarios.Interchange;

/// <summary>
/// The round trip W21 exists for: <see cref="BpmnInterchangeDocumentService.ReadDocument"/> and
/// <see cref="BpmnInterchangeDocumentService.ImportDocumentAsync"/> — the document endpoints' shared service path —
/// let a document round-trip through JSON without losing what <c>Export</c> already proves survives XML alone.
/// </summary>
/// <remarks>
/// Derives from <see cref="BpmnBindingTestBase"/>, not <see cref="BpmnInterchangeTestBase"/>, because building the
/// edit in the second test needs <see cref="BpmnActivityBindingFormat"/> to write a fresh <c>elsa:activityBinding</c>,
/// exactly as <see cref="Scenarios.Publishing.BpmnPublishGateTestBase"/> already does for the same reason.
/// </remarks>
public class BpmnDocumentRoundTripTests : BpmnBindingTestBase
{
    private static readonly XNamespace Camunda = BpmnXNamespaces.Camunda;
    private static readonly XNamespace Elsa = BpmnXNamespaces.Elsa;
    private static readonly XNamespace Di = BpmnXNamespaces.Di;
    private static readonly XNamespace Bpmn = BpmnXNamespaces.Bpmn;

    public BpmnDocumentRoundTripTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
    {
        DocumentService = Services.GetRequiredService<BpmnInterchangeDocumentService>();
        DefinitionStore = Services.GetRequiredService<IWorkflowDefinitionStore>();
    }

    private BpmnInterchangeDocumentService DocumentService { get; }

    private IWorkflowDefinitionStore DefinitionStore { get; }

    [Fact(DisplayName = "Reading a document then posting it back unchanged exports content-equal to the original")]
    public async Task ReadDocument_ThenImportDocumentAsyncUnchanged_ExportsContentEqualToTheOriginal()
    {
        var xml = ReadAsset("camunda-order-process.bpmn");
        var imported = await DocumentService.ImportAsync(xml, definitionId: null, name: null, processId: null, CancellationToken.None);
        Assert.True(imported.ImportResult.Succeeded, string.Join("; ", imported.ImportResult.ValidationErrors.Select(error => error.Message)));

        var definitionId = imported.ImportResult.WorkflowDefinition.DefinitionId;
        var beforeStored = await FindLatestAsync(definitionId);
        var originalExportedXml = Encoding.UTF8.GetString(DocumentService.Export(beforeStored));

        var document = DocumentService.ReadDocument(beforeStored);
        var processId = ProcessIdOf(beforeStored);

        var putResult = await DocumentService.ImportDocumentAsync(document, definitionId, processId, CancellationToken.None);
        Assert.True(putResult.ImportResult.Succeeded, string.Join("; ", putResult.ImportResult.ValidationErrors.Select(error => error.Message)));

        var afterStored = await FindLatestAsync(definitionId);
        var roundTrippedExportedXml = Encoding.UTF8.GetString(DocumentService.Export(afterStored));

        AssertContentEquivalent(XDocument.Parse(originalExportedXml), XDocument.Parse(roundTrippedExportedXml));
    }

    [Fact(DisplayName = "Posting a document back with a new bound task added shows the binding on export and leaves everything else unchanged")]
    public async Task ImportDocumentAsync_WithANewBoundTaskAdded_ExportsTheAdditionAndLeavesTheRestUnchanged()
    {
        var xml = ReadAsset("camunda-order-process.bpmn");
        var imported = await DocumentService.ImportAsync(xml, definitionId: null, name: null, processId: null, CancellationToken.None);
        Assert.True(imported.ImportResult.Succeeded, string.Join("; ", imported.ImportResult.ValidationErrors.Select(error => error.Message)));

        var definitionId = imported.ImportResult.WorkflowDefinition.DefinitionId;
        var stored = await FindLatestAsync(definitionId);
        var originalExportedXml = Encoding.UTF8.GetString(DocumentService.Export(stored));

        var document = DocumentService.ReadDocument(stored);
        var process = Assert.Single(document.Processes);

        const string newTaskId = "ArchiveOrder";
        var binding = Format.Write(new WriteLine("Archiving the order"));
        var newTask = new BpmnElement(newTaskId, BpmnElementTypes.ServiceTask, name: "Archive Order", extensions: BpmnActivityBindingFormat.Attach(null, binding));

        var editedProcess = process with { Elements = process.Elements.Append(newTask).ToList() };
        var editedDocument = document with { Processes = [editedProcess] };

        var processId = ProcessIdOf(stored);

        var putResult = await DocumentService.ImportDocumentAsync(editedDocument, definitionId, processId, CancellationToken.None);
        Assert.True(putResult.ImportResult.Succeeded, string.Join("; ", putResult.ImportResult.ValidationErrors.Select(error => error.Message)));

        var updated = await FindLatestAsync(definitionId);
        var updatedExportedXml = Encoding.UTF8.GetString(DocumentService.Export(updated));

        var originalDocument = XDocument.Parse(originalExportedXml);
        var updatedDocument = XDocument.Parse(updatedExportedXml);

        // Everything that was there before the edit is still there, unchanged.
        AssertContentEquivalent(originalDocument, updatedDocument);

        // The addition shows up.
        var addedTask = updatedDocument.Descendants(Bpmn + "serviceTask").Single(element => element.Attribute("id")?.Value == newTaskId);
        var addedBinding = addedTask.Descendants(Elsa + "activityBinding").Single();
        Assert.Equal("Elsa.WriteLine", addedBinding.Attribute("activityType")?.Value);
        Assert.Contains("Archiving the order", addedBinding.Descendants(Elsa + "input").Single().Value);
    }

    [Fact(DisplayName = "Importing a document against a definition id that does not exist refuses rather than creating one")]
    public async Task ImportDocumentAsync_WhenTheDefinitionDoesNotExist_ThrowsAndCreatesNothing()
    {
        var xml = ReadAsset("camunda-order-process.bpmn");
        var imported = await DocumentService.ImportAsync(xml, definitionId: null, name: null, processId: null, CancellationToken.None);
        Assert.True(imported.ImportResult.Succeeded, string.Join("; ", imported.ImportResult.ValidationErrors.Select(error => error.Message)));

        var stored = await FindLatestAsync(imported.ImportResult.WorkflowDefinition.DefinitionId);
        var document = DocumentService.ReadDocument(stored);

        var missingDefinitionId = $"{Guid.NewGuid()}-does-not-exist";

        await Assert.ThrowsAsync<BpmnDefinitionNotFoundException>(() =>
            DocumentService.ImportDocumentAsync(document, missingDefinitionId, processId: null, CancellationToken.None));

        var filter = WorkflowDefinitionHandle.ByDefinitionId(missingDefinitionId, VersionOptions.Latest).ToFilter();
        var afterAttempt = await DefinitionStore.FindAsync(filter);
        Assert.Null(afterAttempt);
    }

    /// <summary>Everything <c>camunda-order-process.bpmn</c> carries that an edit must not disturb: foreign attributes, foreign extension elements, the elsa: binding on the untouched task, and BPMN DI waypoints.</summary>
    private static void AssertContentEquivalent(XDocument expected, XDocument actual)
    {
        var expectedProcess = expected.Descendants(Bpmn + "process").Single();
        var actualProcess = actual.Descendants(Bpmn + "process").Single();
        Assert.Equal(expectedProcess.Attribute("id")?.Value, actualProcess.Attribute("id")?.Value);
        Assert.Equal("order-process", expectedProcess.Attribute("id")?.Value);
        Assert.Equal(expectedProcess.Attribute(Camunda + "versionTag")?.Value, actualProcess.Attribute(Camunda + "versionTag")?.Value);

        var expectedTask = expected.Descendants(Bpmn + "serviceTask").Single(element => element.Attribute("id")?.Value == "NotifyWarehouse");
        var actualTask = actual.Descendants(Bpmn + "serviceTask").Single(element => element.Attribute("id")?.Value == "NotifyWarehouse");
        Assert.Equal("true", expectedTask.Attribute(Camunda + "asyncBefore")?.Value);
        Assert.Equal(expectedTask.Attribute(Camunda + "asyncBefore")?.Value, actualTask.Attribute(Camunda + "asyncBefore")?.Value);
        Assert.Equal(expectedTask.Descendants(Bpmn + "documentation").Single().Value, actualTask.Descendants(Bpmn + "documentation").Single().Value);

        var expectedBinding = expectedTask.Descendants(Elsa + "activityBinding").Single();
        var actualBinding = actualTask.Descendants(Elsa + "activityBinding").Single();
        Assert.Equal("Elsa.WriteLine", expectedBinding.Attribute("activityType")?.Value);
        Assert.Equal(expectedBinding.Attribute("activityType")?.Value, actualBinding.Attribute("activityType")?.Value);
        Assert.Equal(expectedBinding.Descendants(Elsa + "input").Single().Value, actualBinding.Descendants(Elsa + "input").Single().Value);

        var expectedProperty = expected.Descendants(Camunda + "property").Single();
        var actualProperty = actual.Descendants(Camunda + "property").Single();
        Assert.Equal("owner", expectedProperty.Attribute("name")?.Value);
        Assert.Equal(expectedProperty.Attribute("name")?.Value, actualProperty.Attribute("name")?.Value);
        Assert.Equal(expectedProperty.Attribute("value")?.Value, actualProperty.Attribute("value")?.Value);

        var expectedInputParameter = expected.Descendants(Camunda + "inputParameter").Single();
        var actualInputParameter = actual.Descendants(Camunda + "inputParameter").Single();
        Assert.Equal(expectedInputParameter.Value, actualInputParameter.Value);

        var expectedWaypoints = expected.Descendants(Di + "waypoint").Select(WaypointOf).ToList();
        var actualWaypoints = actual.Descendants(Di + "waypoint").Select(WaypointOf).ToList();
        Assert.Equal(4, expectedWaypoints.Count);
        Assert.Equal(expectedWaypoints, actualWaypoints);
    }

    private static (string X, string Y) WaypointOf(XElement element) => (element.Attribute("x")!.Value, element.Attribute("y")!.Value);

    /// <summary>The <c>processId</c> a document endpoint would reuse, mirroring how <c>Put</c> resolves it from the stored definition.</summary>
    private static string? ProcessIdOf(WorkflowDefinition definition) =>
        definition.CustomProperties.TryGetValue<string>(BpmnInterchangeDocumentService.SourceProcessIdCustomPropertyKey, out var processId) ? processId : null;

    private async Task<WorkflowDefinition> FindLatestAsync(string definitionId)
    {
        var filter = WorkflowDefinitionHandle.ByDefinitionId(definitionId, VersionOptions.Latest).ToFilter();
        var definition = await DefinitionStore.FindAsync(filter);
        Assert.NotNull(definition);
        return definition!;
    }

    /// <summary>Reads a fixture from the <c>Assets</c> directory shipped alongside this test project.</summary>
    private static string ReadAsset(string fileName) => BpmnAssetReader.Read(fileName);
}
