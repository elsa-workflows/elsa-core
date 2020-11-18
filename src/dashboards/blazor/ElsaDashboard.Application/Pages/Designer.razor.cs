using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Elsa.Client.Models;
using ElsaDashboard.Application.Models;
using ElsaDashboard.Application.Services;
using ElsaDashboard.Application.Shared;
using ElsaDashboard.Shared.Rpc;
using Microsoft.AspNetCore.Components;

namespace ElsaDashboard.Application.Pages
{
    partial class Designer
    {
        [Parameter]public string? WorkflowDefinitionVersionId { get; set; }
        [Inject] private IWorkflowDefinitionService WorkflowDefinitionService { get; set; } = default!;
        [Inject] private IActivityService ActivityService { get; set; } = default!;
        private IDictionary<string, ActivityDescriptor> ActivityDescriptors { get; set; } = default!;
        private WorkflowDefinition WorkflowDefinition { get; set; } = default!;
        private WorkflowModel WorkflowModel { get; set; } = WorkflowModel.Blank();
        private BackgroundWorker BackgroundWorker { get; } = new();

        protected override async Task OnInitializedAsync()
        {
            ActivityDescriptors = (await ActivityService.GetActivitiesAsync()).ToDictionary(x => x.Type);
            
            if (WorkflowDefinitionVersionId != null)
            {
                WorkflowDefinition = await WorkflowDefinitionService.GetByVersionIdAsync(WorkflowDefinitionVersionId);
                WorkflowModel = CreateWorkflowModel(WorkflowDefinition);
            }
            else
            {
                WorkflowModel = WorkflowModel.Blank();
            }
            
            InvokeAsync(() => BackgroundWorker.StartAsync());
        }

        private WorkflowModel CreateWorkflowModel(WorkflowDefinition workflowDefinition)
        {
            return new WorkflowModel
            {
                Name = workflowDefinition.Name,
                Activities = workflowDefinition.Activities.Select(CreateActivityModel).ToImmutableList(),
                Connections = workflowDefinition.Connections.Select(CreateConnectionModel).ToImmutableList()
            };
        }

        private ConnectionModel CreateConnectionModel(ConnectionDefinition connectionDefinition)
        {
            return new(connectionDefinition.SourceActivityId, connectionDefinition.TargetActivityId, connectionDefinition.Outcome);
        }

        private ActivityModel CreateActivityModel(ActivityDefinition activityDefinition)
        {
            var descriptor = ActivityDescriptors[activityDefinition.Type];
            return new ActivityModel(activityDefinition.ActivityId, activityDefinition.Type, descriptor.Outcomes);
        }
        
        private async ValueTask SaveWorkflowAsync()
        {
            WorkflowDefinition.Connections = WorkflowModel.Connections.Select(x => new ConnectionDefinition(x.SourceId, x.TargetId, x.Outcome)).ToList();
            WorkflowDefinition = await WorkflowDefinitionService.SaveAsync(WorkflowDefinition);
        }

        private async Task OnWorkflowChanged(WorkflowModelChangedEventArgs e)
        {
            WorkflowModel = e.WorkflowModel;
            await BackgroundWorker.ScheduleTask(SaveWorkflowAsync);
        }
    }
}