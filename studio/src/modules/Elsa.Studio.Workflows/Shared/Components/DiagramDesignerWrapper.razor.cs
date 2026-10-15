using System.Text.Json.Nodes;
using Elsa.Api.Client.Extensions;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Models;
using Elsa.Api.Client.Resources.WorkflowInstances.Models;
using Elsa.Api.Client.Resources.WorkflowInstances.Requests;
using Elsa.Api.Client.Shared.Models;
using Elsa.Studio.Workflows.Domain.Contexts;
using Elsa.Studio.Workflows.Domain.Contracts;
using Elsa.Studio.Workflows.Domain.Extensions;
using Elsa.Studio.Workflows.DiagramDesigners;
using Elsa.Studio.Workflows.Domain.Models;
using Elsa.Studio.Workflows.Domain.Models.Bpmn;
using Elsa.Studio.Workflows.Extensions;
using Elsa.Studio.Workflows.Models;
using Elsa.Studio.Workflows.Shared.Args;
using Elsa.Studio.Workflows.UI.Args;
using Elsa.Studio.Workflows.UI.Contexts;
using Elsa.Studio.Workflows.UI.Contracts;
using Elsa.Studio.Workflows.UI.Models;
using Humanizer;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using MudBlazor;

namespace Elsa.Studio.Workflows.Shared.Components;

/// A wrapper around the diagram designer that provides a breadcrumb and a toolbar.
public partial class DiagramDesignerWrapper
{
    private const string SelfDesignerPortName = "__self";
    private IDiagramDesigner? _diagramDesigner;
    private readonly object _loadActivityLock = new();
    private Task _loadActivityTask = Task.CompletedTask;
    private Stack<ActivityPathSegment> _pathSegments = new();
    private JsonObject? _currentContainerActivity;
    private List<BreadcrumbItem> _breadcrumbItems = new();
    private IDictionary<string, ActivityStats> _activityStats =
        new Dictionary<string, ActivityStats>();
    private readonly Dictionary<string, BpmnElementStats> _elementStats = new();
    private string? _elementStatsInstanceId;
    private HashSet<string> _elementStatsBpmnProcessActivityIds = new();
    private int _elementStatsHighWaterMark;

    /// <summary>
    /// The most journal pages a single <see cref="RefreshElementStatsAsync"/> tick will fetch, at 200 records a
    /// page (see <c>pageSize</c> in <see cref="FetchElementStatsAsync"/>): 50 pages bounds one tick to 10,000
    /// records. A backlog larger than that -- a first load of a long-running instance, or a burst built up while
    /// the tab was backgrounded -- is not truncated, only spread across refreshes: the high-water mark advances by
    /// whatever was actually fetched, so the next tick picks up exactly where this one left off. Settable (rather
    /// than a plain <c>const</c>) only so a test can lower it to exercise the cap without paging 10,000 records.
    /// </summary>
    internal int ElementStatsMaxPagesPerRefresh { get; set; } = 50;

    private ActivityGraph _activityGraph = null!;
    private IDictionary<string, ActivityNode> _indexedActivityNodes =
        new Dictionary<string, ActivityNode>();

    /// The workflow definition version ID.
    [Parameter]
    /// <summary>
    /// Gets or sets the workflow definition version id.
    /// </summary>
    public string WorkflowDefinitionVersionId { get; set; } = null!;

    /// The root activity to display.
    [Parameter]
    public JsonObject Activity { get; set; } = null!;

    /// The workflow definition that the displayed activity belongs to, if known. Used, for example, to propose a
    /// file name for diagram exports.
    [Parameter]
    public WorkflowDefinition? WorkflowDefinition { get; set; }

    /// Whether the designer is read-only.
    [Parameter]
    public bool IsReadOnly { get; set; }

    /// The workflow instance ID, if any.
    [Parameter]
    public string? WorkflowInstanceId { get; set; }

    /// A custom toolbar to display.
    [Parameter]
    public RenderFragment? CustomToolbarItems { get; set; }

    /// Whether the designer is progressing.
    [Parameter]
    /// <summary>
    public bool IsProgressing { get; set; }

    /// An event raised when an activity is selected.
    [Parameter]
    public EventCallback<JsonObject> ActivitySelected { get; set; }

    /// An event raised when an embedded port is selected.
    [Parameter] public EventCallback GraphUpdated { get; set; }
    
