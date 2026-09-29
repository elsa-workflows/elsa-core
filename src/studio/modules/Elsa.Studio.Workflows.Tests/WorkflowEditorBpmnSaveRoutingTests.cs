using System.Reflection;
using System.Text.Json.Nodes;
using Bunit;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Models;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Responses;
using Elsa.Api.Client.Shared.Models;
using Elsa.Studio.Contracts;
using Elsa.Studio.DomInterop.Contracts;
using Elsa.Studio.DomInterop.Models;
using Elsa.Studio.Models;
using Elsa.Studio.Workflows.Components.WorkflowDefinitionEditor.Components;
using Elsa.Studio.Workflows.Components.WorkflowDefinitionEditor.Components.ActivityProperties;
using Elsa.Studio.Workflows.DiagramDesigners.Bpmn;
using Elsa.Studio.Workflows.Domain.Contracts;
using Elsa.Studio.Workflows.Domain.Models;
using Elsa.Studio.Workflows.Domain.Models.Bpmn;
using Elsa.Studio.Workflows.Shared.Components;
using Elsa.Studio.Workflows.Tests.Support;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using Xunit;
using static Elsa.Studio.Workflows.Tests.Support.BpmnDocumentFixtures;

namespace Elsa.Studio.Workflows.Tests;

/// <summary>
/// The integration trap: a BPMN-imported workflow's activity graph is derived from its BPMN document, and elsa-core
/// refuses to read or export that document once the ordinary workflow-definition save has rewritten the graph behind it.
/// So the editor saves a binding change through the document PUT — with the ETag it read the document at — never
/// through the ordinary save, and lets the ordinary save carry the workflow's other properties only, refusing any save
/// that would send a changed graph. Both directions are pinned: the refusal, and the saves that must still go through.
/// </summary>
public sealed class WorkflowEditorBpmnSaveRoutingTests : BunitContext, IAsyncLifetime
{
    private const string ETag = "\"REVISION-1\"";

    private readonly FakeBpmnDocumentService _documentService = new();
    private readonly RecordingEditorService _editorService;
    private readonly ReloadingDefinitionService _definitionService = new();
    private readonly RecordingUserMessageService _messages = new();

