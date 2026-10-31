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
using Elsa.Studio.Workflows.Designer.Options;
using Elsa.Studio.Workflows.Designer.Services;
using Elsa.Studio.Workflows.Domain.Contracts;
using Elsa.Studio.Workflows.Domain.Models;
using Elsa.Studio.Workflows.Extensions;
using Elsa.Studio.Workflows.UI.Args;
using Humanizer;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.JSInterop;
using ThrottleDebounce;

namespace Elsa.Studio.Workflows.Designer.Components;

/// <summary>
/// A Blazor component that renders a flowchart designer.
/// </summary>
public partial class FlowchartDesigner : IDisposable, IAsyncDisposable
{
    private readonly string _containerId = $"container-{Guid.NewGuid():N}";
    private DotNetObjectReference<FlowchartDesigner>? _componentRef;
    private IFlowchartMapper? _flowchartMapper;
    private IActivityMapper? _activityMapper;
    private X6GraphApi _graphApi = null!;
    private IDisposable? _themeSubscription;
    private readonly PendingActionsQueue _pendingGraphActions;
    private readonly RateLimitedFunc<Task> _rateLimitedLoadFlowchartAction;
    private IDictionary<string, ActivityStats>? _activityStats;
    private JsonObject? _flowchart;

    /// <inheritdoc />
    public FlowchartDesigner()
    {
        _pendingGraphActions = new(() => new(_graphApi != null!), () => Logger);
        _rateLimitedLoadFlowchartAction = Debouncer.Debounce(async () => { await InvokeAsync(async () => await LoadFlowchartAsync(Flowchart, ActivityStats)); }, TimeSpan.FromMilliseconds(100));
    }

    /// The flowchart to render.
    [Parameter] public JsonObject Flowchart { get; set; } = null!;

    /// The activity stats to render.
    [Parameter] public IDictionary<string, ActivityStats>? ActivityStats { get; set; }

    /// Whether the flowchart is read-only.
    [Parameter] public bool IsReadOnly { get; set; }

    /// An event raised when an activity is selected.
    [Parameter] public EventCallback<JsonObject> ActivitySelected { get; set; }
    
    [Parameter] public EventCallback<JsonObject> ActivityPropsChanged { get; set; }

    /// An event raised when an activity-embedded port is selected.
    [Parameter] public EventCallback<ActivityEmbeddedPortSelectedArgs> ActivityEmbeddedPortSelected { get; set; }

    /// An event raised when an activity is double-clicked.
    [Parameter] public EventCallback<JsonObject> ActivityDoubleClick { get; set; }

    /// An event raised when the canvas is selected.
    [Parameter] public EventCallback CanvasSelected { get; set; }

    /// An event raised when the graph is updated.
    [Parameter] public EventCallback GraphUpdated { get; set; }

    [Inject] private DesignerJsInterop DesignerJsInterop { get; set; } = null!;
    [Inject] private IThemeService ThemeService { get; set; } = null!;
    [Inject] private IActivityRegistry ActivityRegistry { get; set; } = null!;
    [Inject] private IMapperFactory MapperFactory { get; set; } = null!;
    [Inject] private IIdentityGenerator IdentityGenerator { get; set; } = null!;
    [Inject] private IActivityNameGenerator ActivityNameGenerator { get; set; } = null!;
    [Inject] private IOptions<DesignerOptions> Options { get; set; } = null!;
    [Inject] private ILogger<FlowchartDesigner> Logger { get; set; } = null!;

    /// <summary>
    /// Invoked from JavaScript when an activity is selected.
    /// </summary>
    /// <param name="activity">The selected activity.</param>
    [JSInvokable]
    /// <summary>
    /// Performs the handle activity selected operation asynchronously.
    /// </summary>
    /// <param name="activity">The activity.</param>
    public async Task HandleActivitySelected(JsonObject activity)
    {
        if (ActivitySelected.HasDelegate)
            await ActivitySelected.InvokeAsync(activity);
    }

