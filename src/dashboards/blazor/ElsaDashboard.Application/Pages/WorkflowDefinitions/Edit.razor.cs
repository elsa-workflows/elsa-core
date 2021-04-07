using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Elsa.Client.Models;
using ElsaDashboard.Application.Extensions;
using ElsaDashboard.Application.Models;
using ElsaDashboard.Application.Options;
using ElsaDashboard.Application.Services;
using ElsaDashboard.Application.Shared;
using ElsaDashboard.Shared.Rpc;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Options;

namespace ElsaDashboard.Application.Pages.WorkflowDefinitions
{
    partial class Edit
    {
        [Parameter] public string? WorkflowDefinitionId { get; set; }
        [Inject] private IWorkflowDefinitionService WorkflowDefinitionService { get; set; } = default!;
        [Inject] private IActivityService ActivityService { get; set; } = default!;
        [Inject] private NavigationManager NavigationManager { get; set; } = default!;
        [Inject] private IFlyoutPanelService FlyoutPanelService { get; set; } = default!;
        [Inject] private IOptions<ElsaDashboardOptions> Options { get; set; } = default!;
        private IDictionary<string, ActivityDescriptor> ActivityDescriptors { get; set; } = default!;
        private EventCallbackFactory EventCallbackFactory { get; } = new();
        private string ElsaServerUrl => Options.Value.ElsaServerUrl.ToString();

        private WorkflowDefinition WorkflowDefinition { get; set; } = new()
        {
            Name = "Untitled",
            DisplayName = "Untitled",
            Version = 1
        };

        private WorkflowModel WorkflowModel { get; set; } = WorkflowModel.Blank();
        private BackgroundWorker BackgroundWorker { get; } = new();
        private string DisplayName => !string.IsNullOrWhiteSpace(WorkflowDefinition.DisplayName) ? WorkflowDefinition.DisplayName : !string.IsNullOrWhiteSpace(WorkflowDefinition.Name) ? WorkflowDefinition.Name : "Untitled";
        private static TabItem[] Tabs => new[] {new TabItem("Designer"), new TabItem("DSL", "Dsl"), new TabItem("Settings")};
        private TabItem CurrentTab { get; set; } = Tabs.First();

        protected override async Task OnInitializedAsync()
        {
            ActivityDescriptors = (await ActivityService.GetActivitiesAsync()).ToDictionary(x => x.Type);
            StartBackgroundWorker();
        }

        protected override async Task OnParametersSetAsync()
        {
            if (WorkflowDefinitionId != null)
            {
                WorkflowDefinition = await WorkflowDefinitionService.GetByIdAsync(WorkflowDefinitionId, VersionOptions.Latest);
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
                WorkflowDefinitionId = WorkflowDefinition.Id,
                Activities = WorkflowModel.Activities.Select(CreateActivityDefinition).ToList(),
                Connections = WorkflowModel.Connections.Select(x => new ConnectionDefinition(x.SourceId, x.TargetId, x.Outcome)).ToList(),
                Name = WorkflowDefinition.Name,
                DisplayName = WorkflowDefinition.DisplayName,
                Description = WorkflowDefinition.Description,
                Publish = false,
                Variables = WorkflowDefinition.Variables,
                ContextOptions = WorkflowDefinition.ContextOptions,
                IsSingleton = WorkflowDefinition.IsSingleton,
                PersistenceBehavior = WorkflowDefinition.PersistenceBehavior,
                DeleteCompletedInstances = WorkflowDefinition.DeleteCompletedInstances
            };

            var isNew = WorkflowDefinition.Id == null!;
            var savedWorkflowDefinition = await WorkflowDefinitionService.SaveAsync(request);

            if (isNew)
                NavigationManager.NavigateTo($"workflow-definitions/{savedWorkflowDefinition.DefinitionId}/designer");
        }

        private ActivityDefinition CreateActivityDefinition(ActivityModel activityModel) =>
            new()
            {
                ActivityId = activityModel.ActivityId,
                Type = activityModel.Type
            };

        private WorkflowModel CreateWorkflowModel(WorkflowDefinition workflowDefinition) =>
            new()
            {
                Name = workflowDefinition.Name,
                Activities = workflowDefinition.Activities.Select(CreateActivityModel).ToImmutableList(),
                Connections = workflowDefinition.Connections.Select(CreateConnectionModel).ToImmutableList()
            };

        private ConnectionModel CreateConnectionModel(ConnectionDefinition connectionDefinition) => new(connectionDefinition.SourceActivityId, connectionDefinition.TargetActivityId, connectionDefinition.Outcome);

        private ActivityModel CreateActivityModel(ActivityDefinition activityDefinition)
        {
            var descriptor = ActivityDescriptors[activityDefinition.Type];
            return new ActivityModel
            {
                Name = activityDefinition.Name,
                ActivityId = activityDefinition.ActivityId,
                Type = activityDefinition.Type,
                DisplayName = descriptor.DisplayName,
                Outcomes = descriptor.Outcomes
            };
        }
        