    public WorkflowEditorBpmnSaveRoutingTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        _editorService = new(() => _documentService.Puts.Count);
        _documentService.ReturnsDocument(Document(), ETag);
        Services.AddBpmnEditing(_documentService);
        Services.AddSingleton<IWorkflowDefinitionEditorService>(_editorService);
        Services.AddSingleton<IWorkflowDefinitionService>(_definitionService);
        Services.AddSingleton<IDomAccessor, NoOpDomAccessor>();
        Services.AddSingleton<IUserMessageService>(_messages);
        ComponentFactories.Add<DiagramDesignerWrapper, DesignerStandIn>();
        ComponentFactories.Add<ActivityPropertiesPanel, ActivityPropertiesPanelStandIn>();
        ComponentFactories.Add<BpmnPerformedByPanel, PerformedByPanelStandIn>();
    }

    Task IAsyncLifetime.InitializeAsync() => Task.CompletedTask;
    async Task IAsyncLifetime.DisposeAsync() => await base.DisposeAsync();

    [Fact]
    public void ABpmnImportedWorkflow_ShowsThePerformedBySection_InPlaceOfTheActivityPropertiesPanel()
    {
        var cut = RenderEditor(BpmnDefinition());

        Assert.Equal(DefinitionId, cut.FindComponent<BpmnPerformedByPanel>().Instance.Session.DefinitionId);
        Assert.Empty(cut.FindComponents<ActivityPropertiesPanel>());
    }

    [Fact]
    public void ABpmnRootWithoutImportedSource_KeepsTheActivityPropertiesPanel_AndTheOrdinarySave()
    {
        // Built some other way: there is no document to keep in step, so nothing here may take the ordinary save away.
        var definition = BpmnDefinition();
        definition.CustomProperties.Remove(BpmnProcessConstants.SourceXmlCustomPropertyKey);

        var cut = RenderEditor(definition);

        Assert.NotEmpty(cut.FindComponents<ActivityPropertiesPanel>());
        Assert.Empty(cut.FindComponents<BpmnPerformedByPanel>());
    }

    [Fact]
    public async Task SavingABindingChange_PutsTheDocumentWithTheETagItWasReadAt_NeverTheOrdinarySave_ThenReloadsTheDefinition()
    {
        var cut = RenderEditor(BpmnDefinition());
        var binding = await EditBindingAsync(cut);
        _documentService.AcceptsPut("\"REVISION-2\"");
        var reloaded = _definitionService.Latest = BpmnDefinition("Order Process, as re-imported");

        await InvokeAsync(cut, "OnSaveClick");

        var put = Assert.Single(_documentService.Puts);
        Assert.Equal((DefinitionId, ETag), (put.DefinitionId, put.IfMatch));
        Assert.True(JsonNode.DeepEquals(binding, BpmnActivityBindingFormat.Find(TaskElement(put.Document))));
        Assert.Empty(_editorService.Saves);
        Assert.Equal(1, _definitionService.FindCount);
        Assert.Same(reloaded, cut.Instance.WorkflowDefinition);
        Assert.False(Session(cut).IsDirty);
    }

    [Fact]
    public async Task SavingOtherPropertiesAndABindingChange_SavesThePropertiesFirst_WithTheGraphUnchanged_ThenTheBinding()
    {
        var definition = BpmnDefinition();
        var cut = RenderEditor(definition);
        await EditBindingAsync(cut);
        definition.Name = "Renamed";
        await cut.InvokeAsync(() => cut.Instance.NotifyWorkflowChangedAsync());
        _documentService.AcceptsPut("\"REVISION-2\"");
        _definitionService.Latest = BpmnDefinition();

        await InvokeAsync(cut, "OnSaveClick");

        var save = Assert.Single(_editorService.Saves);
        Assert.Equal(0, save.PutsBefore);
        Assert.Equal("Renamed", save.Definition.Name);
        Assert.True(JsonNode.DeepEquals(WithNodeIds(RootActivity()), save.Root));
        Assert.Single(_documentService.Puts);
    }

    /// <summary>
    /// The ordinary save re-serializes the root and, after a publish, may open a new draft version: either can advance
    /// the document's own ETag (which the graph's serialized text feeds into) without changing what a BPMN import of it
    /// would produce. Sending the binding PUT with the ETag the session read before that save would then be refused for
    /// a change nobody made, so the session re-reads the document first and, finding it unchanged, adopts the new ETag.
    /// </summary>
    [Fact]
    public async Task SavingOtherPropertiesAndABindingChange_WhenTheOrdinarySaveOnlyAdvancesTheETag_SendsThePutWithTheRefreshedETag()
    {
        var definition = BpmnDefinition();
        var cut = RenderEditor(definition);
        await EditBindingAsync(cut);
        definition.Name = "Renamed";
        await cut.InvokeAsync(() => cut.Instance.NotifyWorkflowChangedAsync());
        _documentService.ReturnsDocument(Document(), "\"REVISION-1B\"").AcceptsPut("\"REVISION-2\"").ReturnsDocument(Document(), "\"REVISION-2\"");
        _definitionService.Latest = BpmnDefinition();

        await InvokeAsync(cut, "OnSaveClick");

        var put = Assert.Single(_documentService.Puts);
        Assert.Equal("\"REVISION-1B\"", put.IfMatch);
        Assert.False(Session(cut).IsDirty);
        Assert.Contains(_messages.Messages, message => message.Contains("Workflow saved", StringComparison.Ordinal));
    }

    /// <summary>The document the ordinary save's re-read turns up may genuinely have changed; that is still a conflict.</summary>
    [Fact]
    public async Task SavingOtherPropertiesAndABindingChange_WhenTheOrdinarySavesReReadFindsARealChange_ReportsTheConflict_WithoutSendingThePut()
    {
        var definition = BpmnDefinition();
        var cut = RenderEditor(definition);
        await EditBindingAsync(cut);
        definition.Name = "Renamed";
        await cut.InvokeAsync(() => cut.Instance.NotifyWorkflowChangedAsync());
        var changedOnTheServer = Document();
        TaskElement(changedOnTheServer)["name"] = "Changed by someone else";
        _documentService.ReturnsDocument(changedOnTheServer, "\"REVISION-1B\"");
        _definitionService.Latest = BpmnDefinition();

        await InvokeAsync(cut, "OnSaveClick");

        Assert.Single(_editorService.Saves);
        Assert.Empty(_documentService.Puts);
        Assert.Equal(BpmnDocumentFailureReason.PreconditionFailed, Session(cut).SaveFailure!.Reason);
        Assert.True(Session(cut).IsDirty);
        Assert.Contains(_messages.Messages, message => message.Contains("were not saved", StringComparison.Ordinal));
    }

    /// <summary>A PUT that succeeds is not the whole story if the read that should confirm it fails: saying only
    /// "Workflow saved" would hide that the document panel now shows a load failure.</summary>
    [Fact]
    public async Task SavingABindingChange_WhenThePutSucceedsButTheFollowUpReadFails_SaysSoRatherThanJustThatItSaved()
    {
        var cut = RenderEditor(BpmnDefinition());
        await EditBindingAsync(cut);
        _documentService.AcceptsPut("\"REVISION-2\"").RefusesGet(BpmnDocumentFailureReason.SourceStale);
        _definitionService.Latest = BpmnDefinition();

        await InvokeAsync(cut, "OnSaveClick");

        Assert.Single(_documentService.Puts);
        Assert.True(Session(cut).SavedButReloadFailed);
        Assert.Contains(_messages.Messages, message => message.Contains("could not be reloaded", StringComparison.Ordinal));
        Assert.DoesNotContain(_messages.Messages, message => message == "Workflow saved");
    }

    [Fact]
    public async Task AnOrdinarySaveThatWouldChangeTheGraph_IsRefused_AndNothingIsSent()
    {
        var definition = BpmnDefinition();
        var cut = RenderEditor(definition);
        definition.Root!["activities"]![0]!["text"] = JsonValue.Create("edited outside the document");

        await InvokeSaveChangesAsync(cut);

        Assert.Empty(_editorService.Saves);
        Assert.Contains(_messages.Messages, message => message.Contains("come from its BPMN document", StringComparison.Ordinal));
    }

    [Fact]
    public async Task AnOrdinarySaveOfTheOtherProperties_GoesThrough_WithTheGraphAsTheServerSentIt()
    {
        var definition = BpmnDefinition();
        var cut = RenderEditor(definition);
        definition.Description = "Handles a customer order end to end.";

        await InvokeSaveChangesAsync(cut);

        var save = Assert.Single(_editorService.Saves);
        Assert.Equal("Handles a customer order end to end.", save.Definition.Description);
        Assert.True(JsonNode.DeepEquals(WithNodeIds(RootActivity()), save.Root));
    }

    [Fact]
    public async Task ApplyingAGraphChangeFromTheCodeView_CannotBeSaved()
    {
        var cut = RenderEditor(BpmnDefinition());
        var edited = BpmnDefinition();
        edited.Root!["activities"]![0]!["text"] = JsonValue.Create("edited in the code view");

        await cut.InvokeAsync(() => cut.Instance.ApplyWorkflowDefinitionAsync(edited));
        await InvokeAsync(cut, "OnSaveClick");

        Assert.Empty(_editorService.Saves);
        Assert.Empty(_documentService.Puts);
        Assert.Contains(_messages.Messages, message => message.Contains("come from its BPMN document", StringComparison.Ordinal));
    }

    [Fact]
    public async Task AnOrdinarySave_ReadsTheDocumentAgain_SoAStaleDocumentIsReportedAtOnce()
    {
        var cut = RenderEditor(BpmnDefinition());
        await Session(cut).EnsureLoadedAsync();
        _documentService.RefusesGet(BpmnDocumentFailureReason.SourceStale);

        await InvokeSaveChangesAsync(cut);

        Assert.Single(_editorService.Saves);
        Assert.Equal(BpmnDocumentFailureReason.SourceStale, Session(cut).LoadFailure!.Reason);
    }

    [Fact]
    public async Task PublishingWithUnsavedBindingChanges_IsRefused()
    {
        var cut = RenderEditor(BpmnDefinition());
        await EditBindingAsync(cut);

        await InvokeAsync(cut, "OnPublishClicked");

        Assert.Empty(_editorService.Saves);
        Assert.Contains(_messages.Messages, message => message.Contains("before publishing", StringComparison.Ordinal));
    }

    [Fact]
    public async Task ASaveRefusedBecauseTheDefinitionMovedOn_IsNeitherRetriedNorReloaded()
    {
        var cut = RenderEditor(BpmnDefinition());
        await EditBindingAsync(cut);
        _documentService.RefusesPut(BpmnDocumentFailureReason.PreconditionFailed);

        await InvokeAsync(cut, "OnSaveClick");

        Assert.Single(_documentService.Puts);
        Assert.Equal(0, _definitionService.FindCount);
        Assert.Equal(BpmnDocumentFailureReason.PreconditionFailed, Session(cut).SaveFailure!.Reason);
        Assert.True(Session(cut).IsDirty);
    }

    [Fact]
    public async Task Reloading_ReplacesTheDefinitionAndTheDocument_DiscardingUnsavedBindingChanges()
    {
        var cut = RenderEditor(BpmnDefinition());
        await EditBindingAsync(cut);
        var reloaded = _definitionService.Latest = BpmnDefinition();

        await cut.InvokeAsync(() => cut.FindComponent<BpmnPerformedByPanel>().Instance.ReloadRequested!());

        Assert.Same(reloaded, cut.Instance.WorkflowDefinition);
        Assert.Equal(2, _documentService.GetCount);
        Assert.False(Session(cut).IsDirty);
    }

    private IRenderedComponent<WorkflowEditor> RenderEditor(WorkflowDefinition definition) =>
        Render<WorkflowEditor>(parameters => parameters.Add(x => x.WorkflowDefinition, definition));

    private static BpmnDocumentSession Session(IRenderedComponent<WorkflowEditor> cut) => cut.FindComponent<BpmnPerformedByPanel>().Instance.Session;

    private static async Task<JsonObject> EditBindingAsync(IRenderedComponent<WorkflowEditor> cut)
    {
        var session = Session(cut);
        await session.EnsureLoadedAsync();
        var binding = BpmnActivityBindingFormat.Create("Elsa.HttpRequest", [KeyValuePair.Create<string, JsonNode?>("url", JsonNode.Parse("""{"typeName":"Uri","expression":{"type":"JavaScript","value":"getUrl()"}}"""))]);
        session.SetBinding(TaskId, binding);
        return binding;
    }

    private static Task InvokeSaveChangesAsync(IRenderedComponent<WorkflowEditor> cut) =>
        InvokeAsync(cut, "SaveChangesAsync", true, false, false, null, null, null);

    private static Task InvokeAsync(IRenderedComponent<WorkflowEditor> cut, string methodName, params object?[] arguments) =>
        cut.InvokeAsync(() => (Task)typeof(WorkflowEditor).GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(cut.Instance, arguments.Length == 0 ? null : arguments)!);

    private static WorkflowDefinition BpmnDefinition(string name = "Order Process") => new()
    {
        Id = "version-1",
        DefinitionId = DefinitionId,
        Name = name,
        Version = 1,
        IsLatest = true,
        Root = WithNodeIds(RootActivity()),
        CustomProperties = new Dictionary<string, object> { [BpmnProcessConstants.SourceXmlCustomPropertyKey] = "<definitions />" }
    };

    /// <summary>
    /// The fixture is the stored graph, which carries no node ids; a definition the API returns has one on every
    /// activity, and the editor's activity graph is keyed on them.
    /// </summary>
    private static JsonObject WithNodeIds(JsonObject activity)
    {
        activity["nodeId"] = activity["id"]!.GetValue<string>();

        foreach (var child in (activity["activities"] as JsonArray ?? []).OfType<JsonObject>())
            WithNodeIds(child);

        return activity;
    }

    private sealed class RecordingEditorService(Func<int> putsSoFar) : IWorkflowDefinitionEditorService
    {
        public List<(WorkflowDefinition Definition, JsonObject? Root, bool Publish, int PutsBefore)> Saves { get; } = [];

        public Task<Result<SaveWorkflowDefinitionResponse, ValidationErrors>> SaveAsync(WorkflowDefinition workflowDefinition, bool publish, Func<WorkflowDefinition, Task>? workflowSavedCallback = null, CancellationToken cancellationToken = default)
        {
            Saves.Add((workflowDefinition, (JsonObject?)workflowDefinition.Root?.DeepClone(), publish, putsSoFar()));
            return Task.FromResult(new Result<SaveWorkflowDefinitionResponse, ValidationErrors>(new SaveWorkflowDefinitionResponse(workflowDefinition, false, 0)));
        }

        public Task<SaveWorkflowDefinitionResponse> PublishAsync(WorkflowDefinition workflowDefinition, Func<WorkflowDefinition, Task>? workflowPublishedCallback = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<Result<WorkflowDefinition, ValidationErrors>> RetractAsync(WorkflowDefinition workflowDefinition, Func<WorkflowDefinition, Task>? workflowRetractedCallback = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<FileDownload> ExportAsync(WorkflowDefinition workflowDefinition, bool includeConsumingWorkflows = false, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    private sealed class ReloadingDefinitionService : ThrowingWorkflowDefinitionServiceBase
    {
        public WorkflowDefinition? Latest { get; set; }
        public int FindCount { get; private set; }

        public override Task<WorkflowDefinition?> FindByDefinitionIdAsync(string definitionId, VersionOptions? versionOptions = null, CancellationToken cancellationToken = default)
        {
            FindCount++;
            return Task.FromResult(Latest);
        }
    }

    private sealed class RecordingUserMessageService : IUserMessageService
    {
        public List<string> Messages { get; } = [];
        public void ShowSnackbarTextMessage(string message, Severity severity = Severity.Normal, Action<SnackbarOptions>? snackbarOptions = null) => Messages.Add(message);
        public void ShowSnackbarTextMessage(IEnumerable<string> messages, Severity severity = Severity.Normal, Action<SnackbarOptions>? snackbarOptions = null) => Messages.AddRange(messages);
    }

    private sealed class NoOpDomAccessor : IDomAccessor
    {
        public Task<DomRect> GetBoundingClientRectAsync(ElementRef elementRef, CancellationToken cancellationToken = default) => Task.FromResult(new DomRect());
        public Task<double> GetVisibleHeightAsync(ElementRef elementRef, CancellationToken cancellationToken = default) => Task.FromResult(0d);
        public Task ClickElementAsync(ElementRef elementRef, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class DesignerStandIn : DiagramDesignerWrapper
    {
        protected override Task OnInitializedAsync() => Task.CompletedTask;

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
        }
    }

    private sealed class ActivityPropertiesPanelStandIn : ActivityPropertiesPanel
    {
        protected override Task OnInitializedAsync() => Task.CompletedTask;

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
        }
    }

    /// <summary>Keeps the parameters the editor hands the section, so a test can drive the session it was given.</summary>
    private sealed class PerformedByPanelStandIn : BpmnPerformedByPanel
    {
        protected override Task OnInitializedAsync() => Task.CompletedTask;
        protected override Task OnParametersSetAsync() => Task.CompletedTask;

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
        }
    }
}
