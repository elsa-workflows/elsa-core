using System.Text.Json.Nodes;
using Elsa.Studio.Workflows.Designer.Components;
using Elsa.Studio.Workflows.Designer.Models;
using Elsa.Studio.Workflows.Designer.Options;
using Elsa.Studio.Workflows.Domain.Models;
using Elsa.Studio.Workflows.Domain.Models.Bpmn;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Options;

namespace Elsa.Studio.Workflows.DiagramDesigners.Bpmn;

/// <summary>
/// A wrapper around the <see cref="Designer.Components.BpmnDesigner"/> component that switches
/// between the X6 canvas and, while <see cref="DesignerOptions.UseReactFlow"/> is in effect, a notice
/// that BPMN is not yet available on the React Flow canvas -- exactly as <c>FlowchartDesignerWrapper</c>
/// switches, except that dropping W9b's React Flow adapter here is a clear notice, not the JSON
/// fallback and not a crash.
/// </summary>
public partial class BpmnDesignerWrapper
{
    /// <summary>
    /// The name an editor cascades its receiver of BPMN element selections under (see <see cref="ElementSelected"/>).
    /// </summary>
    public const string ElementSelectedCascadeName = "BpmnElementSelected";

    /// <summary>
    /// The root <c>Elsa.BpmnProcess</c> activity to display.
    /// </summary>
    [Parameter] public JsonObject Activity { get; set; } = null!;

    /// <summary>
    /// The imported BPMN document's source XML, for BPMN DI geometry.
    /// </summary>
    [Parameter] public string? SourceXml { get; set; }

    /// <summary>
    /// A map of activity stats.
    /// </summary>
    [Parameter] public IDictionary<string, ActivityStats>? ActivityStats { get; set; }

    /// <summary>
    /// An event raised when an activity is selected.
    /// </summary>
    [Parameter] public EventCallback<JsonObject> ActivitySelected { get; set; }

    /// <summary>
    /// An event raised when an activity is double-clicked.
    /// </summary>
    [Parameter] public EventCallback<JsonObject> ActivityDoubleClick { get; set; }

    /// <summary>
    /// Receives the selected BPMN element itself, or <see langword="null"/> when nothing in particular is selected. An
    /// editor that edits bindings — which has to know which element was clicked even when no activity is bound to it —
    /// cascades one under <see cref="ElementSelectedCascadeName"/>; the viewers do not, so this stays unset there.
    /// </summary>
    [CascadingParameter(Name = ElementSelectedCascadeName)] public EventCallback<BpmnElementSelection?> ElementSelected { get; set; }

    [Inject] private IOptions<DesignerOptions> DesignerOptions { get; set; } = null!;

    private BpmnDesigner? Designer { get; set; }
    private bool UseReactFlow => DesignerOptions.Value.UseReactFlow;

    /// <summary>
    /// The latest element-keyed instance overlay handed to <see cref="UpdateElementStatsAsync"/>, retained so it
    /// can be applied on <see cref="Designer"/> as soon as it exists (see <see cref="OnAfterRenderAsync"/>). Mirrors
    /// <c>BpmnDiagramDesigner._pendingElementStats</c> one level up, since <see cref="Designer"/> can still be null
    /// when a call arrives just after this wrapper itself has mounted.
    /// </summary>
    private IReadOnlyDictionary<string, BpmnElementStats>? _pendingElementStats;

    /// <summary>
    /// Whether the scope being displayed has nothing to draw: no <c>process</c> payload at all, or one that declares
    /// no elements. That is what a <c>BpmnProcess</c> added from the toolbox looks like, and what a scope whose
    /// import produced nothing looks like.
    /// </summary>
    /// <remarks>
    /// Read from the <see cref="Activity"/> parameter rather than from the field <see cref="LoadBpmnAsync"/> writes,
    /// so the notice is decided by the same render pass that would otherwise mount the canvas. The wrapper is
    /// re-created per displayed segment (<c>BpmnDiagramDesigner.DisplayDesigner</c> keys it on the designer
    /// instance), so the parameter is always the scope currently being shown.
    /// </remarks>
    private bool IsEmptyScope => GetElementCount(Activity) == 0;

