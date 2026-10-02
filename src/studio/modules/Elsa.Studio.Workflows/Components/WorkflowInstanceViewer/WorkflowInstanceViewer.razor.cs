using System.Text.Json.Nodes;
using Elsa.Api.Client.Extensions;
using Elsa.Api.Client.RealTime.Messages;
using Elsa.Api.Client.Resources.ActivityExecutions.Models;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Models;
using Elsa.Api.Client.Resources.WorkflowInstances.Enums;
using Elsa.Api.Client.Resources.WorkflowInstances.Models;
using Elsa.Api.Client.Resources.WorkflowInstances.Requests;
using Elsa.Studio.Workflows.Components.WorkflowInstanceViewer.Components;
using Elsa.Studio.Workflows.Contracts;
using Elsa.Studio.Workflows.Domain.Contracts;
using Elsa.Studio.Workflows.Extensions;
using Elsa.Studio.Workflows.Pages.WorkflowInstances.View.Models;
using Elsa.Studio.Workflows.Shared.Args;
using Microsoft.AspNetCore.Components;

namespace Elsa.Studio.Workflows.Components.WorkflowInstanceViewer;

/// The index page for viewing a workflow instance.
public partial class WorkflowInstanceViewer : IAsyncDisposable
{
    private WorkflowInstance _workflowInstance = null!;
    private WorkflowDefinition _workflowDefinition = null!;
    private WorkflowInstanceWorkspace _workspace = null!;
    private IWorkflowInstanceObserver? _workflowInstanceObserver;
    private int _leftPanelTabIndex;
    private string? _selectedActivityExecutionRecordId;
    private string? _selectedActivityNodeId;
    private bool _isExecutionDetailsDrawerOpen;
    private ActivityExecutionRecord? _selectedExecutionForDrawer;
    
    // Track which node IDs were programmatically selected (not by user clicking in designer)
    // This prevents OnActivitySelected from clearing journal/call stack selection
    private string? _programmaticallySelectedNodeId;
    
    // Lookup for custom activity display names (from designer metadata)
    private readonly Dictionary<string, string> _activityDisplayNameLookup = new();
    
    // Lookup for activity names (from designer node)
    private readonly Dictionary<string, string> _activityNameLookup = new();

    /// The ID of the workflow instance to view.
    [Parameter] public string InstanceId { get; set; } = null!;

    /// An event that is invoked when a workflow definition is edited.
    [Parameter] public EventCallback<string> EditWorkflowDefinition { get; set; }

    [Inject] private IWorkflowInstanceService WorkflowInstanceService { get; set; } = null!;

    [Inject] private IWorkflowDefinitionService WorkflowDefinitionService { get; set; } = null!;
    [Inject] private IWorkflowInstanceObserverFactory WorkflowInstanceObserverFactory { get; set; } = null!;
    [Inject] private IActivityExecutionService ActivityExecutionService { get; set; } = null!;
    [Inject] private IActivityVisitor ActivityVisitor { get; set; } = null!;

    private Journal Journal { get; set; } = null!;
    private IDictionary<string, object?> LeftPanelTabAttributes => new Dictionary<string, object?>
    {
        ["WorkflowInstanceId"] = _workflowInstance?.Id,
        ["WorkflowDefinition"] = _workflowDefinition,
        ["WorkflowInstance"] = _workflowInstance
    };

    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        var instance = await WorkflowInstanceService.GetAsync(InstanceId) ?? throw new InvalidOperationException($"Workflow instance with ID {InstanceId} not found.");
        var workflowDefinition = await WorkflowDefinitionService.FindByIdAsync(instance.DefinitionVersionId);
        _workflowInstance = instance;
        _workflowDefinition = workflowDefinition!;
        
        // Build the activity display name lookup from the workflow definition
        await BuildActivityDisplayNameLookupAsync();
        
        await SelectWorkflowInstanceAsync(instance);

