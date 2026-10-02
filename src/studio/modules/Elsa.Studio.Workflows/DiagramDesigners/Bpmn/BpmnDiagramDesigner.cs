using System.Text.Json;
using System.Text.Json.Nodes;
using Elsa.Api.Client.Extensions;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Models;
using Elsa.Studio.Contracts;
using Elsa.Studio.DomInterop.Contracts;
using Elsa.Studio.Localization;
using Elsa.Studio.Workflows.Domain.Contracts;
using Elsa.Studio.Workflows.Domain.Models.Bpmn;
using Elsa.Studio.Workflows.Designer.Options;
using Elsa.Studio.Workflows.Domain.Models;
using Elsa.Studio.Workflows.Extensions;
using Elsa.Studio.Workflows.UI.Contexts;
using Elsa.Studio.Workflows.UI.Contracts;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Options;
using MudBlazor;

namespace Elsa.Studio.Workflows.DiagramDesigners.Bpmn;

/// <summary>
/// A diagram designer that displays an imported BPMN process, read-only. Editing lands in a later
/// increment (W14); this designer only ever shows the diagram and forwards selection.
/// </summary>
public class BpmnDiagramDesigner(
    ILocalizer localizer,
    IOptions<DesignerOptions> designerOptions,
    IDialogService dialogService,
    IBpmnInterchangeService bpmnInterchangeService,
    IFiles files,
    IUserMessageService userMessageService) : IDiagramDesignerToolboxProvider, IBpmnElementStatsSink
{
    private readonly Guid _id = Guid.NewGuid();
    private BpmnDesignerWrapper? _designerWrapper;
    private JsonObject _rootActivity = [];
    private string? _sourceXml;
    private WorkflowDefinition? _workflowDefinition;

    /// <summary>
    /// The latest element-keyed instance overlay handed to <see cref="UpdateElementStatsAsync"/>, held only until
    /// <see cref="_designerWrapper"/> is captured -- from that point on, <see cref="BpmnDesignerWrapper"/> is the
    /// one that retains and flushes it (see <see cref="BpmnDesignerWrapper.SetPendingElementStats"/>).
    /// </summary>
    /// <remarks>
    /// A refresh can arrive before the canvas exists -- most notably the one unconditional refresh a freshly
    /// opened instance gets on load (see <c>DiagramDesignerWrapper.LoadActivityCoreAsync</c>), which runs during
    /// <c>OnInitializedAsync</c>, well before this designer's own <see cref="BpmnDesignerWrapper"/> has been
    /// created. Dropping that refresh instead of retaining it would mean a finished instance -- one that never
    /// gets a later observer-driven refresh to fall back on -- never gets its overlay at all. Only the latest
    /// value is kept, matching the semantics <see cref="UpdateElementStatsAsync"/> already documents.
    /// </remarks>
    private IReadOnlyDictionary<string, BpmnElementStats>? _pendingElementStats;

    /// <inheritdoc />
    public async Task LoadRootActivityAsync(JsonObject activity, IDictionary<string, ActivityStats>? activityStatsMap)
    {
        _rootActivity = activity;
        await InvokeDesignerActionAsync(x => x.LoadBpmnAsync(activity, _sourceXml, activityStatsMap));
    }

    /// <inheritdoc />
    /// <remarks>
    /// The canvas is read-only and has nothing to react to, so this keeps two copies of the in-memory
    /// root activity in step: the matching child activity node is replaced by id, wherever in the tree
    /// it is, so that <see cref="ReadRootActivityAsync"/> returns the edited tree, and the same
    /// replacement is forwarded to the mounted <see cref="BpmnDesignerWrapper"/> so its own held tree
    /// -- the one selection resolves from -- does not go stale. The canvas itself is left untouched.
    /// </remarks>
    public async Task UpdateActivityAsync(string id, JsonObject activity)
    {
        if (_rootActivity.GetId() == id)
            _rootActivity = (JsonObject)activity.DeepClone()!;
        else
            _rootActivity.ReplaceActivity(id, activity);

        await InvokeDesignerActionAsync(x => x.UpdateActivityAsync(id, activity));
    }

    /// <inheritdoc />
    public async Task UpdateActivityStatsAsync(string id, ActivityStats stats)
    {
        await InvokeDesignerActionAsync(x => x.UpdateActivityStatsAsync(id, stats));
    }

    /// <inheritdoc />
    public async Task SelectActivityAsync(string id)
    {
        await InvokeDesignerActionAsync(x => x.SelectActivityAsync(id));
    }

    /// <inheritdoc />
    public async Task UpdateElementStatsAsync(IReadOnlyDictionary<string, BpmnElementStats> elementStats)
    {
        _pendingElementStats = elementStats;
        await InvokeDesignerActionAsync(x => x.UpdateElementStatsAsync(elementStats));
    }

    /// <inheritdoc />
    /// <remarks>
    /// Returns the whole root activity JSON exactly as it was given and since edited by
    /// <see cref="UpdateActivityAsync"/>, never a projection rebuilt from the read-only view model:
    /// the view model is the canvas's own concern and the document it was built from is what this
    /// designer holds and hands back (D4).
    /// </remarks>
    public Task<JsonObject> ReadRootActivityAsync() => Task.FromResult(_rootActivity);

    /// <inheritdoc />
    public RenderFragment DisplayDesigner(DisplayContext context)
    {
        var activity = context.Activity;
        var sequence = 0;

        _rootActivity = activity;
        _sourceXml = GetSourceXml(context.WorkflowDefinition);
        _workflowDefinition = context.WorkflowDefinition;

        return builder =>
        {
            builder.OpenComponent<BpmnDesignerWrapper>(sequence++);
            builder.SetKey(_id);
            builder.AddAttribute(sequence++, nameof(BpmnDesignerWrapper.Activity), activity);
            builder.AddAttribute(sequence++, nameof(BpmnDesignerWrapper.SourceXml), _sourceXml);
            builder.AddAttribute(sequence++, nameof(BpmnDesignerWrapper.ActivityStats), context.ActivityStats);
            builder.AddAttribute(sequence++, nameof(BpmnDesignerWrapper.ActivitySelected), context.ActivitySelectedCallback);
            builder.AddAttribute(sequence++, nameof(BpmnDesignerWrapper.ActivityDoubleClick), context.ActivityDoubleClickCallback);
            builder.AddComponentReferenceCapture(sequence++, @ref =>
            {
                var isFirstMount = _designerWrapper == null;
                _designerWrapper = (BpmnDesignerWrapper)@ref;

                // Hand off the latest retained overlay the moment the wrapper exists, rather than only on the
                // next explicit UpdateElementStatsAsync call, which -- for a finished instance -- may never come
                // (see the remarks on _pendingElementStats). This is a synchronous field assignment, not an async
                // call: this callback is itself synchronous, so starting and discarding a task here would swallow
                // any exception it threw. BpmnDesignerWrapper's own awaited first-render flush is what actually
                // delivers the value once its canvas is ready.
                if (isFirstMount && _pendingElementStats != null)
                    _designerWrapper.SetPendingElementStats(_pendingElementStats);
            });

            builder.CloseComponent();
        };
    }

    /// <inheritdoc />
    public IEnumerable<RenderFragment> GetToolboxItems(bool isReadOnly)
    {
        // The React Flow adapter for BPMN does not exist yet (W9b), so there is no canvas to zoom or
        // center while it is in effect.
        if (designerOptions.Value.UseReactFlow)
            yield break;

        yield return DiagramDesignerToolbox.DisplayToolboxItem(localizer["Zoom to fit"], Icons.Material.Outlined.FitScreen, localizer["Zoom to fit the screen"], OnZoomToFitClicked);
        yield return DiagramDesignerToolbox.DisplayToolboxItem(localizer["Center"], Icons.Material.Filled.FilterCenterFocus, localizer["Center"], OnCenterClicked);
        yield return DiagramDesignerToolbox.DisplayToolboxItem(localizer["Export"], Icons.Material.Outlined.Download, localizer["Export as BPMN 2.0 XML"], OnExportClicked);
    }

    private async Task InvokeDesignerActionAsync(Func<BpmnDesignerWrapper, Task> action)
    {
        if (_designerWrapper != null)
            await action(_designerWrapper);
    }

    private Task OnZoomToFitClicked() => _designerWrapper != null ? _designerWrapper.ZoomToFitAsync() : Task.CompletedTask;
    private Task OnCenterClicked() => _designerWrapper != null ? _designerWrapper.CenterContentAsync() : Task.CompletedTask;

    private async Task OnExportClicked()
    {
        if (_workflowDefinition == null)
            return;

        var options = new DialogOptions
        {
            MaxWidth = MaxWidth.Small,
            FullWidth = true,
            CloseButton = true,
            CloseOnEscapeKey = true
        };

        var dialog = await dialogService.ShowAsync<ExportBpmnDialog>(localizer["Export BPMN"], options);
        var result = await dialog.Result;

        if (result?.Canceled is not false)
            return;

        var exportResult = await bpmnInterchangeService.ExportAsync(_workflowDefinition.DefinitionId);

        if (!exportResult.IsSuccess)
        {
            userMessageService.ShowSnackbarTextMessage(DescribeExportFailure(exportResult.Failure!), Severity.Error);
            return;
        }

        var download = exportResult.Success!;

        if (download.Content.CanSeek)
            download.Content.Seek(0, SeekOrigin.Begin);

        await files.DownloadFileFromStreamAsync(download.FileName, download.Content);
    }

    /// <summary>
    /// Renders the two 422 refusals in the user's own words, per this program's design; anything else falls back to
    /// the server's own message.
    /// </summary>
    private string DescribeExportFailure(BpmnExportFailure failure) => failure.Reason switch
    {
        BpmnExportFailureReason.NotFound => localizer["Definition not found."],
        BpmnExportFailureReason.NotImportedFromBpmn => localizer["This definition was not imported from BPMN."],
        BpmnExportFailureReason.DefinitionChangedSinceImport => localizer["The definition changed since import; export reflects the imported document only."],
        _ => localizer[failure.Message]
    };

    /// <summary>
    /// Reads the imported BPMN document's source XML from the workflow definition's custom
    /// properties, or null when there is none.
    /// </summary>
    private static string? GetSourceXml(WorkflowDefinition? workflowDefinition)
    {
        if (workflowDefinition == null || !workflowDefinition.CustomProperties.TryGetValue(BpmnProcessConstants.SourceXmlCustomPropertyKey, out var value) || value == null!)
            return null;

        return value switch
        {
            string s => s,
            JsonElement { ValueKind: JsonValueKind.String } element => element.GetString(),
            _ => null
        };
    }
}