    private static int GetElementCount(JsonObject? activity) =>
        activity?["process"] is JsonObject process && process["elements"] is JsonArray elements ? elements.Count : 0;

    /// <summary>
    /// Loads the specified root activity into the designer.
    /// </summary>
    public async Task LoadBpmnAsync(JsonObject activity, string? sourceXml, IDictionary<string, ActivityStats>? activityStats)
    {
        Activity = activity;
        SourceXml = sourceXml;
        ActivityStats = activityStats;

        if (Designer != null)
            await Designer.LoadBpmnAsync(activity, sourceXml, activityStats);
    }

    /// <summary>
    /// Keeps the underlying designer's held activity tree in step with an edit made elsewhere (the
    /// properties panel), without touching the canvas. See <see cref="BpmnDesigner.UpdateActivityAsync"/>.
    /// </summary>
    public async Task UpdateActivityAsync(string id, JsonObject activity)
    {
        if (Designer != null)
            await Designer.UpdateActivityAsync(id, activity);
    }

    /// <summary>
    /// Updates the stats of the specified activity.
    /// </summary>
    public async Task UpdateActivityStatsAsync(string id, ActivityStats stats)
    {
        if (Designer != null)
            await Designer.UpdateActivityStatsAsync(id, stats);
    }

    /// <summary>
    /// Updates the element-keyed instance overlay (gateways, events and sequence flows).
    /// </summary>
    public async Task UpdateElementStatsAsync(IReadOnlyDictionary<string, BpmnElementStats> elementStats)
    {
        _pendingElementStats = elementStats;

        if (Designer != null)
            await Designer.UpdateElementStatsAsync(elementStats);
    }

    /// <summary>
    /// Synchronously stakes the latest element-stats overlay for <see cref="OnAfterRenderAsync"/> to flush once
    /// <see cref="Designer"/> exists, without going through <see cref="UpdateElementStatsAsync"/>'s async call.
    /// </summary>
    /// <remarks>
    /// This is what lets <see cref="BpmnDiagramDesigner"/> hand a retained overlay to this wrapper the moment it is
    /// captured -- from inside a synchronous component-reference-capture callback, where starting and discarding an
    /// async call would swallow any exception it threw.
    /// </remarks>
    internal void SetPendingElementStats(IReadOnlyDictionary<string, BpmnElementStats> elementStats) => _pendingElementStats = elementStats;

    /// <summary>
    /// Applies a retained element-stats overlay that arrived before <see cref="Designer"/> existed, the moment it
    /// does -- the same "drain pending work on first render" shape <see cref="Designer"/> itself uses for its own
    /// initial <c>LoadBpmnAsync</c> call.
    /// </summary>
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender && Designer != null && _pendingElementStats != null)
            await Designer.UpdateElementStatsAsync(_pendingElementStats);
    }

    /// <summary>
    /// Selects the element bound to the specified activity.
    /// </summary>
    public async Task SelectActivityAsync(string id)
    {
        if (Designer != null)
            await Designer.SelectActivityAsync(id);
    }

    /// <summary>
    /// Zooms the designer to fit the content.
    /// </summary>
    public async Task ZoomToFitAsync()
    {
        if (Designer != null)
            await Designer.ZoomToFitAsync();
    }

    /// <summary>
    /// Centers the content of the designer.
    /// </summary>
    public async Task CenterContentAsync()
    {
        if (Designer != null)
            await Designer.CenterContentAsync();
    }

    private async Task OnCanvasSelected()
    {
        if (ActivitySelected.HasDelegate)
            await ActivitySelected.InvokeAsync(Activity);
    }
}