    [Parameter] public EventCallback<JsonObject> ActivityUpdated { get; set; }

    /// An event raised when the path changes.
    [Parameter]
    public EventCallback<DesignerPathChangedArgs> PathChanged { get; set; }

    [Inject]
    private IDiagramDesignerService DiagramDesignerService { get; set; } = null!;

    [Inject]
    private IDialogService DialogService { get; set; } = null!;

    [Inject]
    private IWorkflowRootActivityTemplateProvider WorkflowRootActivityTemplateProvider { get; set; } = null!;

    [Inject]
    private IActivityDisplaySettingsRegistry ActivityDisplaySettingsRegistry { get; set; } = null!;

    [Inject]
    private IActivityPortService ActivityPortService { get; set; } = null!;

    [Inject]
    private IActivityRegistry ActivityRegistry { get; set; } = null!;

    [Inject]
    private ILogger<DiagramDesignerWrapper> Logger { get; set; } = null!;

    [Inject]
    private IIdentityGenerator IdentityGenerator { get; set; } = null!;

    [Inject]
    private IActivityExecutionService ActivityExecutionService { get; set; } = null!;

    [Inject]
    private IActivityVisitor ActivityVisitor { get; set; } = null!;

    [Inject]
    private IWorkflowDefinitionService WorkflowDefinitionService { get; set; } = null!;

    [Inject]
    private IWorkflowInstanceService WorkflowInstanceService { get; set; } = null!;

    [Inject]
    private ISnackbar Snackbar { get; set; } = null!;

    private ActivityPathSegment? CurrentPathSegment =>
        _pathSegments.TryPeek(out var segment) ? segment : null;

    private string? _lastSelectedNodeId;

    /// Selects the activity with the specified ID; optionally specifying a node ID to load the selected node path from the backend when activity cannot be found in current container.
    /// <param name="activityId">The ID of the activity to select.</param>
    /// <param name="nodeId">The node ID of the activity to select.</param>
    public async Task SelectActivityByActivityIdAsync(string activityId, string? nodeId = null)
    {
        var containerActivity = GetCurrentContainerActivity();
        var activities = containerActivity?.GetActivities();
        var activityToSelect = activities?.FirstOrDefault(x => x.GetId() == activityId);

        await SelectActivityAsync(activityToSelect, nodeId);
    }

    /// Selects the activity with the specified node ID.
    /// <param name="nodeId">The ID of the activity node to select.</param>
    public async Task SelectActivityAsync(string nodeId)
    {
        var activity = _activityGraph.ActivityNodeLookup.TryGetValue(nodeId, out var node) ? node.Activity : null;
        await SelectActivityAsync(activity, nodeId);
    }

    private async Task SelectActivityAsync(JsonObject? activityToSelect, string? nodeId = null)
    {
        var targetNodeId = activityToSelect?.GetId() ?? nodeId;
        if (string.IsNullOrEmpty(targetNodeId))
            return;

        // Exit early if it's the same as last time
        if (targetNodeId == _lastSelectedNodeId)
            return;

        // Remember for the next call
        _lastSelectedNodeId = targetNodeId;

        // If we found the activity in the current container, just select it directly
        if (activityToSelect != null)
        {
            await _diagramDesigner!.SelectActivityAsync(activityToSelect.GetId());
            return;
        }

        // Activity not found in current container - need to load the path from backend
        if (nodeId == null)
            return;

        // Load the selected node path from the backend.
        var pathSegmentsResponse = await WorkflowDefinitionService.GetPathSegmentsAsync(
            WorkflowDefinitionVersionId,
            nodeId
        );
        if (pathSegmentsResponse == null)
            return;

        await IndexActivityNodes(pathSegmentsResponse.Container.Activity);

        activityToSelect = pathSegmentsResponse.ChildNode.Activity;
        var pathSegments = pathSegmentsResponse.PathSegments.ToList();
        StateHasChanged();

        // Reassign the current path.
        await UpdatePathSegmentsAsync(segments =>
        {
            segments.Clear();

            foreach (var segment in pathSegments)
                segments.Push(segment);
        });

        // Display the new segment.
        _currentContainerActivity = null;
        await DisplayCurrentSegmentAsync();

        // Select the activity.
        await _diagramDesigner!.SelectActivityAsync(activityToSelect.GetId());
    }

