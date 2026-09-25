using Elsa.Studio.Contracts;
using Elsa.Studio.Extensions;
using Elsa.Studio.Models;
using MudBlazor;

namespace Elsa.Studio.Diagnostics.OpenTelemetry.Menu;

/// <summary>
/// Exposes menu entries for OpenTelemetry diagnostics.
/// </summary>
public class OpenTelemetryMenu(IRemoteFeatureProvider remoteFeatureProvider) : IMenuProvider
{
    /// <inheritdoc />
    public async ValueTask<IEnumerable<MenuItem>> GetMenuItemsAsync(CancellationToken cancellationToken = default)
    {
        if (!await remoteFeatureProvider.IsEnabledOrDefaultAsync(Feature.RemoteFeatureName, cancellationToken))
            return [];

        IEnumerable<MenuItem> menuItems =
        [
            new()
            {
                Icon = Icons.Material.Filled.Timeline,
                Href = "diagnostics/opentelemetry",
                Text = "OpenTelemetry",
                GroupName = MenuItemGroups.Diagnostics.Name
            }
        ];

        return menuItems;
    }
}