        private async Task AddActivityAsync(ActivityDescriptor activityDescriptor, string? sourceActivityId, string? targetActivityId, string? outcome)
        {
            outcome ??= "Done";

            var activity = new ActivityModel
            {
                ActivityId = Guid.NewGuid().ToString("N"),
                Type = activityDescriptor.Type,
                Outcomes = activityDescriptor.Outcomes,
                DisplayName = activityDescriptor.DisplayName
            };

            var model = WorkflowModel.AddActivity(activity);

            if (targetActivityId != null)
            {
                var existingConnection = model.Connections.FirstOrDefault(x => x.TargetId == targetActivityId && x.Outcome == outcome);

                if (existingConnection != null)
                {
                    model = model with {Connections = model.Connections.Remove(existingConnection)};
                    var replacementConnection = existingConnection with {SourceId = activity.ActivityId};
                    model.AddConnection(replacementConnection);
                }
                else
                {
                    model = model.AddConnection(activity.ActivityId, targetActivityId, outcome);
                }
            }

            if (sourceActivityId != null)
            {
                var existingConnection = model.Connections.FirstOrDefault(x => x.SourceId == sourceActivityId && x.Outcome == outcome);

                if (existingConnection != null)
                {
                    model = model with {Connections = model.Connections.Remove(existingConnection)};
                    var replacementConnection = existingConnection with {TargetId = activity.ActivityId};
                    model = model.AddConnection(replacementConnection);

                    var connection = new ConnectionModel(activity.ActivityId, existingConnection.TargetId, outcome);
                    model = model.AddConnection(connection);
                }
                else
                {
                    var connection = new ConnectionModel(sourceActivityId, activity.ActivityId, outcome);
                    model = model.AddConnection(connection);
                }
            }

            await UpdateWorkflowModelAsync(model);
            await FlyoutPanelService.HideAsync();
        }

        private async Task RemoveActivityAsync(ActivityModel activityModel)
        {
            var incomingConnections = WorkflowModel.GetInboundConnections(activityModel.ActivityId).ToList();
            var outgoingConnections = WorkflowModel.GetOutboundConnections(activityModel.ActivityId).ToList();
            
            // Remove activity (will also remove its connections).
            var model = WorkflowModel.RemoveActivity(activityModel);

            // For each incoming activity, try to connect it to a outgoing activity based on outcome.
            foreach (var incomingConnection in incomingConnections)
            {
                var incomingActivity = model.FindActivity(incomingConnection.SourceId);
                var outgoingConnection = outgoingConnections.FirstOrDefault(x => x.Outcome == incomingConnection.Outcome);

                if (outgoingConnection != null) 
                    model = model.AddConnection(incomingActivity.ActivityId, outgoingConnection.TargetId, incomingConnection.Outcome);
            }

            await UpdateWorkflowModelAsync(model);
        }

        private void SelectTab(TabItem tab) => CurrentTab = tab;

        private async Task UpdateWorkflowModelAsync(WorkflowModel workflowModel)
        {
            WorkflowModel = workflowModel;
            await BackgroundWorker.ScheduleTask(SaveWorkflowAsync);
        }

        private async Task ClosePanelAsync() => await FlyoutPanelService.HideAsync();
        
        private async Task ShowActivityPickerAsync(string? sourceActivityId, string? targetActivityId, string? outcome)
        {
            await FlyoutPanelService.ShowAsync<ActivityPicker>(
                "Activities",
                new {ActivitySelected = EventCallbackFactory.Create<ActivityDescriptorSelectedEventArgs>(this, e => OnActivityPickedAsync(e, sourceActivityId, targetActivityId, outcome))},
                ButtonDescriptor.Create("Cancel", _ => ClosePanelAsync()));
        }

        private void StartBackgroundWorker()
        {
#pragma warning disable 4014
            InvokeAsync(() => BackgroundWorker.StartAsync());
#pragma warning restore 4014
        }

        private async Task OnWorkflowChanged(WorkflowModelChangedEventArgs e) => await UpdateWorkflowModelAsync(e.WorkflowModel);
        private async Task OnAddActivityInvoked(AddActivityInvokedEventArgs e) => await ShowActivityPickerAsync(e.SourceActivityId, e.TargetActivityId, e.Outcome);
        private async Task OnDeleteActivityInvoked(DeleteActivityInvokedEventArgs e) => await RemoveActivityAsync(e.ActivityModel);

        private async Task OnEditActivityInvoked(EditActivityInvokedEventArgs e)
        {
            //var activityInfo = Model.
            
            // await FlyoutPanelService.ShowAsync<ActivityEditor>(
            // activityInfo.DisplayName,
            // new {ActivityInfo = activityInfo},
            // ButtonDescriptor.Create("Cancel", _ => ShowActivityPickerAsync(sourceActivityId, targetActivityId, outcome)),
            // ButtonDescriptor.Create("OK", _ => AddActivityAsync(activityInfo, sourceActivityId, targetActivityId, outcome), true));
        }
        
        private async Task OnActivitySelected(ActivitySelectedEventArgs e)
        {
            
        }
        
        private async Task OnActivityPickedAsync(ActivityDescriptorSelectedEventArgs e, string? sourceActivityId, string? targetActivityId, string? outcome)
        {
            var activityInfo = e.ActivityDescriptor;

            await FlyoutPanelService.ShowAsync<ActivityEditor>(
                activityInfo.DisplayName,
                new {ActivityInfo = activityInfo},
                ButtonDescriptor.Create("Cancel", _ => ShowActivityPickerAsync(sourceActivityId, targetActivityId, outcome)),
                ButtonDescriptor.Create("OK", _ => AddActivityAsync(activityInfo, sourceActivityId, targetActivityId, outcome), true));
        }

        private async Task OnSettingsChanged()
        {
            await BackgroundWorker.ScheduleTask(SaveWorkflowAsync); 
        }
    }
}