using System.Threading.Tasks;

namespace ElsaDashboard.Blazor.Application.Services
{
    public interface IFlyoutPanelService
    {
        ValueTask ShowAsync();
        ValueTask HideAsync();
    }
}