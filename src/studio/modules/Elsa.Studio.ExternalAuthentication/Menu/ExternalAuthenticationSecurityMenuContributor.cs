using Elsa.Studio.Contracts;
using Elsa.Studio.Extensions;
using Elsa.Studio.ExternalAuthentication.Models;
using Elsa.Studio.ExternalAuthentication.Services;
using Elsa.Studio.Models;
using Elsa.Studio.Security.Contracts;
using MudBlazor;

namespace Elsa.Studio.ExternalAuthentication.Menu;

public sealed class ExternalAuthenticationSecurityMenuContributor(
    IRemoteFeatureProvider remoteFeatures,
    IExternalAuthenticationPermissionService permissions) : ISecurityMenuContributor
{
    public async ValueTask<IEnumerable<MenuItem>> GetMenuItemsAsync(CancellationToken cancellationToken = default)
    {
        if (!await remoteFeatures.IsEnabledOrDefaultAsync(Feature.RemoteFeatureName, cancellationToken))
            return [];

        var items = new List<MenuItem>();
        if (await permissions.HasAsync(ExternalAuthenticationPermissions.Read, cancellationToken))
        {
            items.Add(new()
            {
                Icon = Icons.Material.Filled.AdminPanelSettings,
                Href = "security/external-authentication/connections",
                Text = "Identity provider connections",
                Order = 100
            });
        }

        if (await permissions.HasAsync(ExternalAuthenticationPermissions.ManageLinks, cancellationToken))
        {
            items.Add(new()
            {
                Icon = Icons.Material.Filled.Link,
                Href = "security/external-authentication/identity-links",
                Text = "External identity links",
                Order = 200
            });
        }

        if (await permissions.HasAsync(ExternalAuthenticationPermissions.SessionsRead, cancellationToken))
        {
            items.Add(new()
            {
                Icon = Icons.Material.Filled.Devices,
                Href = "security/external-authentication/sessions",
                Text = "Authentication sessions",
                Order = 300
            });
        }

        return items;
    }
}
