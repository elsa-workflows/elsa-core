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
using Elsa.Studio.Workflows.Domain.Contracts;
using Elsa.Studio.Workflows.Domain.Extensions;
using Elsa.Studio.Workflows.Domain.Models;
using Elsa.Studio.Workflows.Extensions;
using Elsa.Studio.Workflows.UI.Args;
using Elsa.Studio.Workflows.UI.Contracts;
using Humanizer;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using ThrottleDebounce;

namespace Elsa.Studio.Workflows.Designer.Components;

/// <summary>
/// A Blazor flowchart designer rendered with React Flow. Coexists with
/// <see cref="FlowchartDesigner"/>; pages can opt in by swapping the tag.
/// </summary>
public partial class ReactFlowDesigner : IAsyncDisposable
{
    private readonly string _containerId = $"rf-container-{Guid.NewGuid():N}";
    private DotNetObjectReference<ReactFlowDesigner>? _componentRef;
    private IFlowchartMapper? _flowchartMapper;
    private ReactFlowGraphApi? _graphApi;
    private JsonObject? _flowchart;
    private IDictionary<string, ActivityStats>? _activityStats;
    private readonly RateLimitedFunc<Task> _rateLimitedLoadFlowchartAction;

    public ReactFlowDesigner()
    {
        _rateLimitedLoadFlowchartAction = Debouncer.Debounce(
            async () => await InvokeAsync(async () => await LoadFlowchartAsync(Flowchart, ActivityStats)),
            TimeSpan.FromMilliseconds(100));
    }

    [Parameter] public JsonObject Flowchart { get; set; } = null!;
    [Parameter] public IDictionary<string, ActivityStats>? ActivityStats { get; set; }
    [Parameter] public bool IsReadOnly { get; set; }
    [Parameter] public EventCallback<JsonObject> ActivitySelected { get; set; }
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

    /// <summary>
    /// Receives the cells the user copied/cut from the React Flow clipboard,
    /// generates fresh activity IDs/names, recursively rewires embedded ports,
    /// then asks the JS side to add the new cells. Mirrors
    /// <see cref="FlowchartDesigner.HandlePasteCellsRequested"/>.
    /// </summary>
    [JSInvokable]
    public async Task HandlePasteCellsRequested(X6ActivityNode[] activityNodes, X6Edge[] edges)
    {
        if (_graphApi is null) return;

        var allActivities = Flowchart.GetActivities().ToList();
        var activityLookup = new Dictionary<string, X6ActivityNode>();
        var container = Flowchart;

        foreach (var activityNode in activityNodes)
        {
            var activity = activityNode.Data;
            var activityType = activity.GetTypeName();
            var activityVersion = activity.GetVersion();
            var descriptor = ActivityRegistry.Find(activityType, activityVersion)!;
            var newActivityId = IdentityGenerator.GenerateId();
            var newName = ActivityNameGenerator.GenerateNextName(allActivities, descriptor);

            activityLookup[activityNode.Id] = activityNode;

            activity.SetId(newActivityId);
            activity.SetNodeId($"{container.GetNodeId()}:{newActivityId}");
            activity.SetName(newName);
            activityNode.Id = newActivityId;

            allActivities.Add(activity);

            // Nudge the pasted node so it doesn't sit exactly on top of the original.
            var designerMetadata = activity.GetDesignerMetadata();
            designerMetadata.Position.X = activityNode.Position.X + 64;
            designerMetadata.Position.Y = activityNode.Position.Y + 64;
            activityNode.Position.X = designerMetadata.Position.X;
            activityNode.Position.Y = designerMetadata.Position.Y;
            activity.SetDesignerMetadata(designerMetadata);

            ProcessEmbeddedPorts(activity, descriptor);
        }

        foreach (var edge in edges)
        {
            if (!activityLookup.TryGetValue(edge.Source.Cell, out var sourceActivity)) continue;
            if (!activityLookup.TryGetValue(edge.Target.Cell, out var targetActivity)) continue;
            edge.Source.Cell = sourceActivity.Data.GetId();
            edge.Target.Cell = targetActivity.Data.GetId();
        }

        await _graphApi.PasteCellsAsync(activityNodes, edges);
    }

