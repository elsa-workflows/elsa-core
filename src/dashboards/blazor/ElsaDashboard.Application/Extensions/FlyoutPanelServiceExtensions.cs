using System.Threading.Tasks;
using ElsaDashboard.Application.Services;
using Microsoft.AspNetCore.Components;

namespace ElsaDashboard.Application.Extensions
{
    public static class FlyoutPanelServiceExtensions
    {
        public static ValueTask ShowAsync<T>(this IFlyoutPanelService service, string title) where T : ComponentBase => service.ShowAsync(typeof(T), title);
    }
}