using Elsa.Studio.Contracts;
using Elsa.Studio.Extensions;
using Elsa.Studio.Models;
using MudBlazor;

namespace Elsa.Studio.Diagnostics.StructuredLogs.Menu;

/// <summary>
/// Exposes menu entries for structured logs.
/// </summary>
public class StructuredLogsMenu(IRemoteFeatureProvider remoteFeatureProvider) : IMenuProvider
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
                Icon = Icons.Material.Filled.FormatListBulleted,
                Href = "diagnostics/structured-logs",
                Text = "Structured Logs",
                GroupName = MenuItemGroups.Diagnostics.Name
            }
        ];

        return menuItems;
    }
}
