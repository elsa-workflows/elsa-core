using System.Text.Json.Nodes;
using Elsa.Api.Client.Resources.ActivityExecutions.Models;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Models;
using Elsa.Api.Client.Resources.WorkflowInstances.Models;
using Elsa.Studio.Workflows.Pages.WorkflowInstances.View.Models;
using Elsa.Studio.Workflows.Shared.Args;
using Elsa.Studio.Workflows.UI.Contracts;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Elsa.Studio.Workflows.Components.WorkflowInstanceViewer.Components;

/// <summary>
/// Represents the workflow instance workspace.
/// </summary>
public partial class WorkflowInstanceWorkspace : IWorkspace
{
    private WorkflowInstanceDetails _workflowInstanceDetails = null!;
    private WorkflowInstanceDesigner _workflowInstanceDesigner = null!;
    
    /// Gets or sets the workflow instance to view.
    [Parameter] public WorkflowInstance? WorkflowInstance { get; set; }
    
    /// Gets or sets the workflow definition of the workflow instance to view.
    [Parameter] public WorkflowDefinition? WorkflowDefinition { get; set; }
    
    /// An event callback that is invoked when the current workflow graph path has changed.
    [Parameter] public EventCallback<DesignerPathChangedArgs> PathChanged { get; set; }
    
    /// An event callback that is invoked when an activity is selected.
    [Parameter] public EventCallback<JsonObject> ActivitySelected { get; set; }

    /// An event callback that is invoked when an activity execution is selected.
    [Parameter] public EventCallback<string?> ActivityExecutionSelected { get; set; }

    /// An event that is invoked when a workflow definition is edited.
    [Parameter] public EventCallback<string> EditWorkflowDefinition { get; set; }
    /// <inheritdoc />
    public bool IsReadOnly => true;

    /// <inheritdoc />
    public bool HasWorkflowEditPermission => true;
    
    /// Selects the associated activity in the designer and activates its Event tab.
    public async Task SelectWorkflowExecutionLogRecordAsync(JournalEntry entry)
    {
        await _workflowInstanceDesigner.SelectWorkflowExecutionLogRecordAsync(entry);
    }

    /// Selects the associated activity in the designer and activates its Event tab.
    public async Task OnIncidentActivityNodeIdClicked(string activityNodeId)
    {
        await _workflowInstanceDesigner.SelectActivityAsync(activityNodeId);
    }

    /// Selects the associated activity in the designer.
    public async Task SelectActivityByIdAsync(string activityId, string nodeId)
    {
        await _workflowInstanceDesigner.SelectActivityByIdAsync(activityId, nodeId);
    }
    
    private async Task OnPathChanged(DesignerPathChangedArgs args)
    {
        await _workflowInstanceDetails.UpdateSubWorkflowAsync(args.ParentActivity);
        _workflowInstanceDesigner.UpdateSubWorkflow(args.ParentActivity);
        
        if(PathChanged.HasDelegate)
            await PathChanged.InvokeAsync(args);
    }
}
