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
using Elsa.Studio.Workflows.UI.Contracts;
using Elsa.Studio.Workflows.UI.Models;
using Humanizer;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using ThrottleDebounce;

namespace Elsa.Studio.Workflows.Designer.Components;

/// <summary>
/// A constrained React Flow designer for Sequence activities.
/// </summary>
public partial class SequenceFlowDesigner : IAsyncDisposable
{
    private readonly string _containerId = $"sequence-rf-container-{Guid.NewGuid():N}";
    private readonly RateLimitedFunc<Task> _rateLimitedLoadSequenceAction;
    private DotNetObjectReference<SequenceFlowDesigner>? _componentRef;
    private ISequenceMapper? _sequenceMapper;
    private ReactFlowGraphApi? _graphApi;
    private JsonObject? _sequence;
    private IDictionary<string, ActivityStats>? _activityStats;

    public SequenceFlowDesigner()
    {
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

    [Inject] private ReactFlowJsInterop JsInterop { get; set; } = null!;
    [Inject] private IMapperFactory MapperFactory { get; set; } = null!;
    [Inject] private IActivityRegistry ActivityRegistry { get; set; } = null!;
    [Inject] private IIdentityGenerator IdentityGenerator { get; set; } = null!;
    [Inject] private IActivityNameGenerator ActivityNameGenerator { get; set; } = null!;
    [Inject] private IActivityDisplaySettingsRegistry ActivityDisplaySettingsRegistry { get; set; } = null!;

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
        if (_graphApi is null) return;

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

        await _graphApi.PasteCellsAsync(activityNodes, []);
    }

    public async Task LoadSequenceAsync(JsonObject activity, IDictionary<string, ActivityStats>? activityStats)
    {
        Sequence = activity;
        ActivityStats = activityStats;
        _sequence = activity;
        _activityStats = activityStats;

        if (_graphApi is null) return;

        var mapper = await GetSequenceMapperAsync();
        var graph = mapper.Map(activity, activityStats);
        await _graphApi.LoadGraphAsync(graph);
    }

    public async Task<JsonObject> ReadSequenceAsync()
    {
        if (_graphApi is null) return Sequence;

        var serializerOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
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
    }

    public async Task ZoomToFitAsync()
    {
        if (_graphApi is not null)
            await _graphApi.ZoomToFitAsync();
    }

    public async Task CenterContentAsync()
    {
        if (_graphApi is not null)
            await _graphApi.CenterContentAsync();
    }

    public async Task AutoLayoutAsync()
    {
        if (_graphApi is not null)
            await _graphApi.AutoLayoutAsync();
    }

    public async Task SetLayoutOrientationAsync(string orientation)
    {
        if (_graphApi is not null)
            await _graphApi.SetSequenceOrientationAsync(orientation);
    }

    public async Task MoveSelectedActivityAsync(int direction)
    {
        if (_graphApi is not null)
            await _graphApi.MoveSelectedSequenceNodeAsync(direction);
    }

    public async Task SelectActivityAsync(string id)
    {
        if (_graphApi is not null)
            await _graphApi.SelectActivityAsync(id);
    }

    public async Task UpdateActivityAsync(string id, JsonObject activity)
    {
        if (_graphApi is null) return;
        var mapper = await MapperFactory.CreateActivityMapperAsync();
        var node = mapper.MapActivity(activity);
        await _graphApi.UpdateActivityAsync(node);
        await NotifyActivityUpdatedAsync(activity);
    }

    public async Task UpdateActivityStatsAsync(string activityId, ActivityStats stats)
    {
        if (_graphApi is not null)
            await _graphApi.UpdateActivityStatsAsync(activityId, stats);
    }

    public async Task AddActivityAsync(JsonObject activity)
    {
        if (_graphApi is null) return;
        var mapper = await MapperFactory.CreateActivityMapperAsync();
        var node = mapper.MapActivity(activity);
        await _graphApi.AddActivityNodeAsync(node);
    }

    [JSInvokable]
    public async Task<IList<ActivityDescriptorDto>> GetAvailableActivities()
    {
        await ActivityRegistry.EnsureLoadedAsync();
        return ActivityRegistry.ListBrowsable().Select(d =>
        {
            var settings = ActivityDisplaySettingsRegistry.GetSettings(d.TypeName);
            return new ActivityDescriptorDto(
                d.TypeName,
                d.Version,
                d.Name,
                d.DisplayName ?? d.Name,
                d.Category,
                d.Description,
                settings.Color,
                settings.Icon);
        })
        .OrderBy(d => d.Category)
        .ThenBy(d => d.DisplayName)
        .ToList();
    }

    [JSInvokable]
    public async Task<JsonObject?> AddActivityAtPosition(string typeName, int version, double x, double y)
    {
        if (_graphApi is null) return null;

        await ActivityRegistry.EnsureLoadedAsync();
        var descriptor = ActivityRegistry.Find(typeName, version);
        if (descriptor == null) return null;

        var newActivity = SequenceActivityFactory.CreateActivity(Sequence, descriptor, IdentityGenerator, ActivityNameGenerator, x, y);
        await AddActivityAsync(newActivity);
        await NotifyActivityUpdatedAsync(newActivity);
        return newActivity;
    }

    protected override void OnInitialized() => _sequence = Sequence;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender) return;

        _componentRef = DotNetObjectReference.Create(this);
        _graphApi = await JsInterop.CreateGraphAsync(_containerId, _componentRef, IsReadOnly, "sequence");
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

    private async Task NotifyActivityUpdatedAsync(JsonObject activity)
    {
        if (ActivityUpdated.HasDelegate)
            await ActivityUpdated.InvokeAsync(activity);
    }

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

    public async ValueTask DisposeAsync()
    {
        if (_graphApi is not null)
            await _graphApi.DisposeGraphAsync();

        _componentRef?.Dispose();
    }
}
