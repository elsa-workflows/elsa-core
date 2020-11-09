using System.Threading.Tasks;

namespace ElsaDashboard.Blazor.Shared.Services
{
    public interface IFlyoutPanelService
    {
        ValueTask ShowAsync();
        ValueTask HideAsync();
    }
}