    /// <summary>
    /// Performs the update activity operation asynchronously.
    /// </summary>
    /// <param name="activity">The activity.</param>
    public async Task UpdateActivityAsync(JsonObject activity)
    {
        if (ActivityPropsChanged.HasDelegate)
            await ActivityPropsChanged.InvokeAsync(activity);
    }

    /// <summary>
    /// Invoked from JavaScript when an activity embedded port is selected.
    /// </summary>
    /// <param name="activity">The selected activity.</param>
    /// <param name="portName">The selected port name.</param>
    [JSInvokable]
    /// <summary>
    /// Performs the handle activity embedded port selected operation asynchronously.
    /// </summary>
    /// <param name="activity">The activity.</param>
    /// <param name="portName">The port name.</param>
    public async Task HandleActivityEmbeddedPortSelected(JsonObject activity, string portName)
    {
        if (ActivityEmbeddedPortSelected.HasDelegate)
        {
            var args = new ActivityEmbeddedPortSelectedArgs(activity, portName);
            await ActivityEmbeddedPortSelected.InvokeAsync(args);
        }
    }

    /// <summary>
    /// Invoked from JavaScript when an activity is double-clicked.
    /// </summary>
    /// <param name="activity">The clicked activity.</param>
    [JSInvokable]
    /// <summary>
    /// Performs the handle activity double click operation asynchronously.
    /// </summary>
    /// <param name="activity">The activity.</param>
    public async Task HandleActivityDoubleClick(JsonObject activity)
    {
        if (ActivityDoubleClick.HasDelegate)
        {
            await ActivityDoubleClick.InvokeAsync(activity);
        }
    }

    /// Invoked from JavaScript when the canvas is selected.
    [JSInvokable]
    /// <summary>
    /// Performs the handle canvas selected operation asynchronously.
    /// </summary>
    public async Task HandleCanvasSelected()
    {
        if (CanvasSelected.HasDelegate)
            await CanvasSelected.InvokeAsync();
    }

    /// Invoked from JavaScript when the graph is updated.
    [JSInvokable]
    /// <summary>
    /// Performs the handle graph updated operation asynchronously.
    /// </summary>
    public async Task HandleGraphUpdated()
    {
        if (GraphUpdated.HasDelegate)
            await GraphUpdated.InvokeAsync();
    }

    /// Invoked from JavaScript when the graph is updated.
    [JSInvokable]
    /// <summary>
    /// Performs the handle paste cells requested operation asynchronously.
    /// </summary>
    /// <param name="activityNodes">The activity nodes.</param>
    /// <param name="edges">The edges.</param>
    public async Task HandlePasteCellsRequested(X6ActivityNode[] activityNodes, X6Edge[] edges)
    {
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

            // Capture the original activity ID so we can update the edges.
            activityLookup[activityNode.Id] = activityNode;

            // Update the activity ID and name.
            activity.SetId(newActivityId);
            activity.SetNodeId($"{container.GetNodeId()}:{newActivityId}");
            activity.SetName(newName);
            activityNode.Id = newActivityId;

            // Add activity to the list of all activities. This is used to generate a new name for the next activity.
            allActivities.Add(activity);

            // Update the activity metadata.
            var designerMetadata = activity.GetDesignerMetadata();
            designerMetadata.Position.X = activityNode.Position.X + 64;
            designerMetadata.Position.Y = activityNode.Position.Y + 64;
            activityNode.Position.X = designerMetadata.Position.X;
            activityNode.Position.Y = designerMetadata.Position.Y;
            activity.SetDesignerMetadata(designerMetadata);

            // If the activity contains embedded ports, generate new IDs for the contained flowchart.
            ProcessEmbeddedPorts(activity, descriptor);
        }

        // Update the edges.
        foreach (var edge in edges)
        {
            var sourceActivityId = edge.Source.Cell;
            var targetActivityId = edge.Target.Cell;
            var sourceActivity = activityLookup[sourceActivityId];
            var targetActivity = activityLookup[targetActivityId];

            edge.Source.Cell = sourceActivity.Data.GetId();
            edge.Target.Cell = targetActivity.Data.GetId();
        }

