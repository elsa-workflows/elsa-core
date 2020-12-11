using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Elsa.Client.Models;
using ElsaDashboard.Application.Models;
using ElsaDashboard.Shared.Rpc;
using Microsoft.AspNetCore.Components;

namespace ElsaDashboard.Application.Pages.WorkflowRegistry
{
    partial class Viewer
    {
        [Parameter] public string WorkflowDefinitionId { get; set; } = default!;
        [Inject] private IWorkflowRegistryService WorkflowRegistryService { get; set; } = default!;
        [Inject] private IActivityService ActivityService { get; set; } = default!;
        private IDictionary<string, ActivityInfo> ActivityDescriptors { get; set; } = default!;
        private WorkflowBlueprint WorkflowBlueprint { get; set; } = default!;
        private WorkflowModel WorkflowModel { get; set; } = WorkflowModel.Blank();

        protected override async Task OnInitializedAsync()
        {
            ActivityDescriptors = (await ActivityService.GetActivitiesAsync()).ToDictionary(x => x.Type);
        }

        protected override async Task OnParametersSetAsync()
        {
            WorkflowBlueprint = (await WorkflowRegistryService.GetById(WorkflowDefinitionId, VersionOptions.Latest))!;
            WorkflowModel = CreateWorkflowModel(WorkflowBlueprint);
        }

        private WorkflowModel CreateWorkflowModel(WorkflowBlueprint workflowBlueprint)
        {
            return new()
            {
                Name = workflowBlueprint.Name,
                Activities = workflowBlueprint.Activities.Select(CreateActivityModel).ToImmutableList(),
                Connections = workflowBlueprint.Connections.Select(CreateConnectionModel).ToImmutableList()
            };
        }

        private ConnectionModel CreateConnectionModel(ConnectionDefinition connectionDefinition)
        {
            return new(connectionDefinition.SourceActivityId, connectionDefinition.TargetActivityId, connectionDefinition.Outcome);
        }

        private ActivityModel CreateActivityModel(ActivityBlueprint activityBlueprint)
        {
            var descriptor = ActivityDescriptors[activityBlueprint.Type];
            return new ActivityModel(activityBlueprint.Id, activityBlueprint.Type, descriptor.Outcomes);
        }
    }
}