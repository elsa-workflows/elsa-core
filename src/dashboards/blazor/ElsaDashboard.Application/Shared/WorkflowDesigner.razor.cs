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
        [Parameter] public WorkflowModel Model { private get; set; } = WorkflowModel.Demo();
        [Inject] private IJSRuntime JS { get; set; } = default!;
        [Inject] private IFlyoutPanelService FlyoutPanelService { get; set; } = default!;
        private IJSObjectReference _designerModule = default!;

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                _designerModule = await JS.InvokeAsync<IJSObjectReference>("import", "./_content/ElsaDashboard.Application/workflowDesigner.js");
                
            }
            
            await RepaintConnections();
        }

        public async ValueTask DisposeAsync()
        {
            if (_designerModule != null!)
                await _designerModule.DisposeAsync();
        }

        private IEnumerable<ActivityModel> GetRootActivities() => Model.GetChildActivities(null);

        private async ValueTask RepaintConnections()
        {
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
            }).ToArray();
            
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
            }).ToArray();

            var allConnections = rootConnections.Concat(connections);
            await _designerModule.InvokeVoidAsync("updateConnections", (object) allConnections);
        }

        private async ValueTask ClosePanelAsync() => await FlyoutPanelService.HideAsync();

        private async ValueTask ShowActivityPickerAsync()
        {
            await FlyoutPanelService.ShowAsync<ActivityPicker>(
                "Activities",
                new { ActivitySelected = new EventCallback<ActivityDescriptorSelectedEventArgs>(this, (Func<ActivityDescriptorSelectedEventArgs, ValueTask>) OnActivityPickedAsync) },
                new ButtonDescriptor("Cancel", new EventCallback<ButtonClickEventArgs>(this, (Func<ButtonClickEventArgs, ValueTask>) (_ => ClosePanelAsync()))));
        }

        private async ValueTask AddActivityAsync(ActivityDescriptor activityDescriptor)
        {
            var activity = new ActivityModel(Guid.NewGuid().ToString("N"), activityDescriptor.Type);
            Model = Model.AddActivity(activity);
            await FlyoutPanelService.HideAsync();
            await RepaintConnections();
        }

        private async ValueTask OnStartClick() => await ShowActivityPickerAsync();

        private async ValueTask OnActivityPickedAsync(ActivityDescriptorSelectedEventArgs e)
        {
            var activityDescriptor = e.ActivityDescriptor;

            await FlyoutPanelService.ShowAsync<ActivityEditor>(
                activityDescriptor.DisplayName,
                new { activityDescriptor },
                new ButtonDescriptor("Cancel", new EventCallback<ButtonClickEventArgs>(this, (Func<ButtonClickEventArgs, ValueTask>) (_ => ShowActivityPickerAsync()))),
                new ButtonDescriptor("OK", new EventCallback<ButtonClickEventArgs>(this, (Func<ButtonClickEventArgs, ValueTask>) (_ => AddActivityAsync(activityDescriptor))), true));
        }

        private async ValueTask OnActivityClick(MouseEventArgs e)
        {
            await FlyoutPanelService.ShowAsync<ActivityEditor>("Timer Properties");
        }
    }
}