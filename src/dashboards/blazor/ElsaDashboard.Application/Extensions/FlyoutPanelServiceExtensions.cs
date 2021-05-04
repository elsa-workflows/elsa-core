using System.Threading.Tasks;
using ElsaDashboard.Application.Models;
using ElsaDashboard.Application.Services;
using Microsoft.AspNetCore.Components;

namespace ElsaDashboard.Application.Extensions
{
    public static class FlyoutPanelServiceExtensions
    {
        public static ValueTask ShowAsync<T>(this IFlyoutPanelService service, string title) where T : ComponentBase =>
            service.ShowAsync<T>(new FlyoutPanelOptions
            {
                Title = title
            });

        

        public static ValueTask ShowAsync<T>(this IFlyoutPanelService service, string title, object parameters, params ButtonDescriptor[] buttons) where T : ComponentBase =>
            service.ShowAsync<T>(new FlyoutPanelOptions
            {
                Title = title,
                Buttons = buttons,
                Parameters = parameters.ToDictionary()
            });
        
        public static ValueTask ShowAsync<T>(this IFlyoutPanelService service, FlyoutPanelOptions options) where T : ComponentBase =>
            service.ShowAsync(new FlyoutPanelOptions
            {
                ContentComponentType = typeof(T),
                Buttons = options.Buttons,
                Parameters = options.Parameters,
                Title = options.Title
            });
    }
}