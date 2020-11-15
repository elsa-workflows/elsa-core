using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Formats.Asn1;
using System.Linq;
using System.Threading.Tasks;
using Elsa.Client.Models;
using ElsaDashboard.Application.Extensions;
using ElsaDashboard.Application.Models;
using ElsaDashboard.Application.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace ElsaDashboard.Application.Shared
{
    partial class WorkflowDesigner : IAsyncDisposable
    {
        [Parameter] public WorkflowModel Model { private get; set; } = WorkflowModel.Demo();
        [Inject] private IJSRuntime JS { get; set; } = default!;
        [Inject] private IFlyoutPanelService FlyoutPanelService { get; set; } = default!;
        private IJSObjectReference _designerModule = default!;
        private bool _connectionsChanged = true;
        private EventCallbackFactory EventCallbackFactory { get; } = new();

        public async ValueTask DisposeAsync()
        {
            if (_designerModule != null!)
                await _designerModule.DisposeAsync();
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                _designerModule = await JS.InvokeAsync<IJSObjectReference>("import", "./_content/ElsaDashboard.Application/workflowDesigner.js");
            }

            await RepaintConnections();
        }

        private IEnumerable<ActivityModel> GetRootActivities() => Model.GetChildActivities(null);

        private async ValueTask RepaintConnections()
        {
            if (!_connectionsChanged)
                return;

            _connectionsChanged = false;

            var rootActivities = Model.GetChildActivities(null).ToList();

            var rootConnections = rootActivities.SelectMany(x =>
            {
                return new[]
                {
                    new
                    {
                        sourceId = "start-button",
                        targetId = $"start-button-plus-{x.ActivityId}"
                    },
                    new
                    {
                        sourceId = $"start-button-plus-{x.ActivityId}",
                        targetId = x.ActivityId
                    },
                };
            });

            // Connections from activity outcome to anchor:
            var sourceConnections = Model.Activities.SelectMany(activity => activity.Outcomes.Select(x =>
                new
                {
                    sourceId = activity.ActivityId,
                    targetId = $"{activity.ActivityId}-{x}"
                }));

            var connections = Model.Connections.SelectMany(x => new[]
            {
                new
                {
                    sourceId = $"{x.SourceId}-{x.Outcome}",
                    targetId = x.TargetId
                },
            });

            var allConnections = rootConnections.Concat(connections).Concat(sourceConnections);
            await _designerModule.InvokeVoidAsync("updateConnections", (object) allConnections);
        }

        private async Task ClosePanelAsync() => await FlyoutPanelService.HideAsync();

        private async Task ShowActivityPickerAsync(string? parentActivityId, string? targetActivityId, string? outcome)
        {
            await FlyoutPanelService.ShowAsync<ActivityPicker>(
                "Activities",
                new { ActivitySelected = EventCallbackFactory.Create<ActivityDescriptorSelectedEventArgs>(this, e => OnActivityPickedAsync(e, parentActivityId, targetActivityId, outcome)) },
                ButtonDescriptor.Create("Cancel", _ => ClosePanelAsync()));
        }

        private async Task AddActivityAsync(ActivityDescriptor activityDescriptor, string? sourceActivityId, string? targetActivityId, string? outcome)
        {
            var activity = new ActivityModel(Guid.NewGuid().ToString("N"), activityDescriptor.Type, activityDescriptor.Outcomes);
            var model = Model.AddActivity(activity);

            if (targetActivityId != null)
            {
                var existingConnection = model.Connections.FirstOrDefault(x => x.TargetId == targetActivityId && x.Outcome == outcome);

                if (existingConnection != null)
                {
                    model = model with {Connections = model.Connections.Remove(existingConnection)};
                    var replacementConnection = existingConnection with { SourceId = activity.ActivityId};
                    model.AddConnection(replacementConnection);
                }
                else
                {
                    var connection = new ConnectionModel(activity.ActivityId, targetActivityId, outcome!);
                    model = model.AddConnection(connection);
                }
            }

            if (sourceActivityId != null)
            {
                var existingConnection = model.Connections.FirstOrDefault(x => x.SourceId == sourceActivityId && x.Outcome == outcome);

                if (existingConnection != null)
                {
                    model = model with {Connections = model.Connections.Remove(existingConnection)};
                    var replacementConnection = existingConnection with { TargetId = activity.ActivityId};
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

            Model = model;
            await FlyoutPanelService.HideAsync();
            ConnectionsHasChanged();
        }

        private void ConnectionsHasChanged()
        {
            _connectionsChanged = true;
            StateHasChanged();
        }

        private async Task OnAddActivityClick(string? sourceActivityId, string? targetActivityId, string? outcome) => await ShowActivityPickerAsync(sourceActivityId, targetActivityId, outcome);

        private async Task OnActivityPickedAsync(ActivityDescriptorSelectedEventArgs e, string? sourceActivityId, string? targetActivityId, string? outcome)
        {
            var activityDescriptor = e.ActivityDescriptor;

            await FlyoutPanelService.ShowAsync<ActivityEditor>(
                activityDescriptor.DisplayName,
                new { ActivityDescriptor = activityDescriptor },
                ButtonDescriptor.Create("Cancel", _ => ShowActivityPickerAsync(sourceActivityId, targetActivityId, outcome)),
                ButtonDescriptor.Create("OK", _ => AddActivityAsync(activityDescriptor, sourceActivityId, targetActivityId, outcome), true));
        }

        private async ValueTask OnActivityClick(MouseEventArgs e)
        {
            await FlyoutPanelService.ShowAsync<ActivityEditor>("Timer Properties");
        }
    }
}