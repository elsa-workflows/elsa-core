using System;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace ElsaDashboard.Blazor.Shared.Services
{
    public class FlyoutPanelService : IFlyoutPanelService, IAsyncDisposable
    {
        private readonly IJSRuntime _jsRuntime;
        private IJSObjectReference _module;

        public FlyoutPanelService(IJSRuntime jsRuntime)
        {
            _jsRuntime = jsRuntime;
        }
        
        public async ValueTask ShowAsync()
        {
            await InitModuleAsync();
            await _module.InvokeVoidAsync("show");
        }
        
        public async ValueTask HideAsync()
        {
            await InitModuleAsync();
            await _module.InvokeVoidAsync("hide");
        }

        public async ValueTask DisposeAsync()
        {
            if (_module != null)
                await _module.DisposeAsync();
        }
        
        private async Task InitModuleAsync()
        {
            _module = await _jsRuntime.InvokeAsync<IJSObjectReference>("import", "./_content/ElsaDashboard.Blazor.Shared/flyoutPanel.js");
        }
    }
}