    public async Task LoadFlowchartAsync(JsonObject activity, IDictionary<string, ActivityStats>? activityStats)
    {
        Flowchart = activity;
        ActivityStats = activityStats;
        // Keep the OnParametersSetAsync diff fields in sync so the parameter
        // change that bubbles back through Blazor's render doesn't schedule
        // a redundant debounced reload (which would re-fitView and make the
        // canvas look like it "refreshed" on every click).
        _flowchart = activity;
        _activityStats = activityStats;

        if (_graphApi is null) return;

        var mapper = await GetFlowchartMapperAsync();
        var container = activity.FindActivitiesContainer() ?? CreateSyntheticContainer(activity);
        var graph = mapper.Map(container, activityStats);
        await _graphApi.LoadGraphAsync(graph);
    }

    public async Task ZoomToFitAsync()
    {
        if (_graphApi is not null) await _graphApi.ZoomToFitAsync();
    }

    public async Task CenterContentAsync()
    {
        if (_graphApi is not null) await _graphApi.CenterContentAsync();
    }

    public async Task SelectActivityAsync(string id)
    {
        if (_graphApi is not null) await _graphApi.SelectActivityAsync(id);
    }

    public async Task UpdateActivityAsync(string id, JsonObject activity)
    {
        if (_graphApi is null) return;
        var mapper = await MapperFactory.CreateActivityMapperAsync();
        var node = mapper.MapActivity(activity);
        // Preserve the existing identity even if the caller renamed the activity:
        // the caller passes the new id via `activity` already, so the mapped node
        // carries the right id.
        await _graphApi.UpdateActivityAsync(node);
    }

    public async Task UpdateActivityStatsAsync(string activityId, ActivityStats stats)
    {
        if (_graphApi is null) return;
        await _graphApi.UpdateActivityStatsAsync(activityId, stats);
    }

    /// <summary>
    /// Reads the current graph state from the React designer and converts it
    /// back into a flowchart JSON via the same <see cref="IFlowchartMapper"/>
    /// that <see cref="FlowchartDesigner"/> uses, so saving works identically.
    /// </summary>
    public async Task<JsonObject> ReadFlowchartAsync()
    {
        if (_graphApi is null) return Flowchart;

        var serializerOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        var data = await _graphApi.ReadGraphAsync();
        var cells = data.GetProperty("cells").EnumerateArray();
        var nodes = cells.Where(x => x.GetProperty("shape").GetString() == "elsa-activity")
            .Select(x => x.Deserialize<X6ActivityNode>(serializerOptions)!)
            .ToList();
        var edges = cells.Where(x => x.GetProperty("shape").GetString() == "elsa-edge")
            .Select(x => x.Deserialize<X6Edge>(serializerOptions)!)
            .ToList();
        var graph = new X6Graph(nodes, edges);
        var mapper = await GetFlowchartMapperAsync();
        var exportedFlowchart = mapper.Map(graph);
        var activities = exportedFlowchart.GetActivities();
        var connections = exportedFlowchart.GetConnections();

        var flowchart = Flowchart;
        flowchart.SetProperty(activities, "activities");
        flowchart.SetProperty(connections.SerializeToNode(), "connections");
        return flowchart;
    }

    public async Task AutoLayoutAsync(JsonObject activity, IDictionary<string, ActivityStats>? activityStats)
    {
        if (_graphApi is null) return;
        // Run the dagre layout on the current React Flow state. The C# Flowchart
        // parameter is accepted for parity with FlowchartDesigner but unused —
        // the JS side already has the live nodes/edges, including unsaved edits.
        _ = activity;
        _ = activityStats;
        await _graphApi.AutoLayoutAsync();
    }

    public async Task AddActivityAsync(JsonObject activity)
    {
        if (_graphApi is null) return;
        var mapper = await MapperFactory.CreateActivityMapperAsync();
        var node = mapper.MapActivity(activity);
        await _graphApi.AddActivityNodeAsync(node);
    }

    /// <summary>
    /// Returns the activity descriptors that should appear in the inline
    /// "drag-out → pick activity" picker. Mirrors the data the toolbox uses,
    /// trimmed for JSInterop payload size.
    /// </summary>
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

