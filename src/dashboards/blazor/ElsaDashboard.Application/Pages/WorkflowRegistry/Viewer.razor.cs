using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Elsa.Client.Models;
using ElsaDashboard.Application.Models;
using ElsaDashboard.Services;
using ElsaDashboard.Shared.Rpc;
using Microsoft.AspNetCore.Components;

namespace ElsaDashboard.Application.Pages.WorkflowRegistry
{
    partial class Viewer
    {
        [Parameter] public string WorkflowDefinitionId { get; set; } = default!;
        [Inject] private IWorkflowRegistryService WorkflowRegistryService { get; set; } = default!;
        [Inject] private IActivityService ActivityService { get; set; } = default!;
        [Inject] private IActivityDisplayService ActivityDisplayService { get; set; } = default!;
        private IDictionary<string, ActivityDescriptor> ActivityDescriptors { get; set; } = default!;
        private WorkflowBlueprint WorkflowBlueprint { get; set; } = default!;
        private WorkflowModel WorkflowModel { get; set; } = WorkflowModel.Blank();

        protected override async Task OnInitializedAsync()
        {
            ActivityDescriptors = (await ActivityService.GetActivitiesAsync()).ToDictionary(x => x.Type);
        }

        protected override async Task OnParametersSetAsync()
        {
            WorkflowBlueprint = (await WorkflowRegistryService.GetById(WorkflowDefinitionId, VersionOptions.Latest))!;
            WorkflowModel = await CreateWorkflowModelAsync(WorkflowBlueprint);
        }

        private async ValueTask<WorkflowModel> CreateWorkflowModelAsync(WorkflowBlueprint workflowBlueprint)
        {
            var activities = await Task.WhenAll(workflowBlueprint.Activities.Select(async x => await CreateActivityModelAsync(x)));
            
            return new()
            {
                Name = workflowBlueprint.Name,
                Activities = activities.ToImmutableList(),
                Connections = workflowBlueprint.Connections.Select(CreateConnectionModel).ToImmutableList()
            };
        }

        private ConnectionModel CreateConnectionModel(ConnectionDefinition connectionDefinition)
        {
            return new(connectionDefinition.SourceActivityId, connectionDefinition.TargetActivityId, connectionDefinition.Outcome);
        }

        private async ValueTask<ActivityModel> CreateActivityModelAsync(ActivityBlueprint activityBlueprint)
        {
            var descriptor = ActivityDescriptors[activityBlueprint.Type];
            var displayDescriptor = await ActivityDisplayService.GetDisplayDescriptorAsync(activityBlueprint.Type);
            
            return new ActivityModel
            {
                Name = activityBlueprint.Name,
                Type = activityBlueprint.Type,
                DisplayName = activityBlueprint.DisplayName ?? descriptor.DisplayName,
                Outcomes = descriptor.Outcomes,
                ActivityId = activityBlueprint.Id,
                Description = activityBlueprint.Description ?? descriptor.Description,
                Properties = activityBlueprint.Properties,
                DisplayDescriptor = displayDescriptor
            };
        }
    }
}