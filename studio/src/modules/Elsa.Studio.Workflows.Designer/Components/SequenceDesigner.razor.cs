using System.Text.Json;
using System.Text.Json.Nodes;
using Elsa.Api.Client.Extensions;
using Elsa.Api.Client.Resources.ActivityDescriptors.Enums;
using Elsa.Api.Client.Resources.ActivityDescriptors.Models;
using Elsa.Studio.Contracts;
using Elsa.Studio.Extensions;
using Elsa.Studio.Workflows.Designer.Contracts;
using Elsa.Studio.Workflows.Designer.Interop;
using Elsa.Studio.Workflows.Designer.Models;
using Elsa.Studio.Workflows.Designer.Services;
using Elsa.Studio.Workflows.Domain.Contracts;
using Elsa.Studio.Workflows.Domain.Extensions;
using Elsa.Studio.Workflows.Domain.Models;
using Elsa.Studio.Workflows.Extensions;
using Elsa.Studio.Workflows.UI.Args;
using Elsa.Studio.Workflows.UI.Models;
using Humanizer;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using ThrottleDebounce;

namespace Elsa.Studio.Workflows.Designer.Components;

/// <summary>
/// A constrained X6 designer for Sequence activities.
/// </summary>
public partial class SequenceDesigner : IDisposable, IAsyncDisposable
{
    private readonly string _containerId = $"sequence-container-{Guid.NewGuid():N}";
    private readonly PendingActionsQueue _pendingGraphActions;
    private readonly RateLimitedFunc<Task> _rateLimitedLoadSequenceAction;
    private DotNetObjectReference<SequenceDesigner>? _componentRef;
    private ISequenceMapper? _sequenceMapper;
    private JsonObject? _sequence;
    private IDictionary<string, ActivityStats>? _activityStats;
    private X6GraphApi _graphApi = null!;
    private IDisposable? _themeSubscription;

    public SequenceDesigner()
    {
        _pendingGraphActions = new(() => new(_graphApi != null!), () => Logger);
        _rateLimitedLoadSequenceAction = Debouncer.Debounce(
            async () => await InvokeAsync(async () => await LoadSequenceAsync(Sequence, ActivityStats)),
            TimeSpan.FromMilliseconds(100));
    }

    [Parameter] public JsonObject Sequence { get; set; } = null!;
    [Parameter] public IDictionary<string, ActivityStats>? ActivityStats { get; set; }
    [Parameter] public bool IsReadOnly { get; set; }
    [Parameter] public EventCallback<JsonObject> ActivitySelected { get; set; }
    [Parameter] public EventCallback<JsonObject> ActivityUpdated { get; set; }
    [Parameter] public EventCallback<JsonObject> ActivityDoubleClick { get; set; }
    [Parameter] public EventCallback<ActivityEmbeddedPortSelectedArgs> ActivityEmbeddedPortSelected { get; set; }
    [Parameter] public EventCallback CanvasSelected { get; set; }
    [Parameter] public EventCallback GraphUpdated { get; set; }

    [Inject] private DesignerJsInterop DesignerJsInterop { get; set; } = null!;
    [Inject] private IThemeService ThemeService { get; set; } = null!;
    [Inject] private IMapperFactory MapperFactory { get; set; } = null!;
    [Inject] private IActivityRegistry ActivityRegistry { get; set; } = null!;
    [Inject] private IIdentityGenerator IdentityGenerator { get; set; } = null!;
    [Inject] private IActivityNameGenerator ActivityNameGenerator { get; set; } = null!;
    [Inject] private ILogger<SequenceDesigner> Logger { get; set; } = null!;

    [JSInvokable]
    public async Task HandleActivitySelected(JsonObject activity)
    {
        if (ActivitySelected.HasDelegate)
            await ActivitySelected.InvokeAsync(activity);
    }

    [JSInvokable]
    public async Task HandleActivityDoubleClick(JsonObject activity)
    {
        if (ActivityDoubleClick.HasDelegate)
            await ActivityDoubleClick.InvokeAsync(activity);
    }

