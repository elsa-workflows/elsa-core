using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Elsa.Client.Models;
using ElsaDashboard.Application.Models;
using ElsaDashboard.Application.Services;
using ElsaDashboard.Shared.Rpc;
using Microsoft.AspNetCore.Components;

namespace ElsaDashboard.Application.Pages
{
    partial class Designer
    {
        [Parameter] public string? WorkflowDefinitionId { get; set; }
        [Inject] private IWorkflowDefinitionService WorkflowDefinitionService { get; set; } = default!;
        [Inject] private IActivityService ActivityService { get; set; } = default!;
        [Inject] private NavigationManager NavigationManager { get; set; } = default!;
        private IDictionary<string, ActivityDescriptor> ActivityDescriptors { get; set; } = default!;
        private WorkflowDefinition WorkflowDefinition { get; set; } = default!;
        private WorkflowModel WorkflowModel { get; set; } = WorkflowModel.Blank();
        private BackgroundWorker BackgroundWorker { get; } = new();

        protected override async Task OnInitializedAsync()
        {
            ActivityDescriptors = (await ActivityService.GetActivitiesAsync()).ToDictionary(x => x.Type);
            StartBackgroundWorker();
        }

        protected override async Task OnParametersSetAsync()
        {
            if (WorkflowDefinitionId != null)
            {
                WorkflowDefinition = await WorkflowDefinitionService.GetById(WorkflowDefinitionId, VersionOptions.Latest);
                WorkflowModel = CreateWorkflowModel(WorkflowDefinition);
            }
            else
            {
                WorkflowDefinition = new WorkflowDefinition();
                WorkflowModel = WorkflowModel.Blank();
            }
        }

        private async ValueTask SaveWorkflowAsync()
        {
            var request = new SaveWorkflowDefinitionRequest
            {
                WorkflowDefinitionId = WorkflowDefinition.WorkflowDefinitionId,
                Activities = WorkflowModel.Activities.Select(CreateActivityDefinition).ToList(),
                Connections = WorkflowModel.Connections.Select(x => new ConnectionDefinition(x.SourceId, x.TargetId, x.Outcome)).ToList(),
                Name = WorkflowDefinition.Name,
                DisplayName = WorkflowDefinition.DisplayName,
                Description = WorkflowDefinition.Description,
                Enabled = WorkflowDefinition.IsEnabled,
                Publish = false,
                Variables = WorkflowDefinition.Variables,
                ContextOptions = WorkflowDefinition.ContextOptions,
                IsSingleton = WorkflowDefinition.IsSingleton,
                PersistenceBehavior = WorkflowDefinition.PersistenceBehavior,
                DeleteCompletedInstances = WorkflowDefinition.DeleteCompletedInstances
            };

            var isNew = WorkflowDefinition.WorkflowDefinitionId == null!;
            WorkflowDefinition = await WorkflowDefinitionService.SaveAsync(request);

            if (isNew) 
                NavigationManager.NavigateTo($"workflows/{WorkflowDefinition.WorkflowDefinitionId}/designer");
        }

        private ActivityDefinition CreateActivityDefinition(ActivityModel activityModel)
        {
            return new ActivityDefinition
            {
                ActivityId = activityModel.ActivityId,
                Type = activityModel.Type
            };
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

        private void StartBackgroundWorker()
        {
#pragma warning disable 4014
            InvokeAsync(() => BackgroundWorker.StartAsync());
#pragma warning restore 4014
        }

        private async Task OnWorkflowChanged(WorkflowModelChangedEventArgs e)
        {
            WorkflowModel = e.WorkflowModel;
            await BackgroundWorker.ScheduleTask(SaveWorkflowAsync);
        }
    }
}