    /// Updates the stats of the specified activity.
    /// <param name="activityId">The ID of the activity to update.</param>
    /// <param name="stats">The stats to update.</param>
    public async Task UpdateActivityStatsAsync(string activityId, ActivityStats stats)
    {
        await _diagramDesigner!.UpdateActivityStatsAsync(activityId, stats);
    }

    /// <summary>
    /// Refreshes the element-keyed BPMN instance overlay (gateways, events and sequence flows -- anything without
    /// an Elsa activity id) from the workflow instance's journal, and pushes it to the current diagram designer.
    /// </summary>
    /// <remarks>
    /// A no-op when there is no workflow instance to read from, or the current designer does not accept an
    /// element-keyed overlay (<see cref="IBpmnElementStatsSink"/>) -- fetching and folding the journal for a
    /// flowchart or state machine instance would be wasted work. Called on the same cadence
    /// <see cref="Components.WorkflowInstanceViewer.Components.WorkflowInstanceDesigner"/> already refreshes
    /// <see cref="ActivityStats"/> on, so the two overlays stay in step.
    /// </remarks>
    internal virtual async Task RefreshElementStatsAsync()
    {
        if (WorkflowInstanceId == null || _diagramDesigner is not IBpmnElementStatsSink sink)
            return;

        var elementStats = await FetchElementStatsAsync(WorkflowInstanceId);
        await sink.UpdateElementStatsAsync(elementStats);
    }

    /// <summary>
    /// Reads the BPMN diagnostics projected onto the journal of every <c>Elsa.BpmnProcess</c> scope anywhere in
    /// the workflow (not merely the currently displayed container: a nested scope's diagnostics land on that
    /// scope's own activity, and BPMN element and flow ids are unique across the whole document), and folds them
    /// into an element-keyed stats map.
    /// </summary>
    /// <remarks>
    /// Incremental: <see cref="_elementStatsHighWaterMark"/> is the number of matching journal records already
    /// folded into <see cref="_elementStats"/>, so a tick only ever fetches the records that arrived since the
    /// previous one, rather than re-fetching and re-folding the whole journal from the start every time. This is
    /// only safe because the journal is append-only in the order the API returns it (see the <c>Sequence</c> on
    /// <see cref="WorkflowExecutionLogRecord"/>): the high-water mark is a plain count of already-folded records,
    /// not an id or timestamp, because <see cref="JournalFilter"/> has no way to filter by either. The map and
    /// mark are reset whenever the displayed instance or the set of <c>Elsa.BpmnProcess</c> scope activity ids
    /// changes, since a high-water mark from a different instance or a different filter has nothing to do with
    /// the one about to be fetched. A single tick fetches at most <see cref="ElementStatsMaxPagesPerRefresh"/>
    /// pages, so a backlog larger than that -- a first load, or a burst built up while the tab was backgrounded --
    /// is folded a page cap's worth at a time across successive refreshes rather than in one unbounded loop.
    /// </remarks>
    private async Task<IReadOnlyDictionary<string, BpmnElementStats>> FetchElementStatsAsync(string workflowInstanceId)
    {
        var bpmnProcessActivityIds = GetBpmnProcessActivityIds();

        if (bpmnProcessActivityIds.Count == 0)
            return _elementStats;

        if (workflowInstanceId != _elementStatsInstanceId
            || !_elementStatsBpmnProcessActivityIds.SetEquals(bpmnProcessActivityIds))
        {
            _elementStats.Clear();
            _elementStatsHighWaterMark = 0;
            _elementStatsInstanceId = workflowInstanceId;
            _elementStatsBpmnProcessActivityIds = new HashSet<string>(bpmnProcessActivityIds);
        }

        var filter = new JournalFilter { ActivityIds = bpmnProcessActivityIds };
        var entries = new List<WorkflowExecutionLogRecord>();
        const int pageSize = 200;
        var skip = _elementStatsHighWaterMark;

        for (var page = 0; page < ElementStatsMaxPagesPerRefresh; page++)
        {
            var response = await WorkflowInstanceService.GetJournalAsync(workflowInstanceId, filter, skip, pageSize);
            entries.AddRange(response.Items);
            skip += response.Items.Count;

            if (response.Items.Count < pageSize)
                break;
        }

        _elementStatsHighWaterMark = skip;
        BpmnElementStatsProjector.Fold(entries, _elementStats);

        return _elementStats;
    }

