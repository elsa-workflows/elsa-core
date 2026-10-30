using Elsa.Studio.Abstractions;
using Elsa.Studio.Authentication.OpenIdConnect.BlazorWasm.Components;
using Elsa.Studio.Contracts;
using JetBrains.Annotations;

namespace Elsa.Studio.Authentication.OpenIdConnect.BlazorWasm;

/// <summary>
/// Adds the OpenID Connect user menu, including the sign-out entry point, to the Studio app bar.
/// </summary>
[UsedImplicitly]
public class OpenIdConnectBlazorWasmFeature(IAppBarService appBarService) : FeatureBase
{
    /// <inheritdoc />
    public override ValueTask InitializeAsync(CancellationToken cancellationToken = default)
    {
        appBarService.AddComponent<OpenIdConnectUserMenu>();
        return base.InitializeAsync(cancellationToken);
    }
}
