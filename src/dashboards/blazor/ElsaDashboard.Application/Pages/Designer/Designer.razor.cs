using System;
using System.Threading.Tasks;
using Elsa.Client.Models;
using ElsaDashboard.Application.Extensions;
using ElsaDashboard.Application.Models;
using ElsaDashboard.Application.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace ElsaDashboard.Application.Pages.Designer
{
    partial class Designer
    {
        [Inject] private IJSRuntime JS { get; set; } = default!;
        [Inject] private IFlyoutPanelService FlyoutPanelService { get; set; } = default!;
        private IJSObjectReference _designerModule = default!;

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                _designerModule = await JS.InvokeAsync<IJSObjectReference>("import", "./_content/ElsaDashboard.Application/workflowDesigner.js");
            }
        }

        public async ValueTask DisposeAsync()
        {
            if (_designerModule != null!)
                await _designerModule.DisposeAsync();
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
            await FlyoutPanelService.HideAsync();
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