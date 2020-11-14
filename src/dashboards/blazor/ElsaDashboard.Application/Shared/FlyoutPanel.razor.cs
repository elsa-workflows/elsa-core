using ElsaDashboard.Application.Services;
using Microsoft.AspNetCore.Components;

namespace ElsaDashboard.Application.Shared
{
    partial class FlyoutPanel
    {
        [Inject] private IFlyoutPanelService FlyoutPanelService { get; set; } = default!;
        [Parameter] public string? Title { get; set; }
        [Parameter] public RenderFragment? Content { get; set; }

        protected override void OnInitialized()
        {
            FlyoutPanelService.OnShow += Update;
        }

        private async void Update()
        {
            await InvokeAsync(() =>
            {
                Content = builder =>
                {
                    builder.OpenComponent(0, FlyoutPanelService.ContentComponentType!);
                    builder.CloseComponent();
                };

                Title = FlyoutPanelService.Title;
                StateHasChanged();
            });
        }
    }
}