using Elsa.Studio.Contracts;
using Elsa.Studio.Extensions;
using Elsa.Studio.Models;
using Elsa.Studio.Security.Contracts;
using MudBlazor;

namespace Elsa.Studio.Security.Menu;

/// <summary>
/// Contributes the Users and Roles entries. Each entry is gated only by its own structured <c>view</c> grant,
/// so a caller who can administer users but not roles still sees Users, and vice versa.
/// </summary>
public sealed class IdentitySecurityMenuContributor(
    IRemoteFeatureProvider remoteFeatures,
    IUserAdministrationAccessService userAccessService,
    IRoleAdministrationAccessService roleAccessService) : ISecurityMenuContributor
{
    public async ValueTask<IEnumerable<MenuItem>> GetMenuItemsAsync(CancellationToken cancellationToken = default)
    {
        if (!await remoteFeatures.IsEnabledOrDefaultAsync(Feature.RemoteFeatureName, cancellationToken))
            return [];

        var items = new List<MenuItem>();
        var userAccess = await userAccessService.GetAsync(cancellationToken);
        if (userAccess.CanView)
        {
            items.Add(new()
            {
                Icon = Icons.Material.Filled.People,
                Href = "security/users",
                Text = "Users",
                Order = 10
            });
        }

        var roleAccess = await roleAccessService.GetAsync(cancellationToken);
        if (roleAccess.CanView)
        {
            items.Add(new()
            {
                Icon = Icons.Material.Filled.Badge,
                Href = "security/roles",
                Text = "Roles",
                Order = 20
            });
        }

        return items;
    }
}
