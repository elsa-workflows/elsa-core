using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Xml.Linq;
using Bpmn.Interchange;
using Bpmn.Model;
using Elsa.Bpmn.Interchange.Binding;
using Elsa.Bpmn.Interchange.Endpoints.Bpmn;
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
/// let a document round-trip through JSON without losing what <c>Export</c> already proves survives XML alone,
/// including the bodies of nested scopes, which the JSON document cannot carry and the stored source supplies.
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

    [Theory(DisplayName = "Reading a document then posting it back unchanged exports content-equal to the original, nested scopes included")]
    [InlineData("camunda-order-process.bpmn")]
    [InlineData("subprocess-boundary-events.bpmn")]
    [InlineData("transaction-compensation.bpmn")]
    [InlineData("nested-subprocesses.bpmn")]
    [InlineData("camunda-multi-instance-subprocess.bpmn")]
    public async Task ReadDocument_ThenImportDocumentAsyncUnchanged_ExportsContentEqualToTheOriginal(string assetFileName)
    {
        var stored = await ImportAssetAsync(assetFileName);
        var originalExport = ExportOf(stored);

        var roundTripped = await PutAsync(stored, ThroughTheDocumentEndpoints(DocumentService.ReadDocument(stored)));

        // Every element the fixture nests inside a subprocess or transaction is still there, in the same scope, so the
        // comparison below cannot pass by comparing two equally emptied documents...
        Assert.Equal(NestedElementIdsOf(XDocument.Parse(ReadAsset(assetFileName))), NestedElementIdsOf(roundTripped));

        // ...and nothing else changed either: retained extensions, elsa: bindings, a fire-and-forget call's
        // vw:waitForCompletion (carried only by its work binding), a multi-instance marker the reader could not interpret
        // (kept only as retained content) and BPMN DI included.
        Assert.Equal(ComparableContentOf(originalExport.Root!), ComparableContentOf(roundTripped.Root!));
    }

    [Fact(DisplayName = "Posting a document back with a new bound task added shows the binding on export and leaves everything else unchanged")]
    public async Task ImportDocumentAsync_WithANewBoundTaskAdded_ExportsTheAdditionAndLeavesTheRestUnchanged()
    {
        var stored = await ImportAssetAsync("camunda-order-process.bpmn");
        var originalDocument = ExportOf(stored);

        var document = DocumentService.ReadDocument(stored);
        var process = Assert.Single(document.Processes);

        const string newTaskId = "ArchiveOrder";
        var binding = Format.Write(new WriteLine("Archiving the order"));
        var newTask = new BpmnElement(newTaskId, BpmnElementTypes.ServiceTask, name: "Archive Order", extensions: BpmnActivityBindingFormat.Attach(null, binding));

        var editedProcess = process with { Elements = process.Elements.Append(newTask).ToList() };
        var editedDocument = document with { Processes = [editedProcess] };

        var updatedDocument = await PutAsync(stored, editedDocument);

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
        var stored = await ImportAssetAsync("camunda-order-process.bpmn");
        var document = DocumentService.ReadDocument(stored);

        var missingDefinitionId = $"{Guid.NewGuid()}-does-not-exist";

        await Assert.ThrowsAsync<BpmnDefinitionNotFoundException>(() =>
            DocumentService.ImportDocumentAsync(document, missingDefinitionId, processId: null, CancellationToken.None));

        var filter = WorkflowDefinitionHandle.ByDefinitionId(missingDefinitionId, VersionOptions.Latest).ToFilter();
        var afterAttempt = await DefinitionStore.FindAsync(filter);
        Assert.Null(afterAttempt);
    }

    [Fact(DisplayName = "Posting a document back without a subprocess drops its stored body, even under a new subprocess reusing its bindingRef, and keeps every other body")]
    public async Task ImportDocumentAsync_WithASubprocessRemoved_DropsItsStoredBodyAndKeepsTheOthers()
    {
        // Replaces the event subprocess with a new, empty embedded subprocess that took over its bindingRef: what a
        // client cloning the element's JSON and changing its id posts.
        var updated = await PutWithOnRecallEditedAsync(json =>
        {
            var onRecall = ElementOf(json, "OnRecall");
            var elements = onRecall.Parent!.AsArray();
            elements.Remove(onRecall);
            elements.Add(new JsonObject { ["elementId"] = "Archive", ["elementType"] = BpmnElementTypes.SubProcess, ["bindingRef"] = onRecall["bindingRef"]!.DeepClone() });
        });

        Assert.DoesNotContain(NestedScopesOf(updated), scope => scope.Attribute("id")?.Value == "OnRecall");
        Assert.DoesNotContain(ScopeOf(updated, "Archive").Elements(), child => child.Attribute("id") is not null);
    }

    [Fact(DisplayName = "Posting a document back with a subprocess turned into another kind of element drops its stored body")]
    public async Task ImportDocumentAsync_WithASubprocessTurnedIntoAnotherKindOfElement_DropsItsStoredBody()
    {
        var updated = await PutWithOnRecallEditedAsync(json =>
            ElementOf(json, "OnRecall").ReplaceWith(new JsonObject { ["elementId"] = "OnRecall", ["elementType"] = BpmnElementTypes.EndEvent }));

        Assert.Single(updated.Descendants(Bpmn + "endEvent"), element => element.Attribute("id")?.Value == "OnRecall");
    }

    [Theory(DisplayName = "Posting a document back with a kept subprocess's multi-instance marker changed, removed or added writes exactly the posted marker")]
    [InlineData("subprocess-boundary-events.bpmn", "Fulfil", """{"isSequential":false,"cardinality":null,"collectionVariable":"orderLines","itemVariable":"lineItem"}""")]
    [InlineData("subprocess-boundary-events.bpmn", "Fulfil", null)]
    [InlineData("camunda-multi-instance-subprocess.bpmn", "ShipOrders", """{"isSequential":true,"cardinality":3,"collectionVariable":null,"itemVariable":"item"}""")]
    public async Task ImportDocumentAsync_WithAKeptSubprocessLoopMarkerEdited_WritesExactlyThePostedMarker(string assetFileName, string subprocessId, string? loopCharacteristics)
    {
        var stored = await ImportAssetAsync(assetFileName);
        var document = ThroughTheDocumentEndpoints(DocumentService.ReadDocument(stored), json =>
            ElementOf(json, subprocessId)["loopCharacteristics"] = loopCharacteristics is null ? null : JsonNode.Parse(loopCharacteristics));

        await PutAsync(stored, document);

        // What the next GET hands back is the posted marker, not the copy of the stored one the library also keeps in
        // the subprocess's body, which would otherwise be written back ahead of it and win the next read.
        var reread = DocumentService.ReadDocument(await FindLatestAsync(stored.DefinitionId)).Processes.Single().Elements.Single(element => element.ElementId == subprocessId);
        Assert.Equal(loopCharacteristics ?? "null", JsonSerializer.Serialize(reread.LoopCharacteristics, BpmnDocumentJsonOptions.Value));
    }

    [Fact(DisplayName = "Posting a document back with a kept subprocess under another bindingRef still writes its stored body")]
    public async Task ImportDocumentAsync_WhenAKeptSubprocessCarriesAnotherBindingRef_StillWritesItsStoredBody()
    {
        var stored = await ImportAssetAsync("transaction-compensation.bpmn");
        var originalExport = ExportOf(stored);

        var updated = await PutAsync(stored, WithBindingRef(DocumentService.ReadDocument(stored), "BookTrip", "renamed-by-the-client"));

        Assert.Equal(ComparableContentOf(ScopeOf(originalExport, "BookTrip")), ComparableContentOf(ScopeOf(updated, "BookTrip")));
    }

    [Fact(DisplayName = "Posting a document back with a kept subprocess stripped of its bindingRef refuses rather than emptying it")]
    public async Task ImportDocumentAsync_WhenAKeptSubprocessCarriesNoBindingRef_RefusesAndPersistsNothing()
    {
        var stored = await ImportAssetAsync("transaction-compensation.bpmn");
        var storedBefore = (SourceXmlOf(stored), stored.StringData);
        var document = WithBindingRef(DocumentService.ReadDocument(stored), "BookTrip", null);

        var exception = await Assert.ThrowsAsync<BpmnInterchangeException>(() =>
            DocumentService.ImportDocumentAsync(document, stored.DefinitionId, ProcessIdOf(stored), CancellationToken.None));

        Assert.Contains("'BookTrip'", exception.Message);
        var afterAttempt = await FindLatestAsync(stored.DefinitionId);
        Assert.Equal(storedBefore, (SourceXmlOf(afterAttempt), afterAttempt.StringData));
    }

    /// <summary>
    /// Imports <c>subprocess-boundary-events.bpmn</c>, posts its document back with <paramref name="edit"/> applied to the
    /// JSON, and asserts that nothing the <c>OnRecall</c> event subprocess's stored body held is written back anywhere
    /// while the <c>Fulfil</c> subprocess the edit left alone keeps its body exactly as stored. Returns the export.
    /// </summary>
    private async Task<XDocument> PutWithOnRecallEditedAsync(Action<JsonNode> edit)
    {
        var stored = await ImportAssetAsync("subprocess-boundary-events.bpmn");
        var originalExport = ExportOf(stored);
        var onRecallBodyIds = ScopeOf(originalExport, "OnRecall").Descendants().Select(element => element.Attribute("id")?.Value).OfType<string>().ToList();
        Assert.Contains("HandleRecall", onRecallBodyIds);

        var updated = await PutAsync(stored, ThroughTheDocumentEndpoints(DocumentService.ReadDocument(stored), edit));

        Assert.DoesNotContain(updated.Descendants(Bpmn + "process").Single().Descendants(), element => element.Attribute("id")?.Value is { } id && onRecallBodyIds.Contains(id));
        Assert.Equal(ComparableContentOf(ScopeOf(originalExport, "Fulfil")), ComparableContentOf(ScopeOf(updated, "Fulfil")));
        return updated;
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

    private static string? SourceXmlOf(WorkflowDefinition definition) =>
        definition.CustomProperties.TryGetValue<string>(BpmnInterchangeDocumentService.SourceXmlCustomPropertyKey, out var xml) ? xml : null;

    private async Task<WorkflowDefinition> FindLatestAsync(string definitionId)
    {
        var filter = WorkflowDefinitionHandle.ByDefinitionId(definitionId, VersionOptions.Latest).ToFilter();
        var definition = await DefinitionStore.FindAsync(filter);
        Assert.NotNull(definition);
        return definition!;
    }

    /// <summary>Imports a fixture as a new definition, the way <c>Import</c> does, and returns it as stored.</summary>
    private async Task<WorkflowDefinition> ImportAssetAsync(string assetFileName)
    {
        var imported = await DocumentService.ImportAsync(ReadAsset(assetFileName), definitionId: null, name: null, processId: null, CancellationToken.None);
        Assert.True(imported.ImportResult.Succeeded, string.Join("; ", imported.ImportResult.ValidationErrors.Select(error => error.Message)));
        return await FindLatestAsync(imported.ImportResult.WorkflowDefinition.DefinitionId);
    }

    /// <summary>Posts <paramref name="document"/> back onto <paramref name="stored"/> the way the document <c>PUT</c> does, and returns what exporting the result gives.</summary>
    private async Task<XDocument> PutAsync(WorkflowDefinition stored, BpmnDefinitions document)
    {
        var putResult = await DocumentService.ImportDocumentAsync(document, stored.DefinitionId, ProcessIdOf(stored), CancellationToken.None);
        Assert.True(putResult.ImportResult.Succeeded, string.Join("; ", putResult.ImportResult.ValidationErrors.Select(error => error.Message)));
        return ExportOf(await FindLatestAsync(stored.DefinitionId));
    }

    private XDocument ExportOf(WorkflowDefinition definition) => XDocument.Parse(Encoding.UTF8.GetString(DocumentService.Export(definition)));

    /// <summary>
    /// <paramref name="document"/> as a client of the document endpoints holds it: serialized the way <c>GET</c> writes
    /// it, optionally edited as JSON, and deserialized the way <c>PUT</c> reads it.
    /// </summary>
    private static BpmnDefinitions ThroughTheDocumentEndpoints(BpmnDefinitions document, Action<JsonNode>? edit = null)
    {
        var json = JsonNode.Parse(JsonSerializer.Serialize(document, BpmnDocumentJsonOptions.Value))!;
        edit?.Invoke(json);
        return json.Deserialize<BpmnDefinitions>(BpmnDocumentJsonOptions.Value)!;
    }

    private static BpmnDefinitions WithBindingRef(BpmnDefinitions document, string elementId, string? bindingRef) =>
        ThroughTheDocumentEndpoints(document, json => ElementOf(json, elementId)["bindingRef"] = bindingRef);

    private static JsonNode ElementOf(JsonNode document, string elementId) =>
        document["processes"]!.AsArray().SelectMany(process => process!["elements"]!.AsArray()).Single(element => element!["elementId"]!.GetValue<string>() == elementId)!;

    /// <summary>The <c>subProcess</c> or <c>transaction</c> element with the given id.</summary>
    private static XElement ScopeOf(XDocument document, string id) =>
        NestedScopesOf(document).Single(scope => scope.Attribute("id")?.Value == id);

    private static IEnumerable<XElement> NestedScopesOf(XDocument document) => document.Descendants().Where(IsNestedScope);

    private static bool IsNestedScope(XElement element) => element.Name == Bpmn + "subProcess" || element.Name == Bpmn + "transaction";

    /// <summary>
    /// <paramref name="element"/> as text, with each nested scope's <c>multiInstanceLoopCharacteristics</c> reduced to the
    /// distinct markers it carries and moved last. Bpmn.Interchange 0.2.0 both reads a multi-instance subprocess's marker
    /// onto its element and retains a copy as foreign content of the nested process it opens, so <c>Export</c>, which
    /// writes exactly what it read, emits the marker twice: once from the element, once from the retained copy, at the
    /// position the stored document had it. That position moves when a document <c>PUT</c> rewrites the stored document,
    /// even though the marker does not. Comparing the distinct markers still fails on a marker lost or changed.
    /// </summary>
    private static string ComparableContentOf(XElement element)
    {
        var comparable = new XElement(element);

        foreach (var scope in comparable.DescendantsAndSelf().Where(IsNestedScope).ToList())
        {
            var markers = scope.Elements(Bpmn + "multiInstanceLoopCharacteristics").ToList();
            markers.Remove();
            scope.Add(markers.DistinctBy(marker => marker.ToString()));
        }

        return comparable.ToString();
    }

    /// <summary>
    /// Every flow element and sequence flow the document nests inside a subprocess or transaction, as
    /// <c>scope/kind/id</c>. An association is left out: the model records it only as the compensation boundary event's
    /// handler, so the writer derives its id rather than keeping the source's.
    /// </summary>
    private static IReadOnlyList<string> NestedElementIdsOf(XDocument document) =>
        NestedScopesOf(document)
            .SelectMany(scope => scope.Elements()
                .Where(child => child.Name != Bpmn + "association" && child.Attribute("id") is not null)
                .Select(child => $"{scope.Attribute("id")!.Value}/{child.Name.LocalName}/{child.Attribute("id")!.Value}"))
            .Order(StringComparer.Ordinal)
            .ToList();

    /// <summary>Reads a fixture from the <c>Assets</c> directory shipped alongside this test project.</summary>
    private static string ReadAsset(string fileName) => BpmnAssetReader.Read(fileName);
}