        if (_workflowInstance.Status == WorkflowStatus.Running)
            await ObserveWorkflowInstanceAsync();
    }
    
    private async Task BuildActivityDisplayNameLookupAsync()
    {
        _activityDisplayNameLookup.Clear();
        _activityNameLookup.Clear();
        
        if (_workflowDefinition?.Root == null)
            return;
        
        var activityGraph = await ActivityVisitor.VisitAndCreateGraphAsync(_workflowDefinition.Root);
        
        foreach (var node in activityGraph.ActivityNodeLookup.Values)
        {
            var activity = node.Activity;
            var activityId = activity.GetId();
            var displayText = activity.GetDisplayText()?.Trim();
            var activityName = activity.GetName()?.Trim();
            
            if (string.IsNullOrWhiteSpace(activityId))
                continue;
                
            if (!string.IsNullOrWhiteSpace(displayText))
                _activityDisplayNameLookup[activityId] = displayText;
                
            if (!string.IsNullOrWhiteSpace(activityName))
                _activityNameLookup[activityId] = activityName;
        }
    }
    
    private string? GetCustomActivityName(string activityId)
    {
        return _activityDisplayNameLookup.GetValueOrDefault(activityId);
    }
    
    private string? GetActivityName(string activityId)
    {
        return _activityNameLookup.GetValueOrDefault(activityId);
    }

    private async Task ObserveWorkflowInstanceAsync()
    {
        await DisposeObserverAsync();
        _workflowInstanceObserver = await WorkflowInstanceObserverFactory.CreateAsync(_workflowInstance.Id);
        _workflowInstanceObserver.WorkflowInstanceUpdated += OnWorkflowInstanceUpdatedAsync;
    }

    private async Task DisposeObserverAsync()
    {
        if (_workflowInstanceObserver != null)
        {
            _workflowInstanceObserver.WorkflowInstanceUpdated -= OnWorkflowInstanceUpdatedAsync;
            await _workflowInstanceObserver.DisposeAsync();
            _workflowInstanceObserver = null;
        }
    }

    private async Task SelectWorkflowInstanceAsync(WorkflowInstance instance)
    {
        // Select activity node IDs that are direct children of the root.
        var activityNodeIds = _workflowDefinition.Root.GetActivities().Select(x => x.GetNodeId()).ToList();
        var filter = new JournalFilter
        {
            ActivityNodeIds = activityNodeIds
        };
        await Journal.SetWorkflowInstanceAsync(instance, filter);
    }
    
    private async Task OnWorkflowInstanceUpdatedAsync(WorkflowInstanceUpdatedMessage message)
    {
        var workflowInstance = (await WorkflowInstanceService.GetAsync(_workflowInstance.Id))!;
        await InvokeAsync(async () =>
        {
            _workflowInstance = workflowInstance;
            StateHasChanged();

            if (_workflowInstance.Status == WorkflowStatus.Finished) 
                await Journal.SetWorkflowInstanceAsync(_workflowInstance);
        });
    }

    private async Task OnDesignerPathChanged(DesignerPathChangedArgs args)
    {
        var activityNodeIds = args.ContainerActivity.GetActivities().Select(x => x.GetNodeId()).ToList();
        var filter = new JournalFilter
        {
            ActivityNodeIds = activityNodeIds
        };
        await Journal.SetWorkflowInstanceAsync(_workflowInstance, filter);
    }

    private async Task OnWorkflowExecutionLogRecordSelected(JournalEntry entry)
    {
        _programmaticallySelectedNodeId = entry.Record.NodeId;
        await _workspace.SelectWorkflowExecutionLogRecordAsync(entry);
    }

    private async Task OnCallStackEntrySelected(ActivityExecutionRecord record)
    {
        _programmaticallySelectedNodeId = record.ActivityNodeId;
        _selectedActivityNodeId = record.ActivityNodeId;
        _selectedExecutionForDrawer = record;
        _isExecutionDetailsDrawerOpen = true;
        StateHasChanged(); // Open drawer and highlight immediately.

        await _workspace.SelectActivityByIdAsync(record.ActivityId, record.ActivityNodeId);
    }

    private async Task OnActivitySelected(JsonObject arg)
    {
        var nodeId = arg.GetNodeId();
        
        // If this activity was selected programmatically (from journal or call stack), 
        // don't clear the journal selection - just clear the tracking variable
        if (_programmaticallySelectedNodeId == nodeId)
        {
            _programmaticallySelectedNodeId = null;
            return;
        }
        
        // User clicked directly on an activity in the designer - clear journal selection
        _programmaticallySelectedNodeId = null;
        Journal.ClearSelection();
        _selectedActivityNodeId = nodeId;

        // Get the last activity execution record ID for this activity
        var summaries = (await ActivityExecutionService.ListSummariesAsync(_workflowInstance.Id, _selectedActivityNodeId)).ToList();
        _selectedActivityExecutionRecordId = summaries.LastOrDefault()?.Id;

        if (_selectedActivityExecutionRecordId != null)
            _selectedExecutionForDrawer = await ActivityExecutionService.GetAsync(_selectedActivityExecutionRecordId);
        else
            _selectedExecutionForDrawer = null;

        StateHasChanged();
    }

    private async Task OnActivityExecutionSelected(string? executionId)
    {
        if (string.IsNullOrWhiteSpace(executionId))
            return;

        // Load the full execution record
        var record = await ActivityExecutionService.GetAsync(executionId);

        if (record != null)
        {
            _selectedActivityExecutionRecordId = record.Id;
            _selectedActivityNodeId = record.ActivityNodeId;
            _selectedExecutionForDrawer = record;
            _isExecutionDetailsDrawerOpen = true;
            StateHasChanged();
        }
    }

    async ValueTask IAsyncDisposable.DisposeAsync()
    {
        await DisposeObserverAsync();
    }
}
