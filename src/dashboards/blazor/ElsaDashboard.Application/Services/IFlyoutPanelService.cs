using System;
using System.Threading.Tasks;
using ElsaDashboard.Application.Models;

namespace ElsaDashboard.Application.Services
{
    public interface IFlyoutPanelService
    {
        event Action? OnShow;
        event Action? OnHide;
        FlyoutPanelOptions Options { get; }
        ValueTask ShowAsync(FlyoutPanelOptions options);
        ValueTask HideAsync();
    }
}