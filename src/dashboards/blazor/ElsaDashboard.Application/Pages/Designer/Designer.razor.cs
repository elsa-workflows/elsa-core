using System.Threading.Tasks;
using ElsaDashboard.Application.Extensions;
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
            if(_designerModule != null!)
                await _designerModule.DisposeAsync();
        }

        private async ValueTask OnStartClick(MouseEventArgs e)
        {
            await FlyoutPanelService.ShowAsync<ActivityPicker>("Activities");
        }

        private async ValueTask OnActivityClick(MouseEventArgs e)
        {
            await FlyoutPanelService.ShowAsync<ActivityEditor>("Timer Properties");
        }
    }
}