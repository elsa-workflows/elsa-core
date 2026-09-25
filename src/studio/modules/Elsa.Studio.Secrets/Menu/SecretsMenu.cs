using Elsa.Studio.Contracts;
using Elsa.Studio.Extensions;
using Elsa.Studio.Models;
using MudBlazor;
namespace Elsa.Studio.Secrets.Menu;

public class SecretsMenu(IRemoteFeatureProvider remoteFeatureProvider) : IMenuProvider
{
    public async ValueTask<IEnumerable<MenuItem>> GetMenuItemsAsync(CancellationToken cancellationToken = default)
    {
        if (!await remoteFeatureProvider.IsEnabledOrDefaultAsync(Feature.RemoteFeatureName, cancellationToken))
            return [];

        return
        [
            new()
            {
                Icon = Icons.Material.Filled.Key,
                Href = "security/secrets",
                Text = "Secrets",
                GroupName = MenuItemGroups.Administration.Name,
                Order = 300
            }
        ];
    }
}
