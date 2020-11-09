using System;
using System.Threading.Tasks;
using ElsaDashboard.Blazor.Shared.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace ElsaDashboard.Blazor.Shared.Pages.Designer
{
    partial class Designer
    {
        [Inject] private IJSRuntime JS { get; set; } = default!;
        [Inject] private IFlyoutPanelService FlyoutPanelService { get; set; } = default!;
        private IJSObjectReference _designerModule;

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                _designerModule = await JS.InvokeAsync<IJSObjectReference>("import", "./_content/ElsaDashboard.Blazor.Shared/workflowDesigner.js");
            }
        }

        public async ValueTask DisposeAsync()
        {
            if(_designerModule != null)
                await _designerModule.DisposeAsync();
        }

        private async ValueTask OnActivityClick(MouseEventArgs e)
        {
            await FlyoutPanelService.ShowAsync();
        }
    }
}