using System.Text.Json;
using Elsa.Studio.Contracts;
using Elsa.Studio.Workflows.Designer.Interop;
using Elsa.Studio.Workflows.Designer.Models;
using Elsa.Studio.Workflows.Designer.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

namespace Elsa.Studio.Workflows.Designer.Components;

/// <summary>
/// Native X6 visual surface for State Machine states and transitions.
/// </summary>
public partial class StateMachineCanvas : IDisposable, IAsyncDisposable
{
    private readonly string _containerId = $"state-machine-container-{Guid.NewGuid():N}";
    private readonly PendingActionsQueue _pendingActions;
    private DotNetObjectReference<StateMachineCanvas>? _componentRef;
    private X6GraphApi _graphApi = null!;
    private IDisposable? _themeSubscription;
    private StateMachineCanvasGraph? _loadedGraph;
    private string? _loadedSelectedVisualId;

    public StateMachineCanvas()
    {
        _pendingActions = new(() => new(_graphApi != null!), () => Logger);
    }

    [Parameter, EditorRequired] public StateMachineCanvasGraph Graph { get; set; } = null!;
    [Parameter] public string? SelectedVisualId { get; set; }
    [Parameter] public bool IsReadOnly { get; set; }
    [Parameter] public EventCallback<string> StateSelected { get; set; }
    [Parameter] public EventCallback<string> TransitionSelected { get; set; }
    [Parameter] public EventCallback CanvasSelected { get; set; }
    [Parameter] public EventCallback<JsonElement> GraphChanged { get; set; }
    [Parameter] public EventCallback<StateMachineDeleteRequest> DeleteRequested { get; set; }

    [Inject] private DesignerJsInterop DesignerJsInterop { get; set; } = null!;
    [Inject] private IThemeService ThemeService { get; set; } = null!;
    [Inject] private ILogger<StateMachineCanvas> Logger { get; set; } = null!;

    [JSInvokable]
    public Task HandleStateMachineStateSelected(string visualId) => StateSelected.InvokeAsync(visualId);

    [JSInvokable]
    public Task HandleStateMachineTransitionSelected(string visualId) => TransitionSelected.InvokeAsync(visualId);

    [JSInvokable]
    public Task HandleCanvasSelected() => CanvasSelected.InvokeAsync();

    [JSInvokable]
    public async Task HandleGraphUpdated()
    {
        if (!GraphChanged.HasDelegate)
            return;

        var graph = await _graphApi.ReadGraphAsync();
        await GraphChanged.InvokeAsync(graph);
    }

    [JSInvokable]
    public Task HandleStateMachineDeleteRequested(string kind, string visualId) =>
        DeleteRequested.InvokeAsync(new(kind, visualId));

    public Task SelectCellAsync(string visualId, bool center = false) =>
        ScheduleAsync(() => _graphApi.SelectCellAsync(visualId, center));

    public Task ZoomToFitAsync() => ScheduleAsync(() => _graphApi.ZoomToFitAsync());

    public Task CenterContentAsync() => ScheduleAsync(() => _graphApi.CenterContentAsync());

    public Task AutoLayoutAsync() => IsReadOnly
        ? Task.CompletedTask
        : ScheduleAsync(() => _graphApi.AutoLayoutAsync(StateMachineX6Mapper.Map(Graph)));

    public Task ReloadAsync() => LoadGraphAsync(Graph);

    protected override void OnInitialized()
    {
        _themeSubscription = new X6DesignerThemeSubscription(
            ThemeService,
            theme => ScheduleAsync(() => _graphApi.ApplyThemeAsync(theme)));
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender)
            return;

        _componentRef = DotNetObjectReference.Create(this);
        _graphApi = await DesignerJsInterop.CreateGraphAsync(_containerId, _componentRef, IsReadOnly, "stateMachine");
        await _pendingActions.ProcessAsync();
        await LoadGraphAsync(Graph);
        if (SelectedVisualId != null)
            await _graphApi.SelectCellAsync(SelectedVisualId);
    }

    protected override async Task OnParametersSetAsync()
    {
        if (_graphApi == null!)
        {
            _loadedGraph = Graph;
            _loadedSelectedVisualId = SelectedVisualId;
            return;
        }

        var graphChanged = !ReferenceEquals(_loadedGraph, Graph);
        var selectionChanged = !string.Equals(_loadedSelectedVisualId, SelectedVisualId, StringComparison.Ordinal);
        _loadedSelectedVisualId = SelectedVisualId;

        if (graphChanged)
            await LoadGraphAsync(Graph);

        if ((graphChanged || selectionChanged) && SelectedVisualId != null)
            await ScheduleAsync(() => _graphApi.SelectCellAsync(SelectedVisualId));
    }

    private Task LoadGraphAsync(StateMachineCanvasGraph graph)
    {
        _loadedGraph = graph;
        return ScheduleAsync(() => _graphApi.LoadGraphAsync(StateMachineX6Mapper.Map(graph)));
    }

    private Task ScheduleAsync(Func<Task> action) => _pendingActions.EnqueueAsync(action);

    async ValueTask IAsyncDisposable.DisposeAsync()
    {
        if (_graphApi != null!)
            await _graphApi.DisposeGraphAsync();

        Dispose();
    }

    public void Dispose()
    {
        _themeSubscription?.Dispose();
        _componentRef?.Dispose();
    }
}

public sealed record StateMachineDeleteRequest(string Kind, string VisualId);
