using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace ElsaDashboard.Application.Services
{
    public class FlyoutPanelService : IFlyoutPanelService, IAsyncDisposable
    {
        private readonly IJSRuntime _jsRuntime;
        private IJSObjectReference _module = default!;

        public FlyoutPanelService(IJSRuntime jsRuntime)
        {
            _jsRuntime = jsRuntime;
        }

        public event Action? OnShow;
        public event Action? OnHide;

        public Type? ContentComponentType { get; private set; }
        public string? Title { get; private set; }

        public async ValueTask ShowAsync(Type contentComponentType, string title)
        {
            if (!typeof(ComponentBase).IsAssignableFrom(contentComponentType))
                throw new ArgumentException($"{contentComponentType!.FullName} must be a Blazor Component");

            await InitJSModuleAsync();
            await _module.InvokeVoidAsync("show");
            ContentComponentType = contentComponentType;
            Title = title;
            OnShow?.Invoke();
        }
        
        public async ValueTask HideAsync()
        {
            await InitJSModuleAsync();
            await _module.InvokeVoidAsync("hide");
            OnHide?.Invoke();
        }

        public async ValueTask DisposeAsync()
        {
            if (_module != null!)
                await _module.DisposeAsync();
        }
        
        private async Task InitJSModuleAsync()
        {
            _module = await _jsRuntime.InvokeAsync<IJSObjectReference>("import", "./_content/ElsaDashboard.Application/flyoutPanel.js");
        }
    }
}