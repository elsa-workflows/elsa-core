using System.Text.Json.Nodes;
using Elsa.Api.Client.Extensions;
using Elsa.Api.Client.Resources.ActivityDescriptors.Models;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Enums;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Models;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Requests;
using Elsa.Api.Client.Shared.Models;
using Elsa.Studio.Workflows.Domain.Contracts;
using Elsa.Studio.Workflows.UI.Contracts;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Elsa.Studio.Workflows.Components.WorkflowDefinitionEditor.Components.ActivityProperties.Tabs;

/// <summary>
/// Represents the version tab.
/// </summary>
public partial class VersionTab
{
    [Parameter] public ActivityDescriptor ActivityDescriptor { get; set; } = default!;
    [Parameter] public JsonObject Activity { get; set; } = default!;
    [Parameter] public Func<JsonObject, Task>? OnActivityUpdated { get; set; }
    [Inject] private IWorkflowDefinitionService WorkflowDefinitionService { get; set; } = default!;
    [Inject] IActivityRegistry ActivityRegistry { get; set; } = default!;
    private MudTable<WorkflowDefinitionSummary> Table { get; set; } = default!;
    [CascadingParameter] private IWorkspace? Workspace { get; set; }
    private bool IsReadOnly => Workspace?.IsReadOnly == true;

    private string DefinitionId
    {
        get => Activity.GetProperty("workflowDefinitionId")!.GetValue<string>();
        set => Activity.SetProperty(value, "workflowDefinitionId");
    }

    private string DefinitionVersionId
    {
        get => Activity.GetProperty("workflowDefinitionVersionId")!.GetValue<string>();
        set => Activity.SetProperty(value, "workflowDefinitionVersionId");
    }

    private int CurrentVersionUsed
    {
        get => Activity.GetProperty("version")!.GetValue<int>();
        set => Activity.SetProperty(value, "version");
    }
    
    private string CurrentActivityType => Activity.GetTypeName();
    
    private IEnumerable<int> _versionsUsableAsActivity = [];

    private async Task<TableData<WorkflowDefinitionSummary>> LoadVersionsAsync(TableState tableState, CancellationToken cancellationToken)
    {
        if (Activity == null! || ActivityDescriptor == null!)
            return new TableData<WorkflowDefinitionSummary>();

        var page = tableState.Page;
        var pageSize = tableState.PageSize;
        var definitionId = DefinitionId;

        var request = new ListWorkflowDefinitionsRequest
        {
            DefinitionIds = [definitionId],
            OrderDirection = OrderDirection.Descending,
            OrderBy = OrderByWorkflowDefinition.Version,
            Page = page,
            PageSize = pageSize
        };

        _versionsUsableAsActivity = ActivityRegistry.FindAll(CurrentActivityType).Select(d => d.Version);
        
        var response = await WorkflowDefinitionService.ListAsync(request, VersionOptions.All);

        return new TableData<WorkflowDefinitionSummary>
        {
            Items = response.Items,
            TotalItems = (int)response.TotalCount
        };
    }

    private async Task OnUseVersionClicked(WorkflowDefinitionSummary workflowDefinitionVersion)
    {
        var version = workflowDefinitionVersion.Version;

        if (version == CurrentVersionUsed)
            return;

        CurrentVersionUsed = version;
        DefinitionVersionId = workflowDefinitionVersion.Id;

        if (OnActivityUpdated != null)
            await OnActivityUpdated(Activity);
    }
}