        // Update the flowchart.
        await ScheduleGraphActionAsync(async () => await _graphApi.PasteCellsAsync(activityNodes, edges));
    }

    /// <summary>
    /// Reads the flowchart from the graph.
    /// </summary>
    /// <returns>The flowchart.</returns>
    public async Task<JsonObject> ReadFlowchartAsync()
    {
        var serializerOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        return await ScheduleGraphActionAsync(async () =>
        {
            var data = await _graphApi.ReadGraphAsync();
            var cells = data.GetProperty("cells").EnumerateArray();
            var nodes = cells.Where(x => x.GetProperty("shape").GetString() == "elsa-activity").Select(x => x.Deserialize<X6ActivityNode>(serializerOptions)!).ToList();
            var edges = cells.Where(x => x.GetProperty("shape").GetString() == "elsa-edge").Select(x => x.Deserialize<X6Edge>(serializerOptions)!).ToList();
            var graph = new X6Graph(nodes, edges);
            var flowchartMapper = await GetFlowchartMapperAsync();
            var exportedFlowchart = flowchartMapper.Map(graph);
            var activities = exportedFlowchart.GetActivities();
            var connections = exportedFlowchart.GetConnections();

            var flowchart = Flowchart;
            flowchart.SetProperty(activities, "activities");
            flowchart.SetProperty(connections.SerializeToNode(), "connections");

            return flowchart;
        });
    }

    /// <summary>
    /// Loads the flowchart into the graph.
    /// </summary>
    /// <param name="activity">The flowchart to load.</param>
    /// <param name="activityStats">The activity stats to load.</param>
    public async Task LoadFlowchartAsync(
        JsonObject activity,
        IDictionary<string, ActivityStats>? activityStats
    )
    {
        Flowchart = activity;
        ActivityStats = activityStats;

        // 2) Get the mapper as before
        var flowchartMapper = await GetFlowchartMapperAsync();

        // 3) Instead of unwrapping only Elsa.Flowchart, find any container
        //    with an 'activities' array (or fall back to a synthetic one)
        var container = activity.FindActivitiesContainer() ?? CreateSyntheticContainer(activity);

        // 4) Map and schedule the graph load
        var graph = flowchartMapper.Map(container, activityStats);
        await ScheduleGraphActionAsync(() => _graphApi.LoadGraphAsync(graph));
    }

    // Helper: wrap a single activity in an 'activities' array
    // Helper: wrap a single activity in an 'activities' array
    // by cloning it so it has no existing parent.
    private JsonObject CreateSyntheticContainer(JsonObject single)
    {
        // Make a copy so we don't reparent the original
        var cloned = (JsonObject)single.DeepClone()!;

        // Put the clone into a brand-new array
        var arr = new JsonArray();
        arr.Add(cloned);

        // Wrap it up
        return new JsonObject { ["activities"] = arr };
    }

    /// <summary>
    /// Adds an activity to the graph.
    /// </summary>
    /// <param name="activity">The activity to add.</param>
    public async Task AddActivityAsync(JsonObject activity)
    {
        var mapper = await GetActivityMapperAsync();
        var node = mapper.MapActivity(activity);
        await ScheduleGraphActionAsync(() => _graphApi.AddActivityNodeAsync(node));
    }

    /// <summary>
    /// Selects the specified activity in the graph.
    /// </summary>
    /// <param name="id">The ID of the activity to select.</param>
    public async Task SelectActivityAsync(string id)
    {
        await ScheduleGraphActionAsync(() => _graphApi.SelectActivityAsync(id));
    }

    /// Zoom the canvas to fit all activities.
    public async Task ZoomToFitAsync() => await ScheduleGraphActionAsync(() => _graphApi.ZoomToFitAsync());

    /// <summary>
    /// Center the canvas content.
    /// </summary>
    public async Task CenterContentAsync() => await ScheduleGraphActionAsync(() => _graphApi.CenterContentAsync());

    /// <summary>
    /// Exports the graph as an image and lets the browser download it.
    /// </summary>
    /// <param name="options">The export options.</param>
    public async Task ExportGraphAsync(ExportGraphOptions options) => await ScheduleGraphActionAsync(() => _graphApi.ExportGraphAsync(options));

    /// Update the Graph Layout.
    public async Task AutoLayoutAsync(
        JsonObject activity,
        IDictionary<string, ActivityStats>? activityStats
    )
    {
        var flowchartMapper = await GetFlowchartMapperAsync();
        var flowchart = activity.GetFlowchart();
        if (flowchart == null)
            return;
        var graph = flowchartMapper.Map(flowchart, activityStats);
        await ScheduleGraphActionAsync(() => _graphApi.AutoLayoutAsync(graph));
    }

    /// <summary>
    /// Update the specified activity on the graph.
    /// </summary>
    /// <param name="id">The activity ID.</param>
    /// <param name="activity">The updated activity.</param>
    public async Task UpdateActivityAsync(string id, JsonObject activity) => await ScheduleGraphActionAsync(() => _graphApi.UpdateActivityAsync(activity));

    /// <summary>
    /// Update the specified activity stats on the graph.
    /// </summary>
    /// <param name="activityId">The activity ID.</param>
    /// <param name="stats">The updated activity stats.</param>
    public async Task UpdateActivityStatsAsync(string activityId, ActivityStats stats) => await ScheduleGraphActionAsync(() => DesignerJsInterop.UpdateActivityStatsAsync($"activity-{activityId}", activityId, stats));

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        _themeSubscription = new X6DesignerThemeSubscription(
            ThemeService,
            theme => ScheduleGraphActionAsync(() => _graphApi.ApplyThemeAsync(theme)));
        _flowchart = Flowchart;
    }

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _componentRef = DotNetObjectReference.Create(this);
            _graphApi = await DesignerJsInterop.CreateGraphAsync(_containerId, _componentRef, IsReadOnly);
            await _pendingGraphActions.ProcessAsync();
        }
    }

    /// <inheritdoc />
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

    private async Task<IFlowchartMapper> GetFlowchartMapperAsync() => _flowchartMapper ??= await MapperFactory.CreateFlowchartMapperAsync();
    private async Task<IActivityMapper> GetActivityMapperAsync() => _activityMapper ??= await MapperFactory.CreateActivityMapperAsync();
    private async Task ScheduleGraphActionAsync(Func<Task> action) => await _pendingGraphActions.EnqueueAsync(action);
    private async Task<T> ScheduleGraphActionAsync<T>(Func<Task<T>> action) => await _pendingGraphActions.EnqueueAsync(action);

    private void GenerateNewActivityIds(JsonObject container)
    {
        var activities = container.GetActivities().ToList();
        var activityLookup = new Dictionary<string, JsonObject>();
        var newContainerId = IdentityGenerator.GenerateId();

        // Update the container ID.
        container.SetId(newContainerId);
        container.SetNodeId($"{container.GetNodeId()}:{newContainerId}");

        foreach (var activity in activities)
        {
            var activityType = activity.GetTypeName();
            var activityVersion = activity.GetVersion();
            var descriptor = ActivityRegistry.Find(activityType, activityVersion)!;
            var newActivityId = IdentityGenerator.GenerateId();

            // Capture the original activity ID so we can update the edges.  
            activityLookup[activity.GetId()] = activity;

            // Update the activity ID.
            activity.SetId(newActivityId);
            activity.SetNodeId($"{container.GetNodeId()}:{newActivityId}");

            // If the activity contains embedded ports, generate new IDs for the contained flowchart.
            ProcessEmbeddedPorts(activity, descriptor);
        }

        // Update connections.
        var connections = container.GetConnections().ToList();

        foreach (var connection in connections)
        {
            var sourceActivityId = connection.Source.ActivityId;
            var targetActivityId = connection.Target.ActivityId;
            var sourceActivity = activityLookup[sourceActivityId];
            var targetActivity = activityLookup[targetActivityId];

            connection.Source.ActivityId = sourceActivity.GetId();
            connection.Target.ActivityId = targetActivity.GetId();
        }

        container.SetConnections(connections);
    }

    /// Processes each embedded port's activity and generates new IDs for the contained flowchart.
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
