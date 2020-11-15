using System;
using System.Collections.Generic;
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
        [Parameter] public WorkflowModel Model { private get; set; } = WorkflowModel.Blank();
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

        protected override void OnInitialized()
        {
            //Model = WorkflowModel.Demo();
            //ConnectionsHasChanged();
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
            var leafActivities = Model.GetLeafActivities();

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

            var connections = Model.Connections.SelectMany(x =>
            {
                return new[]
                {
                    new
                    {
                        sourceId = x.SourceId,
                        targetId = $"{x.SourceId}-button-plus"
                    },
                    new
                    {
                        sourceId = $"{x.SourceId}-button-plus",
                        targetId = x.TargetId
                    },
                };
            });

            var leafConnections = leafActivities.Select(x => new
            {
                sourceId = x.ActivityId,
                targetId = $"{x.ActivityId}-button-plus"
            });

            var allConnections = rootConnections.Concat(connections).Concat(leafConnections);
            await _designerModule.InvokeVoidAsync("updateConnections", (object) allConnections);
        }

        private async Task ClosePanelAsync() => await FlyoutPanelService.HideAsync();

        private async Task ShowActivityPickerAsync(string? parentActivityId)
        {
            await FlyoutPanelService.ShowAsync<ActivityPicker>(
                "Activities",
                new { ActivitySelected = EventCallbackFactory.Create<ActivityDescriptorSelectedEventArgs>(this, e => OnActivityPickedAsync(e, parentActivityId)) },
                ButtonDescriptor.Create("Cancel", _ => ClosePanelAsync()));
        }

        private async Task AddActivityAsync(ActivityDescriptor activityDescriptor, string? parentActivityId)
        {
            var activity = new ActivityModel(Guid.NewGuid().ToString("N"), activityDescriptor.Type);
            var model = Model.AddActivity(activity);

            if (parentActivityId != null)
            {
                var connection = new ConnectionModel(parentActivityId, activity.ActivityId, "Done");
                model = model.AddConnection(connection);
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

        private async Task OnAddActivityClick(string? parentActivityId) => await ShowActivityPickerAsync(parentActivityId);

        private async Task OnActivityPickedAsync(ActivityDescriptorSelectedEventArgs e, string? parentActivityId)
        {
            var activityDescriptor = e.ActivityDescriptor;

            await FlyoutPanelService.ShowAsync<ActivityEditor>(
                activityDescriptor.DisplayName,
                new { ActivityDescriptor = activityDescriptor },
                ButtonDescriptor.Create("Cancel", _ => ShowActivityPickerAsync(parentActivityId)),
                ButtonDescriptor.Create("OK", _ => AddActivityAsync(activityDescriptor, parentActivityId), true));
        }

        private async ValueTask OnActivityClick(MouseEventArgs e)
        {
            await FlyoutPanelService.ShowAsync<ActivityEditor>("Timer Properties");
        }
    }
}