    /// <summary>
    /// Every <c>Elsa.BpmnProcess</c> activity's own id, anywhere in the whole workflow -- the outermost scope and
    /// every nested one -- since <c>BpmnScopeHost</c> only ever writes a diagnostic onto the scope's own activity.
    /// </summary>
    private ICollection<string> GetBpmnProcessActivityIds() =>
        _activityGraph.ActivityNodeLookup.Values
            .Where(node => node.Activity.GetTypeName() == BpmnProcessConstants.ActivityTypeName)
            .Select(node => node.Activity.GetId())
            .ToList();

    /// Reads the activity from the designer.
    public async Task<JsonObject> ReadActivityAsync()
    {
        return await _diagramDesigner!.ReadRootActivityAsync();
    }

    /// Gets the root activity graph.
    public Task<ActivityGraph> GetActivityGraphAsync()
    {
        return Task.FromResult(_activityGraph);
    }

    /// Gets the root activity.
    public async Task<JsonObject> GetActivityAsync()
    {
        if (_diagramDesigner != null)
        {
            var embeddedActivity = await _diagramDesigner.ReadRootActivityAsync();
            await ApplyCurrentDesignerActivityToGraphAsync(embeddedActivity);
        }

        return _activityGraph.Activity;
    }

    /// Loads the specified activity into the designer.
    /// <param name="activity">The activity to load.</param>
    public Task LoadActivityAsync(JsonObject activity)
    {
        Task previousLoad;
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        lock (_loadActivityLock)
        {
            previousLoad = _loadActivityTask;
            _loadActivityTask = completion.Task;
        }

        _ = RunActivityLoadAsync(previousLoad, activity, completion);
        return completion.Task;
    }

    private async Task RunActivityLoadAsync(Task previousLoad, JsonObject activity, TaskCompletionSource completion)
    {
        try
        {
            try
            {
                await previousLoad;
            }
            catch (Exception ex)
            {
                Logger.LogDebug(ex, "A previous diagram load failed; continuing with the newer load.");
            }

            await LoadActivityCoreAsync(activity);
            completion.TrySetResult();
        }
        catch (OperationCanceledException ex)
        {
            completion.TrySetCanceled(ex.CancellationToken);
        }
        catch (Exception ex)
        {
            completion.TrySetException(ex);
        }
    }

    private async Task LoadActivityCoreAsync(JsonObject activity)
    {
        Activity = activity;
        _diagramDesigner = DiagramDesignerService.GetDiagramDesigner(activity);
        _activityGraph = await ActivityVisitor.VisitAndCreateGraphAsync(activity);
        await IndexActivityNodes(_activityGraph.Activity);
        await UpdatePathSegmentsAsync(segments => segments.Clear());
        await UpdateBreadcrumbItemsAsync();
        StateHasChanged();
    }

    /// Updates the specified activity in the designer.
    /// <param name="activityId">The ID of the activity to update.</param>
    /// <param name="activity">The activity to update.</param>
    public async Task UpdateActivityAsync(string activityId, JsonObject activity)
    {
        var currentContainer = GetCurrentContainerActivityOrRoot();

        if (currentContainer == activity)
        {
            if (GraphUpdated.HasDelegate)
                await GraphUpdated.InvokeAsync();

            return;
        }

        await _diagramDesigner!.UpdateActivityAsync(activityId, activity);
    }

    /// The current container activity or the root activity of the graph.
    public JsonObject GetCurrentContainerActivityOrRoot()
    {
        var activity = GetCurrentContainerActivity();
        return _currentContainerActivity = activity ?? Activity;
    }

    private async Task<JsonObject> GetCurrentContainerActivityOrRootAsync()
    {
        var activity = GetCurrentContainerActivity();

        if (activity == null)
        {
            var lastSegment = _pathSegments.First();
            var parentNodeId = lastSegment.ActivityNodeId;
            var selectedActivityGraph = await WorkflowDefinitionService.FindSubgraphAsync(
                WorkflowDefinitionVersionId,
                parentNodeId
            );
            var propName = lastSegment.PortName.Camelize();
            var selectedPortActivity = (JsonObject?)selectedActivityGraph!.Activity[propName];
            activity = selectedPortActivity;
            _currentContainerActivity = activity;
            await IndexActivityNodes(selectedActivityGraph.Activity);
        }

        return activity ?? Activity;
    }

