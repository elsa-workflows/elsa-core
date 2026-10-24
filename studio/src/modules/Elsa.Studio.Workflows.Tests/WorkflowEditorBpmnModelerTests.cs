using System.Reflection;
using System.Text.Json.Nodes;
using Bunit;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Models;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Responses;
using Elsa.Api.Client.Shared.Models;
using Elsa.Studio.Bpmn.Designers;
using Elsa.Studio.Bpmn.Extensions;
using Elsa.Studio.Contracts;
using Elsa.Studio.DomInterop.Contracts;
using Elsa.Studio.DomInterop.Models;
using Elsa.Studio.Models;
using Elsa.Studio.Workflows.Components.WorkflowDefinitionEditor.Components;
using Elsa.Studio.Workflows.Components.WorkflowDefinitionEditor.Components.ActivityProperties;
using Elsa.Studio.Workflows.Designer.Options;
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
/// With a BPMN designer enabled, the editor edits a BPMN-imported workflow's whole document in it: the document is
/// read and written as XML, never through the document PUT or the ordinary save, and content the designer cannot keep is
/// only dropped once the user has accepted it.
/// </summary>
public sealed class WorkflowEditorBpmnModelerTests : BunitContext, IAsyncLifetime
{
    private const string StoredXml = "<definitions />";
    private const string ETag = "\"REVISION-1\"";

    private readonly FakeBpmnDocumentService _documentService = new();
    private readonly FakeBpmnXmlService _xmlService = new();
    private readonly FakeModelerCanvas _canvas = new();
    private readonly RecordingEditorService _editorService = new();
    private readonly ReloadingDefinitionService _definitionService = new();
    private readonly RecordingUserMessageService _messages = new();

    public WorkflowEditorBpmnModelerTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        _xmlService.ReturnsXml(StoredXml, ETag);
        Services.AddBpmnEditing(_documentService);
        Services.AddBpmnDesigner(new BpmnDesignerDescriptor("test-designer", "_content/Test/designer.js"));
        Services.AddSingleton<IBpmnXmlService>(_xmlService);
        Services.AddSingleton<IWorkflowDefinitionEditorService>(_editorService);
        Services.AddSingleton<IWorkflowDefinitionService>(_definitionService);
        Services.AddSingleton<IDomAccessor, NoOpDomAccessor>();
        Services.AddSingleton<IUserMessageService>(_messages);
        ComponentFactories.Add<DiagramDesignerWrapper, DesignerStandIn>();
        ComponentFactories.Add<ActivityPropertiesPanel, ActivityPropertiesPanelStandIn>();
    }

    Task IAsyncLifetime.InitializeAsync() => Task.CompletedTask;
    async Task IAsyncLifetime.DisposeAsync() => await base.DisposeAsync();

    [Fact(DisplayName = "A BPMN-imported workflow opens in the designer's panel, and its document is never read as JSON")]
    public void OpensInTheDesigner()
    {
        var cut = RenderEditor();

        Assert.Equal(DefinitionId, Session(cut).DefinitionId);
        Assert.Empty(cut.FindComponents<BpmnPerformedByPanel>());
        Assert.Empty(cut.FindComponents<ActivityPropertiesPanel>());
        Assert.Equal(0, _documentService.GetCount);
    }

    [Fact(DisplayName = "While React Flow is in effect, the Performed by panel edits the document as before")]
    public void ReactFlow_KeepsThePerformedByPanel()
    {
        Services.Configure<DesignerOptions>(options => options.UseReactFlow = true);
        _documentService.ReturnsDocument(Document(), ETag);

        var cut = RenderEditor();

        Assert.NotEmpty(cut.FindComponents<BpmnPerformedByPanel>());
        Assert.Empty(cut.FindComponents<BpmnModelerPanel>());
    }

    [Fact(DisplayName = "Saving sends the designer's document as XML with the ETag it was read at, then reloads the workflow without reloading the canvas")]
    public async Task Save_PutsTheXml()
    {
        var cut = RenderEditor();
        await EditAsync(cut);
        var version = Session(cut).DocumentVersion;
        _xmlService.AcceptsPut("\"REVISION-2\"").ReturnsXml(_canvas.Xml, "\"REVISION-2\"");
        var reloaded = _definitionService.Latest = Definition(sourceXml: _canvas.Xml);

        await InvokeAsync(cut, "OnSaveClick");

        Assert.Equal([(DefinitionId, _canvas.Xml, ETag)], _xmlService.Puts);
        Assert.Empty(_editorService.Saves);
        Assert.Empty(_documentService.Puts);
        Assert.Same(reloaded, cut.Instance.WorkflowDefinition);
        Assert.False(Session(cut).IsDirty);
        Assert.Equal(version, Session(cut).DocumentVersion);
        Assert.Contains("Workflow saved", _messages.Messages);
    }

    [Fact(DisplayName = "Saving other properties and the diagram saves the properties first, then sends the XML with the ETag that save left")]
    public async Task SaveWithOtherProperties_RefreshesTheETagFirst()
    {
        var definition = Definition();
        var cut = RenderEditor(definition);
        await EditAsync(cut);
        definition.Name = "Renamed";
        await cut.InvokeAsync(() => cut.Instance.NotifyWorkflowChangedAsync());
        _xmlService.ReturnsXml(StoredXml, "\"REVISION-1B\"").AcceptsPut("\"REVISION-2\"");
        _definitionService.Latest = Definition(sourceXml: _canvas.Xml);

        await InvokeAsync(cut, "OnSaveClick");

        var save = Assert.Single(_editorService.Saves);
        Assert.Equal("Renamed", save.Definition.Name);
        Assert.Equal("\"REVISION-1B\"", Assert.Single(_xmlService.Puts).IfMatch);
    }

    [Fact(DisplayName = "Content the designer cannot keep is only dropped once the user accepts it")]
    public async Task DroppedContent_NeedsTheUsersAcceptance()
    {
        var cut = Render(builder =>
        {
            builder.OpenComponent<MudDialogProvider>(0);
            builder.CloseComponent();
            builder.OpenComponent<WorkflowEditor>(1);
            builder.AddAttribute(2, nameof(WorkflowEditor.WorkflowDefinition), Definition());
            builder.CloseComponent();
        });
        var editor = cut.FindComponent<WorkflowEditor>();
        await EditAsync(editor, [new(BpmnDesignerLoadWarningKinds.Dropped, "Lane set is not shown.", "Ls_1")]);
        _xmlService.AcceptsPut("\"REVISION-2\"");
        _definitionService.Latest = Definition(sourceXml: _canvas.Xml);

        var cancelled = InvokeAsync(editor, "OnSaveClick");
        cut.WaitForAssertion(() => Assert.Contains("Ls_1: Lane set is not shown.", cut.Find("[data-testid=bpmn-dropped-content]").TextContent));
        await cut.InvokeAsync(() => cut.FindAll(".mud-dialog-actions button")[0].Click());
        await cancelled;

        Assert.Empty(_xmlService.Puts);

        var accepted = InvokeAsync(editor, "OnSaveClick");
        cut.WaitForAssertion(() => cut.Find("[data-testid=bpmn-dropped-content-save]"));
        await cut.InvokeAsync(() => cut.Find("[data-testid=bpmn-dropped-content-save]").Click());
        await accepted;

        Assert.Single(_xmlService.Puts);
    }

    [Fact(DisplayName = "Foreign content the server reports as dropped is named after the save")]
    public async Task DroppedForeignContent_IsReported()
    {
        var cut = RenderEditor();
        await EditAsync(cut);
        _xmlService.AcceptsPut("\"REVISION-2\"", new BpmnForeignContentModel { ElementId = "Task_1", Kind = "attribute", Namespace = "urn:acme", Name = "priority" });
        _definitionService.Latest = Definition(sourceXml: _canvas.Xml);

        await InvokeAsync(cut, "OnSaveClick");

        Assert.Contains(_messages.Messages, message => message.Contains("attribute 'priority' on 'Task_1'", StringComparison.Ordinal));
    }

    [Fact(DisplayName = "A refused save is reported, neither retried nor reloaded, and the diagram stays unsaved")]
    public async Task RefusedSave_IsReported()
    {
        var cut = RenderEditor();
        await EditAsync(cut);
        _xmlService.RefusesPut(BpmnDocumentFailureReason.PreconditionFailed);

        await InvokeAsync(cut, "OnSaveClick");

        Assert.Single(_xmlService.Puts);
        Assert.Equal(0, _definitionService.FindCount);
        Assert.True(Session(cut).IsDirty);
        Assert.Contains(_messages.Messages, message => message.Contains("were not saved", StringComparison.Ordinal));
    }

    [Fact(DisplayName = "Publishing with unsaved diagram changes is refused")]
    public async Task Publish_WithUnsavedDiagramChanges_IsRefused()
    {
        var cut = RenderEditor();
        await EditAsync(cut);

        await InvokeAsync(cut, "OnPublishClicked");

        Assert.Empty(_editorService.Saves);
        Assert.Contains(_messages.Messages, message => message.Contains("before publishing", StringComparison.Ordinal));
    }

    [Fact(DisplayName = "Publishing while the diagram is being saved is refused, since the editor's graph predates the saved document")]
    public async Task Publish_DuringADiagramSave_IsRefused()
    {
        var cut = RenderEditor();
        await EditAsync(cut);
        _xmlService.PutGate = new();
        _xmlService.AcceptsPut("\"REVISION-2\"");
        _definitionService.Latest = Definition(sourceXml: _canvas.Xml);

        var save = InvokeAsync(cut, "OnSaveClick");
        cut.WaitForAssertion(() => Assert.Single(_xmlService.Puts));
        await InvokeAsync(cut, "OnPublishClicked");
        _xmlService.PutGate.SetResult();
        await save;

        Assert.Empty(_editorService.Saves);
        Assert.Contains(_messages.Messages, message => message.Contains("finish saving before publishing", StringComparison.Ordinal));
    }

    [Fact(DisplayName = "Reloading reads the workflow and its document again, discarding the designer's edits")]
    public async Task Reload_ReadsTheDocumentAgain()
    {
        var cut = RenderEditor();
        await EditAsync(cut);
        var version = Session(cut).DocumentVersion;
        _definitionService.Latest = Definition();

        await cut.InvokeAsync(() => cut.FindComponent<BpmnModelerPanel>().Instance.ReloadRequested!());

        Assert.Equal(version + 1, Session(cut).DocumentVersion);
    }

    [Fact(DisplayName = "Importing the same document again keeps the designer's document and adopts the new draft's ETag")]
    public async Task ImportOfTheSameDocument_RefreshesTheETag()
    {
        var cut = RenderEditor();
        await EditAsync(cut);
        var version = Session(cut).DocumentVersion;
        _xmlService.ReturnsXml(StoredXml, "\"REVISION-1B\"").AcceptsPut("\"REVISION-2\"").ReturnsXml(_canvas.Xml, "\"REVISION-2\"");
        _definitionService.Latest = Definition(sourceXml: _canvas.Xml);

        await cut.InvokeAsync(() => cut.Instance.SetImportedWorkflowDefinitionAsync(Definition()));
        await InvokeAsync(cut, "OnSaveClick");

        Assert.Equal(version, Session(cut).DocumentVersion);
        Assert.Equal("\"REVISION-1B\"", Assert.Single(_xmlService.Puts).IfMatch);
    }

    [Fact(DisplayName = "Importing a different document into the workflow makes the designer load it")]
    public async Task Import_LoadsTheNewDocument()
    {
        var cut = RenderEditor();
        await EditAsync(cut);
        var version = Session(cut).DocumentVersion;

        await cut.InvokeAsync(() => cut.Instance.SetImportedWorkflowDefinitionAsync(Definition(sourceXml: "<imported/>")));

        Assert.Equal(version + 1, Session(cut).DocumentVersion);
    }

    [Fact(DisplayName = "The panel lists the problems the server finds in the document, and clicking an element a problem names selects it")]
    public async Task Problems_AreListedAndSelectTheirElements()
    {
        _documentService.Analyzes = _ => Task.FromResult(new Result<BpmnImportAnalysisModel, ValidationErrors>(new BpmnImportAnalysisModel
        {
            Refusal = new() { Code = BpmnErrorCodes.ImportBindingInvalid, Message = "Task_1 is unbound.", ElementIds = ["Task_1"] }
        }));
        var cut = RenderEditor();
        await EditAsync(cut);
        await Session(cut).Analysis;

        var problem = cut.WaitForElement("[data-testid=bpmn-problem]");
        Assert.Contains("Task_1 is unbound.", problem.TextContent);
        await cut.InvokeAsync(() => cut.Find("[data-testid=bpmn-problem-element]").Click());

        Assert.Equal(["Task_1"], _canvas.Selections);
        Assert.Equal([new BpmnDesignerMarker("Task_1", BpmnDesignerMarker.Error, "Task_1 is unbound.")], Assert.Single(_canvas.AppliedMarkers));
    }

    [Fact(DisplayName = "The editor has the server check the process the definition was imported from")]
    public async Task Problems_AreFoundForTheDefinitionsProcess()
    {
        _documentService.Analyzes = _ => Task.FromResult(new Result<BpmnImportAnalysisModel, ValidationErrors>(new BpmnImportAnalysisModel()));
        var definition = Definition();
        definition.CustomProperties[BpmnProcessConstants.SourceProcessIdCustomPropertyKey] = "second-process";
        var cut = RenderEditor(definition);
        await EditAsync(cut);
        await Session(cut).Analysis;

        Assert.Equal(["second-process"], _documentService.AnalyzedProcessIds);
    }

    [Fact(DisplayName = "An import that binds another process of the document to the definition has the server check that process")]
    public async Task Problems_FollowAnImportOfAnotherProcess()
    {
        _documentService.Analyzes = _ => Task.FromResult(new Result<BpmnImportAnalysisModel, ValidationErrors>(new BpmnImportAnalysisModel()));
        var cut = RenderEditor(DefinitionOfProcess("first-process"));
        await EditAsync(cut);
        var session = Session(cut);

        await cut.InvokeAsync(() => cut.Instance.SetImportedWorkflowDefinitionAsync(DefinitionOfProcess("second-process")));

        Assert.Same(session, Session(cut));
        Assert.Equal("second-process", session.ProcessId);
    }

    private static WorkflowDefinition DefinitionOfProcess(string processId)
    {
        var definition = Definition();
        definition.CustomProperties[BpmnProcessConstants.SourceProcessIdCustomPropertyKey] = processId;
        return definition;
    }

    private IRenderedComponent<WorkflowEditor> RenderEditor(WorkflowDefinition? definition = null) =>
        Render<WorkflowEditor>(parameters => parameters.Add(x => x.WorkflowDefinition, definition ?? Definition()));

    private static BpmnModelerSession Session(IRenderedComponent<WorkflowEditor> cut) => cut.FindComponent<BpmnModelerPanel>().Instance.Session;

    /// <summary>Opens the document in a stand-in canvas, as the designer would, and makes an edit in it.</summary>
    private async Task EditAsync(IRenderedComponent<WorkflowEditor> cut, IReadOnlyList<BpmnDesignerLoadWarning>? warnings = null)
    {
        var session = Session(cut);
        await cut.InvokeAsync(async () =>
        {
            await session.EnsureLoadedAsync();
            session.AttachCanvas(_canvas);
            session.CanvasLoaded(new(true, null, warnings ?? []), session.DocumentVersion);
            session.CanvasChanged(new(["Task_1"], IsDirty: true, CanUndo: true, CanRedo: false));
        });
    }

    private static Task InvokeAsync(IRenderedComponent<WorkflowEditor> cut, string methodName) =>
        cut.InvokeAsync(() => (Task)typeof(WorkflowEditor).GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(cut.Instance, null)!);

    private static WorkflowDefinition Definition(string sourceXml = StoredXml) => new()
    {
        Id = "version-1",
        DefinitionId = DefinitionId,
        Name = "Order Process",
        Version = 1,
        IsLatest = true,
        Root = WithNodeIds(RootActivity()),
        CustomProperties = new Dictionary<string, object> { [BpmnProcessConstants.SourceXmlCustomPropertyKey] = sourceXml }
    };

    private static JsonObject WithNodeIds(JsonObject activity)
    {
        activity["nodeId"] = activity["id"]!.GetValue<string>();

        foreach (var child in (activity["activities"] as JsonArray ?? []).OfType<JsonObject>())
            WithNodeIds(child);

        return activity;
    }

    private sealed class RecordingEditorService : IWorkflowDefinitionEditorService
    {
        public List<(WorkflowDefinition Definition, JsonObject? Root)> Saves { get; } = [];

        public Task<Result<SaveWorkflowDefinitionResponse, ValidationErrors>> SaveAsync(WorkflowDefinition workflowDefinition, bool publish, Func<WorkflowDefinition, Task>? workflowSavedCallback = null, CancellationToken cancellationToken = default)
        {
            Saves.Add((workflowDefinition, (JsonObject?)workflowDefinition.Root?.DeepClone()));
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
}
