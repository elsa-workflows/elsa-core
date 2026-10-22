using System.Text.Json.Nodes;
using Elsa.Studio.Bpmn.Components;
using Elsa.Studio.Bpmn.Designers;
using Elsa.Studio.Workflows.Domain.Models;
using Elsa.Studio.Workflows.Domain.Models.Bpmn;
using Elsa.Studio.Workflows.Extensions;
using Microsoft.AspNetCore.Components;

namespace Elsa.Studio.Workflows.DiagramDesigners.Bpmn;

/// <summary>
/// The enabled BPMN designer showing an imported workflow's document read-only, as the definition and instance viewers
/// display it: it shows each element's instance state through the designer's <c>applyOverlays</c>, and reports a
/// selected element as the activity bound to it.
/// </summary>
/// <remarks>
/// The designer shows the whole document, subprocess bodies included, so it never needs a nested scope of its own.
/// </remarks>
public partial class BpmnViewerCanvas
{
    private readonly Dictionary<string, ActivityStats> _activityStats = new(StringComparer.Ordinal);
    private readonly SemaphoreSlim _loadGate = new(1, 1);
    private BpmnCanvasHost _host = null!;
    private BpmnElementActivityMap _map = null!;
    private JsonObject? _mappedActivity;
    private IDictionary<string, ActivityStats>? _seededStats;
    private IReadOnlyDictionary<string, BpmnElementStats> _elementStats = new Dictionary<string, BpmnElementStats>();
    private string? _loadedXml;
    private bool _isLoaded;
    private bool _rendered;
    private string? _selectedElementId;
    private string? _requestedElementId;

    /// <summary>The root <c>Elsa.BpmnProcess</c> activity the document was imported into, which names the element each activity is bound to.</summary>
    [Parameter, EditorRequired] public JsonObject Activity { get; set; } = null!;

    /// <summary>The imported document to show.</summary>
    [Parameter, EditorRequired] public string SourceXml { get; set; } = null!;

    /// <summary>The state of each bound activity, keyed by activity id or node id.</summary>
    [Parameter] public IDictionary<string, ActivityStats>? ActivityStats { get; set; }

    /// <summary>Raised with the activity bound to the selected element, or with the scope that holds an element bound to none.</summary>
    [Parameter] public EventCallback<JsonObject> ActivitySelected { get; set; }

    [Inject] private BpmnDesignerResolver DesignerResolver { get; set; } = null!;

    /// <summary>The designer to show. The wrapper shows this canvas only when the resolver has one, so it is never null here.</summary>
    private BpmnDesignerDescriptor Designer => DesignerResolver.Resolve()!;

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        if (!ReferenceEquals(_mappedActivity, Activity))
        {
            _map = BpmnElementActivityMap.From(Activity);
            _mappedActivity = Activity;
        }

        // A new stats map replaces what updates added since; the same one handed in again keeps them.
        if (!ReferenceEquals(_seededStats, ActivityStats))
        {
            _seededStats = ActivityStats;
            _activityStats.Clear();

            foreach (var (key, stats) in ActivityStats ?? new Dictionary<string, ActivityStats>())
                _activityStats[key] = stats;
        }
    }

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender)
            return;

        _rendered = true;
        await LoadAsync();
    }

    /// <summary>Shows <paramref name="sourceXml"/> for <paramref name="activity"/>, with <paramref name="activityStats"/> as each bound activity's state.</summary>
    public async Task LoadBpmnAsync(JsonObject activity, string sourceXml, IDictionary<string, ActivityStats>? activityStats)
    {
        Activity = activity;
        SourceXml = sourceXml;
        ActivityStats = activityStats;
        OnParametersSet();

        if (_rendered)
            await LoadAsync();
    }

    /// <summary>Shows <paramref name="stats"/> as the state of the activity <paramref name="activityId"/>.</summary>
    public Task UpdateActivityStatsAsync(string activityId, ActivityStats stats)
    {
        _activityStats[activityId] = stats;
        return ApplyOverlaysAsync();
    }

    /// <summary>Shows <paramref name="elementStats"/> as the state of gateways, events and sequence flows.</summary>
    public Task UpdateElementStatsAsync(IReadOnlyDictionary<string, BpmnElementStats> elementStats)
    {
        _elementStats = elementStats;
        return ApplyOverlaysAsync();
    }

    /// <summary>
    /// Selects the element bound to the activity <paramref name="activityId"/>; an activity bound to none selects nothing.
    /// The selection this raises is not reported back, since the caller already knows what it asked for.
    /// </summary>
    public async Task SelectActivityAsync(string activityId)
    {
        if (!_isLoaded || _map.ElementIdOf(activityId) is not { } elementId || elementId == _selectedElementId)
            return;

        _requestedElementId = elementId;

        if (!await _host.SelectAsync(elementId))
            _requestedElementId = null;
    }

    /// <summary>
    /// Loads the document unless the designer already shows it, then shows the state held so far. State that arrives
    /// while the document loads is held, and shown once it has. Loads run one at a time, so a document's import can
    /// never finish after that of a document asked for later.
    /// </summary>
    private async Task LoadAsync()
    {
        await _loadGate.WaitAsync();

        try
        {
            if (_loadedXml != SourceXml)
            {
                var xml = SourceXml;
                _isLoaded = false;
                _loadedXml = xml;

                // The designer raises no selection event while it loads, so the selection it had is forgotten here.
                _selectedElementId = null;
                _requestedElementId = null;
                BpmnDesignerLoadResult result;

                try
                {
                    result = await _host.LoadAsync(xml);
                }
                catch (Exception exception) when (exception is not OperationCanceledException)
                {
                    // The host shows why the designer could not be created; there is nothing to show state on.
                    return;
                }

                // A newer document asked for while this one loaded waits its turn, and shows the state once it has loaded.
                if (!result.Imported || SourceXml != xml)
                {
                    return;
                }

                _isLoaded = true;
            }

            await ApplyOverlaysAsync();
        }
        finally
        {
            _loadGate.Release();
        }
    }

    private async Task ApplyOverlaysAsync()
    {
        if (_isLoaded)
            await _host.ApplyOverlaysAsync(_map.Overlay(_activityStats, _elementStats));
    }

    private async Task OnSelectionChanged(BpmnDesignerSelection selection)
    {
        _selectedElementId = selection.Primary?.Id;
        var isRequested = _requestedElementId != null && _requestedElementId == _selectedElementId;
        _requestedElementId = null;

        if (isRequested)
            return;

        var activityId = _selectedElementId != null ? _map.ActivityIdOf(_selectedElementId) : null;
        var activity = activityId != null ? Activity.FindActivity(activityId) : null;

        await ActivitySelected.InvokeAsync(activity ?? Activity);
    }
}