    /// <summary>
    /// Creates a new activity at the given page position (matches the drag-drop
    /// flow's coordinate space) and pushes it to the React Flow side. Returns
    /// the new activity so the JS caller can wire up an edge from the source
    /// port to the new node's "In" handle.
    /// </summary>
    [JSInvokable]
    public async Task<JsonObject?> AddActivityAtPosition(string typeName, int version, double x, double y)
    {
        if (_graphApi is null) return null;

        await ActivityRegistry.EnsureLoadedAsync();
        var descriptor = ActivityRegistry.Find(typeName, version);
        if (descriptor == null) return null;

        var allActivities = Flowchart?.GetActivities().ToList() ?? new List<JsonObject>();
        var newActivityId = IdentityGenerator.GenerateId();
        var newName = ActivityNameGenerator.GenerateNextName(allActivities, descriptor);

        var newActivity = new JsonObject(new Dictionary<string, JsonNode?>
        {
            ["id"] = newActivityId,
            ["nodeId"] = $"{Flowchart?.GetNodeId() ?? string.Empty}:{newActivityId}",
            ["name"] = newName,
            ["type"] = descriptor.TypeName,
            ["version"] = descriptor.Version,
        });

        newActivity.SetDesignerMetadata(new()
        {
            Position = new(x, y),
        });

        foreach (var property in descriptor.ConstructionProperties)
        {
            var valueNode = JsonSerializer.SerializeToNode(property.Value);
            var propertyName = property.Key.Camelize();
            newActivity.SetProperty(valueNode, propertyName);
        }

        if (descriptor.Kind == ActivityKind.Trigger && allActivities.All(activity => activity.GetCanStartWorkflow() != true))
            newActivity.SetCanStartWorkflow(true);

        await AddActivityAsync(newActivity);
        return newActivity;
    }

    protected override void OnInitialized() => _flowchart = Flowchart;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _componentRef = DotNetObjectReference.Create(this);
            _graphApi = await JsInterop.CreateGraphAsync(_containerId, _componentRef, IsReadOnly);
            await LoadFlowchartAsync(Flowchart, ActivityStats);
        }
    }

    protected override async Task OnParametersSetAsync()
    {
        if (!Equals(_flowchart, Flowchart) && _flowchart?.GetNodeId() != Flowchart.GetNodeId())
        {
            _flowchart = Flowchart;
            await _rateLimitedLoadFlowchartAction.InvokeAsync();
        }

        if (!Equals(_activityStats, ActivityStats))
        {
            _activityStats = ActivityStats;
            await _rateLimitedLoadFlowchartAction.InvokeAsync();
        }
    }

    private async Task<IFlowchartMapper> GetFlowchartMapperAsync() =>
        _flowchartMapper ??= await MapperFactory.CreateFlowchartMapperAsync();

    private static JsonObject CreateSyntheticContainer(JsonObject single)
    {
        var cloned = (JsonObject)single.DeepClone()!;
        var arr = new JsonArray { cloned };
        return new JsonObject { ["activities"] = arr };
    }

    private void GenerateNewActivityIds(JsonObject container)
    {
        var activities = container.GetActivities().ToList();
        var activityLookup = new Dictionary<string, JsonObject>();
        var newContainerId = IdentityGenerator.GenerateId();

        container.SetId(newContainerId);
        container.SetNodeId($"{container.GetNodeId()}:{newContainerId}");

        foreach (var activity in activities)
        {
            var activityType = activity.GetTypeName();
            var activityVersion = activity.GetVersion();
            var descriptor = ActivityRegistry.Find(activityType, activityVersion)!;
            var newActivityId = IdentityGenerator.GenerateId();

            activityLookup[activity.GetId()] = activity;

            activity.SetId(newActivityId);
            activity.SetNodeId($"{container.GetNodeId()}:{newActivityId}");

            ProcessEmbeddedPorts(activity, descriptor);
        }

        var connections = container.GetConnections().ToList();
        foreach (var connection in connections)
        {
            if (!activityLookup.TryGetValue(connection.Source.ActivityId, out var sourceActivity)) continue;
            if (!activityLookup.TryGetValue(connection.Target.ActivityId, out var targetActivity)) continue;
            connection.Source.ActivityId = sourceActivity.GetId();
            connection.Target.ActivityId = targetActivity.GetId();
        }
        container.SetConnections(connections);
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
        if (_graphApi is not null) await _graphApi.DisposeGraphAsync();
        _componentRef?.Dispose();
    }
}
