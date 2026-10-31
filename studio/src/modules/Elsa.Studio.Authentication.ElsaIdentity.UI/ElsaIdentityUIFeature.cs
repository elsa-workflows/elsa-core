using Elsa.Studio.Abstractions;
using Elsa.Studio.Authentication.ElsaIdentity.UI.Components;
using Elsa.Studio.Contracts;

namespace Elsa.Studio.Authentication.ElsaIdentity.UI;

/// <summary>
/// Adds the ElsaIdentity user menu, including the sign-out entry point, to the Studio app bar.
/// </summary>
public class ElsaIdentityUIFeature(IAppBarService appBarService) : FeatureBase
{
    /// <inheritdoc />
    public override ValueTask InitializeAsync(CancellationToken cancellationToken = default)
    {
        appBarService.AddComponent<ElsaIdentityUserMenu>();
        return base.InitializeAsync(cancellationToken);
    }
}