    private async Task IndexActivityNodes(JsonObject activity)
    {
        var visitedNode = await ActivityVisitor.VisitAsync(activity);
        var nodes = visitedNode.Flatten();
        foreach (var node in nodes)
            _indexedActivityNodes[node.NodeId] = node;
    }

    private JsonObject? GetCurrentContainerActivity()
    {
        if (_currentContainerActivity != null)
            return _currentContainerActivity;

        var lastSegment = _pathSegments.FirstOrDefault();

        if (lastSegment == null)
        {
            _currentContainerActivity = Activity;
            return _currentContainerActivity;
        }

        var nodeId = lastSegment.ActivityNodeId;
        var node = _indexedActivityNodes.TryGetValue(nodeId, out var activityNode)
            ? activityNode
            : null;

        if (node is null)
            return null;

        var activity = node.Activity;
        var port = lastSegment.PortName;
        if (IsSelfDesignerSegment(lastSegment))
            return activity;

        var embeddedActivity = GetEmbeddedActivity(activity, port);

        if (embeddedActivity?.GetTypeName() == "Elsa.Workflow")
            embeddedActivity = embeddedActivity.GetRoot();

        return embeddedActivity ?? Activity;
    }

    /// The parent activity of the current activity being loaded in the designer.
    public JsonObject GetParentActivity()
    {
        var lastSegment = _pathSegments.FirstOrDefault();
        var nodeId = lastSegment?.ActivityNodeId;
        var node =
            nodeId != null
                ? _indexedActivityNodes.TryGetValue(nodeId, out var activityNode)
                    ? activityNode
                    : null
                : null;
        return node?.Activity ?? Activity;
    }

    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        await ActivityRegistry.EnsureLoadedAsync();
        await LoadActivityAsync(Activity);
    }

    /// Updates the current path.
    /// <param name="action">A delegate that manipulates the path</param>
    private async Task UpdatePathSegmentsAsync(Action<Stack<ActivityPathSegment>> action)
    {
        action(_pathSegments);
        _currentContainerActivity = null;

        if (PathChanged.HasDelegate)
        {
            var parentActivity = GetParentActivity();
            var currentContainerActivity = GetCurrentContainerActivityOrRoot();
            await PathChanged.InvokeAsync(new(parentActivity, currentContainerActivity));
        }
    }

    private JsonObject? GetEmbeddedActivity(JsonObject activity, string portName)
    {
        // A container's own collection of activities (e.g. a flowchart's or a BPMN process's "Activities"
        // property) is reported as a path segment port by elsa-core's path-segments endpoint, but it holds the
        // container's children directly on the container's own JSON rather than as a single embedded activity.
        // Resolving it as an ordinary named port would call into a port provider that never declares such a port
        // for these containers, so the container is its own "embedded activity" for that port instead.
        if (IsCollectionPort(activity, portName))
            return activity;

        var activityTypeName = activity.GetTypeName();
        var activityVersion = activity.GetVersion();
        var activityDescriptor = ActivityRegistry.Find(activityTypeName, activityVersion)!;
        var portProviderContext = new PortProviderContext(activityDescriptor, activity);
        var portProvider = ActivityPortService.GetProvider(portProviderContext);
        var activityInPort = portProvider.ResolvePort(portName, portProviderContext);

        return activityInPort;
    }

    /// Returns true if <paramref name="portName"/> names a collection property on <paramref name="activity"/>
    /// (such as a container's own list of child activities) rather than a single embedded activity.
    private static bool IsCollectionPort(JsonObject activity, string portName) =>
        activity[portName.Camelize()] is JsonArray;

    private async Task UpdateBreadcrumbItemsAsync()
    {
        _breadcrumbItems = (await GetBreadcrumbItems()).ToList();
        await RefreshActivityStatsAsync();
        StateHasChanged();
    }

    private async Task RefreshActivityStatsAsync()
    {
        if (WorkflowInstanceId != null)
        {
            var currentContainerActivity = GetCurrentContainerActivityOrRoot();
            var report = await ActivityExecutionService.GetReportAsync(WorkflowInstanceId, currentContainerActivity);
            _activityStats = report.Stats.ToDictionary(x => x.ActivityNodeId, x => new ActivityStats
            {
                Faulted = x.IsFaulted,
                Blocked = x.IsBlocked,
                Completed = x.CompletedCount,
                Started = x.StartedCount,
                Uncompleted = x.UncompletedCount,
                Metadata = x.Metadata
            });

            await RefreshElementStatsAsync();
        }
    }

    private async Task<IEnumerable<BreadcrumbItem>> GetBreadcrumbItems()
    {
        var breadcrumbItems = new List<BreadcrumbItem>();

        if (_pathSegments.Any())
            breadcrumbItems.Add(new("Root", "#_root_", false, Icons.Material.Outlined.Home));

        var nodeLookup = _indexedActivityNodes;
        var firstSegment = _pathSegments.FirstOrDefault();

        foreach (var segment in _pathSegments.Reverse())
        {
            var activityNodeId = segment.ActivityNodeId;

            if (!nodeLookup.TryGetValue(activityNodeId, out var activityNode))
            {
                activityNode = await WorkflowDefinitionService.FindSubgraphAsync(
                    WorkflowDefinitionVersionId,
                    activityNodeId
                );
            }

            if (activityNode == null)
                continue;

            var activity = activityNode.Activity;
            var activityTypeName = activity.GetTypeName();
            var activityVersion = activity.GetVersion();
            var activityDescriptor = ActivityRegistry.Find(activityTypeName, activityVersion)!;
            var displaySettings = ActivityDisplaySettingsRegistry.GetSettings(activityTypeName);
            var disabled = segment == firstSegment;
            var activityDisplayText = activity.GetName() ?? activityDescriptor.DisplayName ?? activityDescriptor.Name;
            var breadcrumbDisplayText = activityDisplayText;
            if (!IsSelfDesignerSegment(segment))
            {
                var portProviderContext = new PortProviderContext(activityDescriptor, activity);
                var portProvider = ActivityPortService.GetProvider(portProviderContext);
                var ports = portProvider.GetPorts(portProviderContext);

                // A container's own collection port (e.g. "Activities") never appears among a descriptor's declared
                // ports, so there is nothing to suffix the breadcrumb with; fall back to the activity's own display
                // name rather than throwing.
                var embeddedPort = ports.FirstOrDefault(x => x.Name == segment.PortName);
                if (embeddedPort != null)
                    breadcrumbDisplayText = $"{activityDisplayText}: {embeddedPort.DisplayName ?? embeddedPort.Name}";
            }

            var activityBreadcrumbItem = new BreadcrumbItem(
                breadcrumbDisplayText ?? string.Empty,
                $"#{activity.GetId()}",
                disabled,
                displaySettings.Icon
            );

            breadcrumbItems.Add(activityBreadcrumbItem);
        }

        return breadcrumbItems;
    }

    private async Task DisplayCurrentSegmentAsync()
    {
        var currentContainerActivity = await GetCurrentContainerActivityOrRootAsync();

        _diagramDesigner = DiagramDesignerService.GetDiagramDesigner(currentContainerActivity);
        await _diagramDesigner.LoadRootActivityAsync(currentContainerActivity, _activityStats);
        await UpdateBreadcrumbItemsAsync();
    }

    private RenderFragment? DisplayDesigner()
    {
        return _diagramDesigner?.DisplayDesigner(new(
            GetCurrentContainerActivityOrRoot(),
            EventCallback.Factory.Create<JsonObject>(this, OnActivitySelected),
            EventCallback.Factory.Create<JsonObject>(this, OnActivityUpdated),
            EventCallback.Factory.Create<ActivityEmbeddedPortSelectedArgs>(this, OnActivityEmbeddedPortSelected),
            EventCallback.Factory.Create<JsonObject>(this, OnActivityDoubleClick),
            EventCallback.Factory.Create(this, OnGraphUpdated),
            IsReadOnly,
            _activityStats)
        {
            WorkflowDefinition = WorkflowDefinition
        });
    }

    private async Task OnActivitySelected(JsonObject activity)
    {
        // Update the last selected activity ID to prevent redundant selection calls
        _lastSelectedNodeId = activity.GetId();

        // Pass through to the original callback
        if (ActivitySelected.HasDelegate)
            await ActivitySelected.InvokeAsync(activity);
    }

    private async Task OnActivityDoubleClick(JsonObject activity)
    {
        // If the activity is a workflow definition activity, then open the workflow definition editor.
        if (activity.GetWorkflowDefinitionId() != null)
        {
            if (IsReadOnly)
                await OnActivityEmbeddedPortSelected(new(activity, "Root"));

            return;
        }

        if (!HasDiagramDesigner(activity))
            return;

        var segment = new ActivityPathSegment(
            activity.GetNodeId(),
            activity.GetId(),
            activity.GetTypeName(),
            SelfDesignerPortName
        );

        await UpdatePathSegmentsAsync(segments => segments.Push(segment));
        await DisplayCurrentSegmentAsync();
    }
    
    private async Task OnActivityUpdated(JsonObject activity)
    {
        if (ActivityUpdated.HasDelegate)
            await ActivityUpdated.InvokeAsync(activity);
    }

    private async Task OnActivityEmbeddedPortSelected(ActivityEmbeddedPortSelectedArgs args)
    {
        var nodes = _indexedActivityNodes;
        var selectedActivity = args.Activity;
        var activity = nodes.TryGetValue(selectedActivity.GetNodeId(), out var selectedActivityNode)
            ? selectedActivityNode.Activity
            : null;

        if (activity is null)
            return;

        var portName = args.PortName;
        var activityTypeName = activity.GetTypeName();
        var activityVersion = activity.GetVersion();
        var activityDescriptor = ActivityRegistry.Find(activityTypeName, activityVersion)!;
        var portProviderContext = new PortProviderContext(activityDescriptor, activity);
        var portProvider = ActivityPortService.GetProvider(portProviderContext);
        var embeddedActivity = portProvider.ResolvePort(portName, portProviderContext);

        if (embeddedActivity == null)
        {
            // Lazy load.
            if (activityDescriptor.CustomProperties.ContainsKey("WorkflowDefinitionVersionId"))
            {
                var parentNodeId = activity.GetNodeId();
                var selectedActivityGraph =
                    await WorkflowDefinitionService.FindSubgraphAsync(
                        WorkflowDefinitionVersionId,
                        parentNodeId
                    )
                    ?? throw new InvalidOperationException(
                        $"Could not find selected activity graph for {parentNodeId}"
                    );
                var propName = portName.Camelize();
                var selectedPortActivity = (JsonObject)selectedActivityGraph.Activity[propName]!;
                embeddedActivity = selectedPortActivity;
                portProvider.AssignPort(args.PortName, embeddedActivity, new(activityDescriptor, activity));
                await IndexActivityNodes(selectedActivityGraph.Activity);
            }
        }

        if (embeddedActivity != null)
        {
            if (!HasDiagramDesigner(embeddedActivity))
            {
                if (ActivitySelected.HasDelegate)
                    await ActivitySelected.InvokeAsync(embeddedActivity);
                return;
            }
        }
        else
        {
            if (IsReadOnly)
                return;

            var template = await SelectRootActivityTemplateAsync(portProvider, portProviderContext, portName);
            if (template == null)
                return;

            embeddedActivity = template.CreateRoot(IdentityGenerator);
            embeddedActivity["nodeId"] = $"{activity.GetNodeId()}:{embeddedActivity.GetId()}";
            portProvider.AssignPort(args.PortName, embeddedActivity, portProviderContext);

            // Update the graph only after the user confirms the branch type.
            await _diagramDesigner!.UpdateActivityAsync(activity.GetId(), activity);
        }

        // Create a new path segment of the container activity and push it onto the stack.
        var segment = new ActivityPathSegment(
            activity.GetNodeId(),
            activity.GetId(),
            activity.GetTypeName(),
            args.PortName
        );

        await UpdatePathSegmentsAsync(segments => segments.Push(segment));
        await DisplayCurrentSegmentAsync();
    }

    private async Task OnGraphUpdated()
    {
        var readActivity = await TryReadCurrentDesignerActivityAsync();

        if (readActivity == null)
            return;

        await ApplyCurrentDesignerActivityToGraphAsync(readActivity);

        if (GraphUpdated.HasDelegate)
            await GraphUpdated.InvokeAsync();
    }

    private async Task<JsonObject?> TryReadCurrentDesignerActivityAsync()
    {
        try
        {
            return await _diagramDesigner!.ReadRootActivityAsync();
        }
        catch (DiagramDesignerValidationException)
        {
            return null;
        }
    }

    private async Task ApplyCurrentDesignerActivityToGraphAsync(JsonObject embeddedActivity)
    {
        var currentSegment = CurrentPathSegment;

        if (currentSegment == null) // Root activity was updated.
        {
            _activityGraph = await ActivityVisitor.VisitAndCreateGraphAsync(embeddedActivity);
            await IndexActivityNodes(_activityGraph.Activity);
        }
        else
        {
            var currentActivityNode = _activityGraph.ActivityNodeLookup[
                currentSegment.ActivityNodeId
            ];
            var currentActivity = currentActivityNode.Activity;
            var portName = currentSegment.PortName;
            if (IsSelfDesignerSegment(currentSegment))
            {
                ReplaceJsonObjectContents(currentActivity, embeddedActivity);
                await _activityGraph.IndexAsync();
                await IndexActivityNodes(_activityGraph.Activity);
                return;
            }

            var activityTypeName = currentActivity.GetTypeName();
            var activityVersion = currentActivity.GetVersion();
            var activityDescriptor = ActivityRegistry.Find(activityTypeName, activityVersion)!;
            var portProviderContext = new PortProviderContext(activityDescriptor, currentActivity);
            var portProvider = ActivityPortService.GetProvider(portProviderContext);

            portProvider.AssignPort(portName, embeddedActivity, portProviderContext);
            await _activityGraph.IndexAsync();
            await IndexActivityNodes(_activityGraph.Activity);
        }
    }

    private static bool IsSelfDesignerSegment(ActivityPathSegment segment) => segment.PortName == SelfDesignerPortName;

    private bool HasDiagramDesigner(JsonObject activity)
    {
        var designerActivity = activity.GetTypeName() == "Elsa.Workflow" ? activity.GetRoot() : activity;
        return designerActivity != null && DiagramDesignerService.HasDiagramDesigner(designerActivity);
    }

    private async Task<WorkflowRootActivityTemplate?> SelectRootActivityTemplateAsync(
        IActivityPortProvider portProvider,
        PortProviderContext portProviderContext,
        string portName)
    {
        var currentContainerType = GetCurrentContainerActivityOrRoot().GetTypeName();
        var selectedTemplateKey = WorkflowRootActivityTemplateProvider.Find(currentContainerType)?.Key
                                  ?? WorkflowRootActivityTemplateProvider.GetDefault().Key;
        var portDisplayName = portProvider.GetPorts(portProviderContext)
            .FirstOrDefault(x => x.Name == portName)?.DisplayName ?? portName.Humanize();
        var parameters = new DialogParameters<SelectWorkflowRootActivityDialog>
        {
            { x => x.SelectedTemplateKey, selectedTemplateKey }
        };
        var options = new DialogOptions
        {
            CloseOnEscapeKey = true,
            CloseButton = true,
            FullWidth = true,
            MaxWidth = MaxWidth.Small
        };
        var dialog = await DialogService.ShowAsync<SelectWorkflowRootActivityDialog>(
            Localizer["Create {0} branch", portDisplayName],
            parameters,
            options);
        var result = await dialog.Result;

        return result is { Canceled: false, Data: string templateKey }
            ? WorkflowRootActivityTemplateProvider.Find(templateKey)
            : null;
    }

    private static void ReplaceJsonObjectContents(JsonObject target, JsonObject source)
    {
        if (ReferenceEquals(target, source))
            return;

        target.Clear();
        foreach (var property in source.ToList())
        {
            source.Remove(property.Key);
            target[property.Key] = property.Value;
        }
    }

    private async Task OnBreadcrumbItemClicked(BreadcrumbItem item)
    {
        if (item.Href == "#_root_")
        {
            await UpdatePathSegmentsAsync(segments => segments.Clear());
            await DisplayCurrentSegmentAsync();
            return;
        }

        var activityId = item.Href![1..];

        await UpdatePathSegmentsAsync(segments =>
        {
            while (segments.TryPeek(out var segment))
                if (segment.ActivityId == activityId)
                    break;
                else
                    segments.Pop();
        });

        _currentContainerActivity = null;
        await DisplayCurrentSegmentAsync();
    }
}