    [JSInvokable]
    public async Task HandleActivityEmbeddedPortSelected(JsonObject activity, string portName)
    {
        if (ActivityEmbeddedPortSelected.HasDelegate)
            await ActivityEmbeddedPortSelected.InvokeAsync(new ActivityEmbeddedPortSelectedArgs(activity, portName));
    }

    [JSInvokable]
    public async Task HandleCanvasSelected()
    {
        if (CanvasSelected.HasDelegate)
            await CanvasSelected.InvokeAsync();
    }

    [JSInvokable]
    public async Task HandleGraphUpdated()
    {
        if (GraphUpdated.HasDelegate)
            await GraphUpdated.InvokeAsync();
    }

    [JSInvokable]
    public async Task HandlePasteCellsRequested(X6ActivityNode[] activityNodes, X6Edge[] edges)
    {
        var allActivities = Sequence.GetActivities().ToList();
        var container = Sequence;

        foreach (var activityNode in activityNodes)
        {
            var activity = activityNode.Data;
            var descriptor = ActivityRegistry.Find(activity.GetTypeName(), activity.GetVersion())!;
            var newName = ActivityNameGenerator.GenerateNextName(allActivities, descriptor);

            RegenerateActivityIdentity(activity, container.GetNodeId(), descriptor, newName);
            activityNode.Id = activity.GetId();

            allActivities.Add(activity);
        }

        await ScheduleGraphActionAsync(() => _graphApi.PasteCellsAsync(activityNodes, []));
    }

    public async Task LoadSequenceAsync(JsonObject activity, IDictionary<string, ActivityStats>? activityStats)
    {
        Sequence = activity;
        ActivityStats = activityStats;
        _sequence = activity;
        _activityStats = activityStats;

        var mapper = await GetSequenceMapperAsync();
        var graph = mapper.Map(activity, activityStats);
        await ScheduleGraphActionAsync(() => _graphApi.LoadGraphAsync(graph));
    }

    public async Task<JsonObject> ReadSequenceAsync()
    {
        var serializerOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

        return await ScheduleGraphActionAsync(async () =>
        {
            var data = await _graphApi.ReadGraphAsync();
            var cells = data.GetProperty("cells").EnumerateArray();
            var nodes = cells.Where(x => x.TryGetProperty("shape", out var shape) && shape.GetString() == "elsa-activity")
                .Select(x => x.Deserialize<X6ActivityNode>(serializerOptions)!)
                .ToList();
            var graph = new X6Graph(nodes, [])
            {
                LayoutOrientation = data.TryGetProperty("layoutOrientation", out var orientation) ? orientation.GetString() : null
            };
            var mapper = await GetSequenceMapperAsync();
            return mapper.Map(Sequence, graph);
        });
    }

    public async Task AddActivityAsync(JsonObject activity)
    {
        var mapper = await MapperFactory.CreateActivityMapperAsync();
        var node = mapper.MapActivity(activity);
        await ScheduleGraphActionAsync(() => _graphApi.AddActivityNodeAsync(node));
    }

    public async Task UpdateActivityAsync(string id, JsonObject activity)
    {
        await ScheduleGraphActionAsync(() => _graphApi.UpdateActivityAsync(activity));

        if (ActivityUpdated.HasDelegate)
            await ActivityUpdated.InvokeAsync(activity);
    }

    public async Task UpdateActivityStatsAsync(string activityId, ActivityStats stats) =>
        await ScheduleGraphActionAsync(() => DesignerJsInterop.UpdateActivityStatsAsync($"activity-{activityId}", activityId, stats));

    public async Task SelectActivityAsync(string id) =>
        await ScheduleGraphActionAsync(() => _graphApi.SelectActivityAsync(id));

    public async Task ZoomToFitAsync() =>
        await ScheduleGraphActionAsync(() => _graphApi.ZoomToFitAsync());

    public async Task CenterContentAsync() =>
        await ScheduleGraphActionAsync(() => _graphApi.CenterContentAsync());

