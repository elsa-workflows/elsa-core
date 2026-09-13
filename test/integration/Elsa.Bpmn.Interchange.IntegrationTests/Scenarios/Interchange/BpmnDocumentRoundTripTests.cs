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
using TUnit.Assertions.Enums;

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
    private static readonly XNamespace Vw = BpmnXNamespaces.Vw;

    public BpmnDocumentRoundTripTests()
    {
        DocumentService = Services.GetRequiredService<BpmnInterchangeDocumentService>();
        DefinitionStore = Services.GetRequiredService<IWorkflowDefinitionStore>();
    }

    private BpmnInterchangeDocumentService DocumentService { get; }

    private IWorkflowDefinitionStore DefinitionStore { get; }

    [Test]
    [DisplayName("Reading a document then posting it back unchanged exports content-equal to the original, nested scopes included")]
    [Arguments("camunda-order-process.bpmn")]
    [Arguments("subprocess-boundary-events.bpmn")]
    [Arguments("transaction-compensation.bpmn")]
    [Arguments("nested-subprocesses.bpmn")]
    [Arguments("camunda-multi-instance-subprocess.bpmn")]
    [Arguments("top-level-call-activity.bpmn")]
    public async Task ReadDocument_ThenImportDocumentAsyncUnchanged_ExportsContentEqualToTheOriginal(string assetFileName)
    {
        var stored = await ImportAssetAsync(assetFileName);
        var originalExport = ExportOf(stored);

        var roundTripped = await PutAsync(stored, ThroughTheDocumentEndpoints(DocumentService.ReadDocument(stored)));

        // Every element the fixture nests inside a subprocess or transaction is still there, in the same scope, so the
        // comparison below cannot pass by comparing two equally emptied documents...
        await Assert.That(NestedElementIdsOf(roundTripped))
            .IsEquivalentTo(NestedElementIdsOf(XDocument.Parse(ReadAsset(assetFileName))), CollectionOrdering.Matching);

        // ...and nothing else changed either: retained extensions, elsa: bindings, a fire-and-forget call's
        // vw:waitForCompletion (carried only by its work binding), a multi-instance marker the reader could not interpret
        // (kept only as retained content) and BPMN DI included.
        await Assert.That(ComparableContentOf(roundTripped.Root!)).IsEqualTo(ComparableContentOf(originalExport.Root!));
    }

    [Test]
    [DisplayName("Posting a document back with a new bound task added shows the binding on export and leaves everything else unchanged")]
    public async Task ImportDocumentAsync_WithANewBoundTaskAdded_ExportsTheAdditionAndLeavesTheRestUnchanged()
    {
        var stored = await ImportAssetAsync("camunda-order-process.bpmn");
        var originalDocument = ExportOf(stored);

        var document = DocumentService.ReadDocument(stored);
        var process = (await Assert.That(document.Processes).HasSingleItem())!;

        const string newTaskId = "ArchiveOrder";
        var binding = Format.Write(new WriteLine("Archiving the order"));
        var newTask = new BpmnElement(newTaskId, BpmnElementTypes.ServiceTask, name: "Archive Order", extensions: BpmnActivityBindingFormat.Attach(null, binding));

        var editedProcess = process with { Elements = process.Elements.Append(newTask).ToList() };
        var editedDocument = document with { Processes = [editedProcess] };

        var updatedDocument = await PutAsync(stored, editedDocument);

        // Everything that was there before the edit is still there, unchanged.
        await AssertContentEquivalent(originalDocument, updatedDocument);

        // The addition shows up.
        var addedTask = updatedDocument.Descendants(Bpmn + "serviceTask").Single(element => element.Attribute("id")?.Value == newTaskId);
        var addedBinding = addedTask.Descendants(Elsa + "activityBinding").Single();
        await Assert.That(addedBinding.Attribute("activityType")?.Value).IsEqualTo("Elsa.WriteLine");
        await Assert.That(addedBinding.Descendants(Elsa + "input").Single().Value).Contains("Archiving the order", StringComparison.CurrentCulture);
    }

    [Test]
    [DisplayName("Posting a document back with an unrelated task's binding edited keeps a top-level call activity's fire-and-forget flag")]
    public async Task ImportDocumentAsync_WithAnUnrelatedTasksBindingEdited_KeepsATopLevelCallActivitysFireAndForgetFlag()
    {
        var stored = await ImportAssetAsync("top-level-call-activity.bpmn");

        var document = DocumentService.ReadDocument(stored);
        var process = (await Assert.That(document.Processes).HasSingleItem())!;
        var logCompletion = process.Elements.Single(element => element.ElementId == "LogCompletion");

        var newBinding = Format.Write(new WriteLine("Completed, for real this time"));
        var editedLogCompletion = WithOverrides(logCompletion, extensions: BpmnActivityBindingFormat.Attach(logCompletion.Extensions, newBinding));
        var editedProcess = process with { Elements = process.Elements.Select(element => element.ElementId == "LogCompletion" ? editedLogCompletion : element).ToList() };

        var updated = await PutAsync(stored, document with { Processes = [editedProcess] });

        // The edit shows up...
        var editedTask = updated.Descendants(Bpmn + "serviceTask").Single(element => element.Attribute("id")?.Value == "LogCompletion");
        await Assert.That(editedTask.Descendants(Elsa + "input").Single().Value).Contains("Completed, for real this time", StringComparison.CurrentCulture);

        // ...and the unrelated call activity's fire-and-forget flag, which the document itself never carried, still
        // came from its stored work binding rather than a freshly bound, default-waiting one.
        var callActivity = updated.Descendants(Bpmn + "callActivity").Single(element => element.Attribute("id")?.Value == "NotifyDownstream");
        await Assert.That(callActivity.Attribute(Vw + "waitForCompletion")?.Value).IsEqualTo("false");
        await Assert.That(callActivity.Attribute("calledElement")?.Value).IsEqualTo("notification-workflow");
    }

    [Test]
    [DisplayName("Posting a document back with a call activity's calledElement changed does not resurrect the stored fire-and-forget flag")]
    public async Task ImportDocumentAsync_WhenACallActivitysCalledElementChanges_DoesNotResurrectTheStoredFireAndForgetFlag()
    {
        var stored = await ImportAssetAsync("top-level-call-activity.bpmn");

        var document = DocumentService.ReadDocument(stored);
        var process = (await Assert.That(document.Processes).HasSingleItem())!;
        var notifyDownstream = process.Elements.Single(element => element.ElementId == "NotifyDownstream");

        const string newCalledElement = "a-different-workflow";
        var editedProperties = new Dictionary<string, string>(notifyDownstream.Properties) { [BpmnXmlReader.CalledElementPropertyKey] = newCalledElement };
        var editedNotifyDownstream = WithOverrides(notifyDownstream, properties: editedProperties);
        var editedProcess = process with { Elements = process.Elements.Select(element => element.ElementId == "NotifyDownstream" ? editedNotifyDownstream : element).ToList() };

        var updated = await PutAsync(stored, document with { Processes = [editedProcess] });

        // The client's new calledElement is honoured...
        var callActivity = updated.Descendants(Bpmn + "callActivity").Single(element => element.Attribute("id")?.Value == "NotifyDownstream");
        await Assert.That(callActivity.Attribute("calledElement")?.Value).IsEqualTo(newCalledElement);

        // ...and the call binds fresh rather than inheriting the fire-and-forget flag stored against the process it
        // used to call: the client changed what is called, so the options that went with the old call do not survive.
        await Assert.That(callActivity.Attribute(Vw + "waitForCompletion")).IsNull();
    }

    [Test]
    [DisplayName("Posting a document back with a top-level call activity stripped of its bindingRef does not crash, and the call binds fresh")]
    public async Task ImportDocumentAsync_WhenATopLevelCallActivityCarriesNoBindingRef_DoesNotCrashAndBindsFresh()
    {
        var stored = await ImportAssetAsync("top-level-call-activity.bpmn");

        var document = DocumentService.ReadDocument(stored);
        var process = (await Assert.That(document.Processes).HasSingleItem())!;
        var notifyDownstream = process.Elements.Single(element => element.ElementId == "NotifyDownstream");

        var editedNotifyDownstream = WithOverrides(notifyDownstream, clearBindingRef: true);
        var editedProcess = process with { Elements = process.Elements.Select(element => element.ElementId == "NotifyDownstream" ? editedNotifyDownstream : element).ToList() };

        // Neither writing the document nor importing it back throws just because there is nothing to hand the stored
        // call options over under; the worst outcome is the fresh, waiting default below, not a crashed PUT.
        var updated = await PutAsync(stored, document with { Processes = [editedProcess] });

        var callActivity = updated.Descendants(Bpmn + "callActivity").Single(element => element.Attribute("id")?.Value == "NotifyDownstream");
        await Assert.That(callActivity.Attribute("calledElement")?.Value).IsEqualTo("notification-workflow");
        await Assert.That(callActivity.Attribute(Vw + "waitForCompletion")).IsNull();
    }

    [Test]
    [DisplayName("Importing a document against a definition id that does not exist refuses rather than creating one")]
    public async Task ImportDocumentAsync_WhenTheDefinitionDoesNotExist_ThrowsAndCreatesNothing()
    {
        var stored = await ImportAssetAsync("camunda-order-process.bpmn");
        var document = DocumentService.ReadDocument(stored);

        var missingDefinitionId = $"{Guid.NewGuid()}-does-not-exist";

        await Assert.ThrowsExactlyAsync<BpmnDefinitionNotFoundException>(() =>
            DocumentService.ImportDocumentAsync(document, missingDefinitionId, processId: null, CancellationToken.None));

        var filter = WorkflowDefinitionHandle.ByDefinitionId(missingDefinitionId, VersionOptions.Latest).ToFilter();
        var afterAttempt = await DefinitionStore.FindAsync(filter);
        await Assert.That(afterAttempt).IsNull();
    }

    [Test]
    [DisplayName("Posting a document back without a subprocess drops its stored body, even under a new subprocess reusing its bindingRef, and keeps every other body")]
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

        await Assert.That(NestedScopesOf(updated)).DoesNotContain(scope => scope.Attribute("id")?.Value == "OnRecall");
        await Assert.That(ScopeOf(updated, "Archive").Elements()).DoesNotContain(child => child.Attribute("id") is not null);
    }

    [Test]
    [DisplayName("Posting a document back with a subprocess turned into another kind of element drops its stored body")]
    public async Task ImportDocumentAsync_WithASubprocessTurnedIntoAnotherKindOfElement_DropsItsStoredBody()
    {
        var updated = await PutWithOnRecallEditedAsync(json =>
            ElementOf(json, "OnRecall").ReplaceWith(new JsonObject { ["elementId"] = "OnRecall", ["elementType"] = BpmnElementTypes.EndEvent }));

        await Assert.That(updated.Descendants(Bpmn + "endEvent")).HasSingleItem(element => element.Attribute("id")?.Value == "OnRecall");
    }

    [Test]
    [DisplayName("Posting a document back with a kept subprocess's multi-instance marker changed, removed or added writes exactly the posted marker")]
    [Arguments("subprocess-boundary-events.bpmn", "Fulfil", """{"isSequential":false,"cardinality":null,"collectionVariable":"orderLines","itemVariable":"lineItem"}""")]
    [Arguments("subprocess-boundary-events.bpmn", "Fulfil", null)]
    [Arguments("camunda-multi-instance-subprocess.bpmn", "ShipOrders", """{"isSequential":true,"cardinality":3,"collectionVariable":null,"itemVariable":"item"}""")]
    public async Task ImportDocumentAsync_WithAKeptSubprocessLoopMarkerEdited_WritesExactlyThePostedMarker(string assetFileName, string subprocessId, string? loopCharacteristics)
    {
        var stored = await ImportAssetAsync(assetFileName);
        var document = ThroughTheDocumentEndpoints(DocumentService.ReadDocument(stored), json =>
            ElementOf(json, subprocessId)["loopCharacteristics"] = loopCharacteristics is null ? null : JsonNode.Parse(loopCharacteristics));

        await PutAsync(stored, document);

        // What the next GET hands back is the posted marker, not the copy of the stored one the library also keeps in
        // the subprocess's body, which would otherwise be written back ahead of it and win the next read.
        var reread = DocumentService.ReadDocument(await FindLatestAsync(stored.DefinitionId)).Processes.Single().Elements.Single(element => element.ElementId == subprocessId);
        await Assert.That(JsonSerializer.Serialize(reread.LoopCharacteristics, BpmnDocumentJsonOptions.Value)).IsEqualTo(loopCharacteristics ?? "null");
    }

    [Test]
    [DisplayName("Posting a document back with a kept subprocess under another bindingRef still writes its stored body")]
    public async Task ImportDocumentAsync_WhenAKeptSubprocessCarriesAnotherBindingRef_StillWritesItsStoredBody()
    {
        var stored = await ImportAssetAsync("transaction-compensation.bpmn");
        var originalExport = ExportOf(stored);

        var updated = await PutAsync(stored, WithBindingRef(DocumentService.ReadDocument(stored), "BookTrip", "renamed-by-the-client"));

        await Assert.That(ComparableContentOf(ScopeOf(updated, "BookTrip"))).IsEqualTo(ComparableContentOf(ScopeOf(originalExport, "BookTrip")));
    }

    [Test]
    [DisplayName("Posting a document back with a kept subprocess stripped of its bindingRef refuses rather than emptying it")]
    public async Task ImportDocumentAsync_WhenAKeptSubprocessCarriesNoBindingRef_RefusesAndPersistsNothing()
    {
        var stored = await ImportAssetAsync("transaction-compensation.bpmn");
        var storedBefore = (SourceXmlOf(stored), stored.StringData);
        var document = WithBindingRef(DocumentService.ReadDocument(stored), "BookTrip", null);

        var exception = (await Assert.ThrowsExactlyAsync<BpmnInterchangeException>(() =>
            DocumentService.ImportDocumentAsync(document, stored.DefinitionId, ProcessIdOf(stored), CancellationToken.None)))!;

        await Assert.That(exception.Message).Contains("'BookTrip'", StringComparison.CurrentCulture);
        var afterAttempt = await FindLatestAsync(stored.DefinitionId);
        await Assert.That((SourceXmlOf(afterAttempt), afterAttempt.StringData)).IsEqualTo(storedBefore);
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
        await Assert.That(onRecallBodyIds).Contains("HandleRecall");

        var updated = await PutAsync(stored, ThroughTheDocumentEndpoints(DocumentService.ReadDocument(stored), edit));

        await Assert.That(updated.Descendants(Bpmn + "process").Single().Descendants()).DoesNotContain(element => element.Attribute("id")?.Value is { } id && onRecallBodyIds.Contains(id));
        await Assert.That(ComparableContentOf(ScopeOf(updated, "Fulfil"))).IsEqualTo(ComparableContentOf(ScopeOf(originalExport, "Fulfil")));
        return updated;
    }

    /// <summary>Everything <c>camunda-order-process.bpmn</c> carries that an edit must not disturb: foreign attributes, foreign extension elements, the elsa: binding on the untouched task, and BPMN DI waypoints.</summary>
    private static async Task AssertContentEquivalent(XDocument expected, XDocument actual)
    {
        var expectedProcess = expected.Descendants(Bpmn + "process").Single();
        var actualProcess = actual.Descendants(Bpmn + "process").Single();
        await Assert.That(actualProcess.Attribute("id")?.Value).IsEqualTo(expectedProcess.Attribute("id")?.Value);
        await Assert.That(expectedProcess.Attribute("id")?.Value).IsEqualTo("order-process");
        await Assert.That(actualProcess.Attribute(Camunda + "versionTag")?.Value).IsEqualTo(expectedProcess.Attribute(Camunda + "versionTag")?.Value);

        var expectedTask = expected.Descendants(Bpmn + "serviceTask").Single(element => element.Attribute("id")?.Value == "NotifyWarehouse");
        var actualTask = actual.Descendants(Bpmn + "serviceTask").Single(element => element.Attribute("id")?.Value == "NotifyWarehouse");
        await Assert.That(expectedTask.Attribute(Camunda + "asyncBefore")?.Value).IsEqualTo("true");
        await Assert.That(actualTask.Attribute(Camunda + "asyncBefore")?.Value).IsEqualTo(expectedTask.Attribute(Camunda + "asyncBefore")?.Value);
        await Assert.That(actualTask.Descendants(Bpmn + "documentation").Single().Value).IsEqualTo(expectedTask.Descendants(Bpmn + "documentation").Single().Value);

        var expectedBinding = expectedTask.Descendants(Elsa + "activityBinding").Single();
        var actualBinding = actualTask.Descendants(Elsa + "activityBinding").Single();
        await Assert.That(expectedBinding.Attribute("activityType")?.Value).IsEqualTo("Elsa.WriteLine");
        await Assert.That(actualBinding.Attribute("activityType")?.Value).IsEqualTo(expectedBinding.Attribute("activityType")?.Value);
        await Assert.That(actualBinding.Descendants(Elsa + "input").Single().Value).IsEqualTo(expectedBinding.Descendants(Elsa + "input").Single().Value);

        var expectedProperty = expected.Descendants(Camunda + "property").Single();
        var actualProperty = actual.Descendants(Camunda + "property").Single();
        await Assert.That(expectedProperty.Attribute("name")?.Value).IsEqualTo("owner");
        await Assert.That(actualProperty.Attribute("name")?.Value).IsEqualTo(expectedProperty.Attribute("name")?.Value);
        await Assert.That(actualProperty.Attribute("value")?.Value).IsEqualTo(expectedProperty.Attribute("value")?.Value);

        var expectedInputParameter = expected.Descendants(Camunda + "inputParameter").Single();
        var actualInputParameter = actual.Descendants(Camunda + "inputParameter").Single();
        await Assert.That(actualInputParameter.Value).IsEqualTo(expectedInputParameter.Value);

        var expectedWaypoints = expected.Descendants(Di + "waypoint").Select(WaypointOf).ToList();
        var actualWaypoints = actual.Descendants(Di + "waypoint").Select(WaypointOf).ToList();
        await Assert.That(expectedWaypoints.Count).IsEqualTo(4);
        await Assert.That(actualWaypoints).IsEquivalentTo(expectedWaypoints, CollectionOrdering.Matching);
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
        await Assert.That(definition).IsNotNull();
        return definition!;
    }

    /// <summary>Imports a fixture as a new definition, the way <c>Import</c> does, and returns it as stored.</summary>
    private async Task<WorkflowDefinition> ImportAssetAsync(string assetFileName)
    {
        var imported = await DocumentService.ImportAsync(ReadAsset(assetFileName), definitionId: null, name: null, processId: null, CancellationToken.None);
        await Assert.That(imported.ImportResult.Succeeded).IsTrue()
            .Because(string.Join("; ", imported.ImportResult.ValidationErrors.Select(error => error.Message)));
        return await FindLatestAsync(imported.ImportResult.WorkflowDefinition.DefinitionId);
    }

    /// <summary>Posts <paramref name="document"/> back onto <paramref name="stored"/> the way the document <c>PUT</c> does, and returns what exporting the result gives.</summary>
    private async Task<XDocument> PutAsync(WorkflowDefinition stored, BpmnDefinitions document)
    {
        var putResult = await DocumentService.ImportDocumentAsync(document, stored.DefinitionId, ProcessIdOf(stored), CancellationToken.None);
        await Assert.That(putResult.ImportResult.Succeeded).IsTrue()
            .Because(string.Join("; ", putResult.ImportResult.ValidationErrors.Select(error => error.Message)));
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

    /// <summary>
    /// A copy of <paramref name="element"/> with <paramref name="extensions"/> or <paramref name="properties"/>
    /// overridden and everything else carried across unchanged. <see cref="BpmnElement"/> is an immutable plain class,
    /// not a record, so it has no <c>with</c> expression of its own.
    /// </summary>
    private static BpmnElement WithOverrides(
        BpmnElement element,
        BpmnExtensions? extensions = null,
        IReadOnlyDictionary<string, string>? properties = null,
        string? bindingRef = null,
        bool clearBindingRef = false) => new(
        elementId: element.ElementId,
        elementType: element.ElementType,
        name: element.Name,
        bindingRef: clearBindingRef ? null : bindingRef ?? element.BindingRef,
        laneId: element.LaneId,
        defaultFlowId: element.DefaultFlowId,
        eventDefinitions: element.EventDefinitions,
        properties: properties ?? element.Properties,
        attachedToRef: element.AttachedToRef,
        cancelActivity: element.CancelActivity,
        loopCharacteristics: element.LoopCharacteristics,
        isForCompensation: element.IsForCompensation,
        compensationHandlerElementId: element.CompensationHandlerElementId,
        isTransaction: element.IsTransaction,
        triggeredByEvent: element.TriggeredByEvent,
        listenerBindingRef: element.ListenerBindingRef,
        extensions: extensions ?? element.Extensions);

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
    /// even though the marker does not. Comparing the distinct markers still fails on a marker lost or changed. Tracked
    /// upstream as <see href="https://github.com/valence-works/bpmn/issues/21">valence-works/bpmn#21</see>; remove this
    /// workaround once a <c>Bpmn.Interchange</c> release containing that fix is adopted.
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
