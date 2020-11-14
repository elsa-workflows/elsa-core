using System;
using System.Threading.Tasks;

namespace ElsaDashboard.Application.Services
{
    public interface IFlyoutPanelService
    {
        event Action? OnShow;
        event Action? OnHide;
        Type? ContentComponentType { get; }
        string? Title { get; }
        ValueTask ShowAsync(Type contentComponentType, string title);
        ValueTask HideAsync();
    }
}