    public async Task AutoLayoutAsync()
    {
        var mapper = await GetSequenceMapperAsync();
        var graph = mapper.Map(Sequence, ActivityStats);
        await ScheduleGraphActionAsync(() => _graphApi.AutoLayoutAsync(graph));
    }

    public async Task SetLayoutOrientationAsync(string orientation) =>
        await ScheduleGraphActionAsync(() => _graphApi.SetSequenceOrientationAsync(orientation));

    public async Task MoveSelectedActivityAsync(int direction) =>
        await ScheduleGraphActionAsync(() => _graphApi.MoveSelectedSequenceNodeAsync(direction));

    protected override void OnInitialized()
    {
        _themeSubscription = new X6DesignerThemeSubscription(
            ThemeService,
            theme => ScheduleGraphActionAsync(() => _graphApi.ApplyThemeAsync(theme)));
        _sequence = Sequence;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender) return;

        _componentRef = DotNetObjectReference.Create(this);
        _graphApi = await DesignerJsInterop.CreateGraphAsync(_containerId, _componentRef, IsReadOnly, "sequence");
        await _pendingGraphActions.ProcessAsync();
        await LoadSequenceAsync(Sequence, ActivityStats);
    }

    protected override async Task OnParametersSetAsync()
    {
        var shouldReload = false;

        if (!Equals(_sequence, Sequence))
        {
            _sequence = Sequence;
            shouldReload = true;
        }

        if (!Equals(_activityStats, ActivityStats))
        {
            _activityStats = ActivityStats;
            shouldReload = true;
        }

        if (shouldReload)
            await _rateLimitedLoadSequenceAction.InvokeAsync();
    }

    private async Task<ISequenceMapper> GetSequenceMapperAsync() =>
        _sequenceMapper ??= await MapperFactory.CreateSequenceMapperAsync();

    private async Task ScheduleGraphActionAsync(Func<Task> action) =>
        await _pendingGraphActions.EnqueueAsync(action);

    private async Task<T> ScheduleGraphActionAsync<T>(Func<Task<T>> action) =>
        await _pendingGraphActions.EnqueueAsync(action);

    private void GenerateNewActivityIds(JsonObject container)
    {
        var activities = container.GetActivities().ToList();
        var newContainerId = IdentityGenerator.GenerateId();

        container.SetId(newContainerId);
        container.SetNodeId($"{container.GetNodeId()}:{newContainerId}");

        foreach (var activity in activities)
        {
            var descriptor = ActivityRegistry.Find(activity.GetTypeName(), activity.GetVersion())!;
            RegenerateActivityIdentity(activity, container.GetNodeId(), descriptor);
        }
    }

    private void RegenerateActivityIdentity(JsonObject activity, string parentNodeId, ActivityDescriptor descriptor, string? name = null)
    {
        var newActivityId = IdentityGenerator.GenerateId();

        activity.SetId(newActivityId);
        activity.SetNodeId($"{parentNodeId}:{newActivityId}");

        if (name is not null)
            activity.SetName(name);

        ProcessEmbeddedPorts(activity, descriptor);
    }

    private void ProcessEmbeddedPorts(JsonObject activity, ActivityDescriptor descriptor)
    {
        var embeddedPorts = descriptor.Ports.Where(x => x.Type == PortType.Embedded);
        foreach (var embeddedPort in embeddedPorts)
        {
            var camelName = embeddedPort.Name.Camelize();
            if (activity.TryGetPropertyValue(camelName, out var propValue) && propValue is JsonObject childActivity)
                GenerateNewActivityIds(childActivity);
        }
    }

    async ValueTask IAsyncDisposable.DisposeAsync()
    {
        if (_graphApi != null!)
            await _graphApi.DisposeGraphAsync();

        ((IDisposable)this).Dispose();
    }

    void IDisposable.Dispose()
    {
        _themeSubscription?.Dispose();
        _componentRef?.Dispose();
    }
}
